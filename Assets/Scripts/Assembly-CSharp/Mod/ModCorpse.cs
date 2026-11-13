using System.Collections.Generic;
using UnityEngine;
using Zombie3D;

/// <summary>
/// Dead enemies stay on the ground for a while, then break apart into pixels
/// with a glowing edge and a burst of pixel bits, and only then disappear.
///
/// The enemy leaves the game's enemy list at the original moment, so wave
/// counts and spawn limits are not held up by bodies lying on the ground.
/// </summary>
public class ModCorpse : MonoBehaviour
{
	private static Shader dissolveShader;

	private static bool shaderMissing;

	// Dissolve copy of each original material, and the way back.
	private static readonly Dictionary<Material, Material> dissolveFor = new Dictionary<Material, Material>();

	private static readonly Dictionary<Material, Material> originalFor = new Dictionary<Material, Material>();

	private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

	private Enemy enemy;

	private float startTime;

	private bool running;

	private bool dissolving;

	private float nextBits;

	private readonly List<Renderer> renderers = new List<Renderer>();

	private readonly List<Material[]> savedMaterials = new List<Material[]>();

	private MaterialPropertyBlock block;

	/// <summary>
	/// Takes over the removal of a dead enemy's body. Returns false when the
	/// effect is not available, in which case the caller removes the body itself.
	/// </summary>
	public static bool TryStart(Enemy enemy)
	{
		if (enemy == null || !LoadShader())
		{
			return false;
		}
		GameObject obj = enemy.GetEnemyObject();
		if (obj == null || !obj.activeInHierarchy)
		{
			return false;
		}
		ModCorpse corpse = obj.GetComponent<ModCorpse>();
		if (corpse == null)
		{
			corpse = obj.AddComponent<ModCorpse>();
		}
		corpse.Begin(enemy);
		return true;
	}

