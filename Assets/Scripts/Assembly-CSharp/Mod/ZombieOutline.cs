using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thin black line around a character's silhouette (enemies and player
/// characters). On enemies it turns into a coloured glow for a moment when hit.
///
/// The enemy's own material is left untouched. A second skinned renderer shares
/// the same bones and draws the outline shader on top (see ModOutline.shader).
/// </summary>
public class ZombieOutline : MonoBehaviour
{
	private const string OutlineObjectName = "ModOutline";

	private const float MinBloodInterval = 0.07f;

	private static Material sharedMaterial;

	private static bool materialUnavailable;

	private static readonly Dictionary<Mesh, Mesh> smoothMeshes = new Dictionary<Mesh, Mesh>();

	private static readonly int FlashId = Shader.PropertyToID("_Flash");

	private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

	private readonly List<Renderer> outlineRenderers = new List<Renderer>();

	// The renderer each outline copies, in the same order.
	private readonly List<Renderer> sourceRenderers = new List<Renderer>();

	private MaterialPropertyBlock block;

	private Color flashColor = Color.white;

	private float flashDuration;

	private float flashLeft;

	private float lastBloodTime = -10f;

	private bool built;

	/// <summary>Adds the outline once; enemies recycled from the pool keep theirs.</summary>
	public static ZombieOutline Attach(GameObject enemy)
	{
		ZombieOutline outline = enemy.GetComponent<ZombieOutline>();
		if (outline == null)
		{
			outline = enemy.AddComponent<ZombieOutline>();
		}
		outline.Build();
		outline.ResetFlash();
		return outline;
	}

	private static Material GetMaterial()
	{
		if (sharedMaterial != null)
		{
			return sharedMaterial;
		}
		if (materialUnavailable)
		{
			return null;
		}
		Shader shader = Resources.Load<Shader>("ModFX/ModOutline");
		if (shader == null || !shader.isSupported)
		{
			materialUnavailable = true;
			Debug.LogWarning("[Mod] outline shader missing or unsupported, enemies keep their normal look.");
			return null;
		}
		sharedMaterial = new Material(shader);
		sharedMaterial.name = "ModOutline";
		return sharedMaterial;
	}

	private void Build()
	{
		if (built)
		{
			return;
		}
		built = true;
		Material material = GetMaterial();
		if (material == null)
		{
			return;
		}
		SkinnedMeshRenderer[] sources = GetComponentsInChildren<SkinnedMeshRenderer>(true);
		for (int i = 0; i < sources.Length; i++)
		{
			SkinnedMeshRenderer source = sources[i];
			if (source == null || source.sharedMesh == null || source.gameObject.name == OutlineObjectName)
			{
				continue;
			}
			GameObject child = new GameObject(OutlineObjectName);
			child.layer = source.gameObject.layer;
			child.transform.SetParent(source.transform, false);

			SkinnedMeshRenderer outline = child.AddComponent<SkinnedMeshRenderer>();
			Mesh mesh = GetSmoothMesh(source.sharedMesh);
			outline.sharedMesh = mesh;
			outline.bones = source.bones;
			outline.rootBone = source.rootBone;
			outline.localBounds = source.localBounds;
			outline.updateWhenOffscreen = source.updateWhenOffscreen;
			outline.quality = source.quality;
			outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			outline.receiveShadows = false;

			Material[] materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
			for (int m = 0; m < materials.Length; m++)
			{
				materials[m] = material;
			}
			outline.sharedMaterials = materials;
			outlineRenderers.Add(outline);
			sourceRenderers.Add(source);
		}
	}

	/// <summary>
	/// These models are low-poly with hard edges, so each corner has several
	/// normals pointing different ways and a pushed-out outline would split open
	/// there. Averaging the normals of vertices that sit at the same position
	/// keeps the line closed.
	/// </summary>
	private static Mesh GetSmoothMesh(Mesh source)
	{
		Mesh cached;
		if (smoothMeshes.TryGetValue(source, out cached) && cached != null)
		{
			return cached;
		}
		Mesh result = source;
		try
		{
			if (source.isReadable)
			{
				Vector3[] vertices = source.vertices;
				Vector3[] normals = source.normals;
				if (normals != null && normals.Length == vertices.Length && vertices.Length > 0)
				{
					float size = source.bounds.size.magnitude;
					float cell = Mathf.Max(size * 0.0005f, 1E-06f);
					Dictionary<Vector3Int, Vector3> sums = new Dictionary<Vector3Int, Vector3>(vertices.Length);
					Vector3Int[] keys = new Vector3Int[vertices.Length];
					for (int i = 0; i < vertices.Length; i++)
					{
						Vector3 v = vertices[i] / cell;
						Vector3Int key = new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
						keys[i] = key;
						Vector3 sum;
						sums.TryGetValue(key, out sum);
						sums[key] = sum + normals[i];
					}
					Vector3[] smooth = new Vector3[vertices.Length];
					for (int j = 0; j < vertices.Length; j++)
					{
						Vector3 n = sums[keys[j]];
						smooth[j] = (n.sqrMagnitude > 1E-10f) ? n.normalized : normals[j];
					}
					Mesh copy = Object.Instantiate(source);
					copy.name = source.name + "_ModOutline";
					copy.normals = smooth;
					result = copy;
				}
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] could not smooth outline mesh " + source.name + ": " + ex.Message);
			result = source;
		}
		smoothMeshes[source] = result;
		return result;
	}

	/// <summary>Lights the outline up in this colour, fading out over the given time.</summary>
	public void Flash(Color color, float duration)
	{
		if (outlineRenderers.Count == 0 || duration <= 0f)
		{
			return;
		}
		// A long glow (explosion) is not cut short by a quick bullet hit landing right after.
		if (flashLeft > duration)
		{
			return;
		}
		flashColor = color;
		flashDuration = duration;
		flashLeft = duration;
		Apply(1f);
	}

	public bool TryConsumeBloodSlot()
	{
		if (Time.time - lastBloodTime < MinBloodInterval)
		{
			return false;
		}
		lastBloodTime = Time.time;
		return true;
	}

	private void ResetFlash()
	{
		flashLeft = 0f;
		flashDuration = 0f;
		lastBloodTime = -10f;
		Apply(0f);
	}

	private void Update()
	{
		if (flashLeft <= 0f)
		{
			return;
		}
		flashLeft -= Time.deltaTime;
		if (flashLeft <= 0f)
		{
			flashLeft = 0f;
			Apply(0f);
			return;
		}
		float t = flashLeft / flashDuration;
		// Stay bright for most of the time, then drop off quickly.
		Apply(Mathf.Sqrt(t));
	}

	/// <summary>
	/// The game hides a character by switching its renderers off (for example
	/// while it blinks after a hit). The outline has to disappear with it.
	/// </summary>
	private void LateUpdate()
	{
		for (int i = 0; i < outlineRenderers.Count; i++)
		{
			Renderer outline = outlineRenderers[i];
			Renderer source = sourceRenderers[i];
			if (outline != null && source != null && outline.enabled != source.enabled)
			{
				outline.enabled = source.enabled;
			}
		}
	}

	private void Apply(float amount)
	{
		if (outlineRenderers.Count == 0)
		{
			return;
		}
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		block.SetFloat(FlashId, amount);
		block.SetColor(FlashColorId, flashColor);
		for (int i = 0; i < outlineRenderers.Count; i++)
		{
			if (outlineRenderers[i] != null)
			{
				outlineRenderers[i].SetPropertyBlock(block);
			}
		}
	}
}