	/// <summary>
	/// A pooled enemy can be copied while its body is dissolving, which would copy
	/// the dissolve materials too. Called whenever an enemy spawns to undo that.
	/// </summary>
	public static void CleanUpOnSpawn(GameObject obj)
	{
		if (obj == null)
		{
			return;
		}
		ModCorpse corpse = obj.GetComponent<ModCorpse>();
		if (corpse != null)
		{
			corpse.running = false;
			corpse.dissolving = false;
			corpse.renderers.Clear();
			corpse.savedMaterials.Clear();
		}
		if (originalFor.Count == 0)
		{
			return;
		}
		Renderer[] all = obj.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < all.Length; i++)
		{
			Material[] mats = all[i].sharedMaterials;
			bool changed = false;
			for (int m = 0; m < mats.Length; m++)
			{
				Material original;
				if (mats[m] != null && originalFor.TryGetValue(mats[m], out original))
				{
					mats[m] = original;
					changed = true;
				}
			}
			if (changed)
			{
				all[i].sharedMaterials = mats;
				all[i].SetPropertyBlock(null);
			}
		}
	}

	private static bool LoadShader()
	{
		if (dissolveShader != null)
		{
			return true;
		}
		if (shaderMissing)
		{
			return false;
		}
		dissolveShader = Resources.Load<Shader>("ModFX/ModDissolve");
		if (dissolveShader == null || !dissolveShader.isSupported)
		{
			dissolveShader = null;
			shaderMissing = true;
			Debug.LogWarning("[Mod] dissolve shader missing or unsupported, bodies disappear the normal way.");
			return false;
		}
		return true;
	}

	private static Material GetDissolveMaterial(Material original)
	{
		if (original == null)
		{
			return null;
		}
		Material copy;
		if (dissolveFor.TryGetValue(original, out copy) && copy != null)
		{
			return copy;
		}
		copy = new Material(dissolveShader);
		copy.name = original.name + "_ModDissolve";
		if (original.HasProperty("_MainTex"))
		{
			copy.mainTexture = original.mainTexture;
			copy.mainTextureScale = original.mainTextureScale;
			copy.mainTextureOffset = original.mainTextureOffset;
		}
		dissolveFor[original] = copy;
		originalFor[copy] = original;
		return copy;
	}

	private void Begin(Enemy dead)
	{
		enemy = dead;
		startTime = Time.time;
		running = true;
		dissolving = false;
	}

	private void Update()
	{
		if (!running)
		{
			return;
		}
		float elapsed = Time.time - startTime;
		float wait = ModTuning.CorpseGroundTime - ModTuning.CorpseListRemovalTime;
		if (!dissolving)
		{
			if (elapsed < wait)
			{
				return;
			}
			StartDissolve();
		}
		float progress = Mathf.Clamp01((elapsed - wait) / ModTuning.CorpseDissolveTime);
		SetDissolve(progress);
		if (Time.time >= nextBits && progress < 0.9f)
		{
			nextBits = Time.time + 0.12f;
			ModFX.PixelBits(GetBounds(), 6);
		}
		if (progress >= 1f)
		{
			Finish();
		}
	}

	private Bounds GetBounds()
	{
		Bounds bounds = new Bounds(transform.position + Vector3.up * 0.4f, Vector3.one * 0.8f);
		bool first = true;
		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] == null)
			{
				continue;
			}
			if (first)
			{
				bounds = renderers[i].bounds;
				first = false;
			}
			else
			{
				bounds.Encapsulate(renderers[i].bounds);
			}
		}
		return bounds;
	}

	private void StartDissolve()
	{
		dissolving = true;
		renderers.Clear();
		savedMaterials.Clear();
		Renderer[] all = GetComponentsInChildren<Renderer>(false);
		for (int i = 0; i < all.Length; i++)
		{
			Renderer r = all[i];
			if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer))
			{
				continue;
			}
			if (r.gameObject.name == "ModOutline")
			{
				continue;
			}
			Material[] originals = r.sharedMaterials;
			Material[] swapped = new Material[originals.Length];
			for (int m = 0; m < originals.Length; m++)
			{
				swapped[m] = GetDissolveMaterial(originals[m]);
			}
			renderers.Add(r);
			savedMaterials.Add(originals);
			r.sharedMaterials = swapped;
		}
		ZombieOutline outline = GetComponent<ZombieOutline>();
		if (outline != null)
		{
			outline.SetHidden(true);
		}
		ModFX.PixelBits(GetBounds(), 28);
		SetDissolve(0f);
	}

	private void SetDissolve(float amount)
	{
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		block.SetFloat(DissolveId, amount);
		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null)
			{
				renderers[i].SetPropertyBlock(block);
			}
		}
	}

	private void RestoreMaterials()
	{
		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null)
			{
				renderers[i].sharedMaterials = savedMaterials[i];
				renderers[i].SetPropertyBlock(null);
			}
		}
		renderers.Clear();
		savedMaterials.Clear();
		ZombieOutline outline = GetComponent<ZombieOutline>();
		if (outline != null)
		{
			outline.SetHidden(false);
		}
	}

	private void Finish()
	{
		running = false;
		dissolving = false;
		// Hide first so the restored look is never seen for a frame.
		List<Renderer> hidden = new List<Renderer>();
		for (int i = 0; i < renderers.Count; i++)
		{
			if (renderers[i] != null && renderers[i].enabled)
			{
				renderers[i].enabled = false;
				hidden.Add(renderers[i]);
			}
		}
		Enemy dead = enemy;
		enemy = null;
		RestoreMaterials();
		if (dead != null)
		{
			dead.FinishDeadbodyRemoval();
		}
		// The game deactivates pooled bodies rather than destroying them: turn
		// those renderers back on so the next zombie using this body is visible.
		for (int j = 0; j < hidden.Count; j++)
		{
			if (hidden[j] != null)
			{
				hidden[j].enabled = true;
			}
		}
	}

	private void OnDisable()
	{
		if (running && dissolving)
		{
			RestoreMaterials();
		}
		running = false;
		dissolving = false;
	}
}
