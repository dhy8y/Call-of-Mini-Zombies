using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Impact effects built entirely in code (no prefab or texture to import):
///   - blood spray when an enemy is hit
///   - dust, sparks and a bullet mark when the ground or a wall is hit
///   - a wide dust ring and a scorch mark where an explosive goes off
///
/// Each kind of particle uses one shared particle system for the whole scene,
/// so a burst is a few Emit calls and never creates new objects.
/// </summary>
public static class ModFX
{
	private const int MaxMarks = 48;

	private const float BulletMarkLife = 7f;

	private const float ScorchMarkLife = 12f;

	private const float MarkFadeTime = 2f;

	private class Mark
	{
		public GameObject obj;

		public Renderer renderer;

		public Color color;

		public float life;

		public float age;

		public bool active;
	}

	private static readonly int ColorId = Shader.PropertyToID("_Color");

	private static GameObject root;

	private static ParticleSystem blood;

	private static ParticleSystem dust;

	private static ParticleSystem sparks;

	private static ParticleSystem pixels;

	private static Material pixelMaterial;

	private static Material particleMaterial;

	private static Texture2D softDot;

	private static Mesh quad;

	private static readonly List<Mark> marks = new List<Mark>();

	private static int nextMark;

	private static MaterialPropertyBlock markBlock;

	private static bool unavailable;

	// ---- Public effects --------------------------------------------------

	/// <summary>Blood thrown from a hit point, mostly along the direction the bullet was travelling.</summary>
	public static void BloodSpray(Vector3 point, Vector3 direction, float scale)
	{
		if (!Ensure())
		{
			return;
		}
		Vector3 dir = (direction.sqrMagnitude > 0.0001f) ? direction.normalized : Vector3.up;
		int count = Mathf.Clamp(Mathf.RoundToInt(7f * scale), 4, 24);
		for (int i = 0; i < count; i++)
		{
			Vector3 velocity = (dir * Random.Range(1.5f, 4.5f) + Random.insideUnitSphere * 2.2f + Vector3.up * Random.Range(0.5f, 2.5f)) * Mathf.Sqrt(scale);
			float shade = Random.Range(0.45f, 0.8f);
			Emit(blood, point + Random.insideUnitSphere * 0.08f, velocity, Random.Range(0.07f, 0.17f) * Mathf.Sqrt(scale), Random.Range(0.35f, 0.65f), new Color(shade, 0.02f, 0.02f, 0.95f));
		}
	}

	/// <summary>A bullet hitting the ground, a wall or a prop.</summary>
	public static void SurfaceImpact(Vector3 point, Vector3 normal)
	{
		if (!Ensure())
		{
			return;
		}
		Vector3 n = (normal.sqrMagnitude > 0.0001f) ? normal.normalized : Vector3.up;
		Vector3 origin = point + n * 0.05f;
		for (int i = 0; i < 4; i++)
		{
			Vector3 velocity = n * Random.Range(0.3f, 1.1f) + Random.insideUnitSphere * 0.5f;
			float shade = Random.Range(0.5f, 0.7f);
			Emit(dust, origin, velocity, Random.Range(0.18f, 0.32f), Random.Range(0.45f, 0.8f), new Color(shade, shade * 0.93f, shade * 0.82f, 0.5f));
		}
		for (int j = 0; j < 5; j++)
		{
			Vector3 velocity2 = n * Random.Range(2f, 5f) + Random.insideUnitSphere * 2.5f;
			Emit(sparks, origin, velocity2, Random.Range(0.03f, 0.06f), Random.Range(0.12f, 0.28f), new Color(1f, Random.Range(0.6f, 0.9f), 0.25f, 1f));
		}
		PlaceMark(point, n, Random.Range(0.16f, 0.26f), new Color(0.06f, 0.05f, 0.04f, 0.8f), BulletMarkLife);
	}

	/// <summary>An explosive going off just above the ground.</summary>
	public static void Explosion(Vector3 groundPosition)
	{
		if (!Ensure())
		{
			return;
		}
		Vector3 origin = groundPosition + Vector3.up * 0.15f;
		for (int i = 0; i < 18; i++)
		{
			float angle = (float)i / 18f * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
			Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
			Vector3 velocity = outward * Random.Range(2.5f, 5f) + Vector3.up * Random.Range(0.2f, 1.2f);
			float shade = Random.Range(0.35f, 0.55f);
			Emit(dust, origin + outward * 0.4f, velocity, Random.Range(0.6f, 1.1f), Random.Range(0.8f, 1.4f), new Color(shade, shade * 0.92f, shade * 0.8f, 0.55f));
		}
		for (int j = 0; j < 16; j++)
		{
			Vector3 velocity2 = Random.onUnitSphere * Random.Range(4f, 9f);
			velocity2.y = Mathf.Abs(velocity2.y) + 2f;
			Emit(sparks, origin, velocity2, Random.Range(0.05f, 0.09f), Random.Range(0.3f, 0.6f), new Color(1f, Random.Range(0.45f, 0.8f), 0.15f, 1f));
		}
		PlaceMark(groundPosition, Vector3.up, Random.Range(2.6f, 3.4f), new Color(0.03f, 0.03f, 0.03f, 0.7f), ScorchMarkLife);
	}

	/// <summary>Square bits drifting up out of a dissolving body.</summary>
	public static void PixelBits(Bounds area, int count)
	{
		if (!Ensure())
		{
			return;
		}
		Vector3 min = area.min;
		Vector3 size = area.size;
		for (int i = 0; i < count; i++)
		{
			Vector3 position = min + new Vector3(Random.value * size.x, Random.value * size.y, Random.value * size.z);
			Vector3 velocity = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.8f), Random.Range(-0.4f, 0.4f));
			Color color = (Random.value < 0.35f) ? new Color(1f, Random.Range(0.45f, 0.75f), 0.1f, 1f) : new Color(Random.Range(0.15f, 0.35f), Random.Range(0.3f, 0.5f), Random.Range(0.15f, 0.25f), 1f);
			Emit(pixels, position, velocity, Random.Range(0.05f, 0.11f), Random.Range(0.6f, 1.1f), color);
		}
	}

	/// <summary>Fades the ground marks out. Called once per frame by ModRuntime.</summary>
	public static void Tick(float deltaTime)
	{
		if (root == null || marks.Count == 0)
		{
			return;
		}
		for (int i = 0; i < marks.Count; i++)
		{
			Mark mark = marks[i];
			if (!mark.active)
			{
				continue;
			}
			mark.age += deltaTime;
			if (mark.age >= mark.life || mark.obj == null)
			{
				mark.active = false;
				if (mark.obj != null)
				{
					mark.obj.SetActive(false);
				}
				continue;
			}
			float remaining = mark.life - mark.age;
			if (remaining < MarkFadeTime)
			{
				SetMarkColor(mark, remaining / MarkFadeTime);
			}
		}
	}

	// ---- Setup -----------------------------------------------------------

	private static bool Ensure()
	{
		if (unavailable)
		{
			return false;
		}
		// Everything hangs under one object that dies with the scene, so a new
		// level simply rebuilds it on the first effect.
		if (root != null)
		{
			return true;
		}
		if (particleMaterial == null)
		{
			Shader shader = Resources.Load<Shader>("ModFX/ModParticle");
			if (shader == null || !shader.isSupported)
			{
				unavailable = true;
				Debug.LogWarning("[Mod] particle shader missing or unsupported, impact effects are off.");
				return false;
			}
			particleMaterial = new Material(shader);
			particleMaterial.name = "ModParticle";
			particleMaterial.mainTexture = GetSoftDot();
		}
		root = new GameObject("ModFX");
		marks.Clear();
		nextMark = 0;
		blood = CreateSystem("Blood", 400, 1.6f, false, 1f, 0.6f);
		dust = CreateSystem("Dust", 300, -0.04f, false, 1f, 2.4f);
		sparks = CreateSystem("Sparks", 300, 1.1f, true, 1f, 0.3f);
		if (pixelMaterial == null)
		{
			// Hard-edged squares, so the bits read as pixels.
			pixelMaterial = new Material(particleMaterial.shader);
			pixelMaterial.name = "ModPixel";
			pixelMaterial.mainTexture = Texture2D.whiteTexture;
		}
		pixels = CreateSystem("Pixels", 500, -0.15f, false, 1f, 0.2f);
		pixels.GetComponent<ParticleSystemRenderer>().sharedMaterial = pixelMaterial;
		return true;
	}

	private static ParticleSystem CreateSystem(string name, int maxParticles, float gravity, bool stretched, float startScale, float endScale)
	{
		GameObject obj = new GameObject("ModFX_" + name);
		obj.transform.SetParent(root.transform, false);
		ParticleSystem system = obj.AddComponent<ParticleSystem>();
		system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		ParticleSystem.MainModule main = system.main;
		main.duration = 1f;
		main.loop = true;
		main.playOnAwake = false;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.maxParticles = maxParticles;
		main.gravityModifier = gravity;
		main.startSpeed = 0f;
		main.startLifetime = 0.5f;
		main.startSize = 0.1f;

		// Particles only ever come from Emit calls.
		ParticleSystem.EmissionModule emission = system.emission;
		emission.enabled = false;
		ParticleSystem.ShapeModule shape = system.shape;
		shape.enabled = false;

		ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
		fade.enabled = true;
		Gradient gradient = new Gradient();
		gradient.SetKeys(new GradientColorKey[2]
		{
			new GradientColorKey(Color.white, 0f),
			new GradientColorKey(Color.white, 1f)
		}, new GradientAlphaKey[3]
		{
			new GradientAlphaKey(1f, 0f),
			new GradientAlphaKey(0.85f, 0.55f),
			new GradientAlphaKey(0f, 1f)
		});
		fade.color = new ParticleSystem.MinMaxGradient(gradient);

		ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
		size.enabled = true;
		size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, startScale, 1f, endScale));

		ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
		renderer.sharedMaterial = particleMaterial;
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		if (stretched)
		{
			renderer.renderMode = ParticleSystemRenderMode.Stretch;
			renderer.velocityScale = 0.06f;
			renderer.lengthScale = 1.5f;
		}
		else
		{
			renderer.renderMode = ParticleSystemRenderMode.Billboard;
		}

		// Looping with emission off: the system stays alive and simulates
		// whatever Emit adds, without producing anything by itself.
		system.Play(true);
		return system;
	}

	private static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, float size, float lifetime, Color color)
	{
		if (system == null)
		{
			return;
		}
		ParticleSystem.EmitParams emit = default(ParticleSystem.EmitParams);
		emit.position = position;
		emit.velocity = velocity;
		emit.startSize = size;
		emit.startLifetime = lifetime;
		emit.startColor = color;
		system.Emit(emit, 1);
	}

	/// <summary>White disc, opaque in the middle and fading to nothing at the edge.</summary>
	private static Texture2D GetSoftDot()
	{
		if (softDot != null)
		{
			return softDot;
		}
		const int size = 32;
		softDot = new Texture2D(size, size, TextureFormat.RGBA32, false);
		softDot.name = "ModSoftDot";
		softDot.wrapMode = TextureWrapMode.Clamp;
		softDot.filterMode = FilterMode.Bilinear;
		Color32[] pixels = new Color32[size * size];
		float half = (float)(size - 1) * 0.5f;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dx = ((float)x - half) / half;
				float dy = ((float)y - half) / half;
				float d = Mathf.Sqrt(dx * dx + dy * dy);
				float a = Mathf.Clamp01(1f - d);
				a = a * a * (3f - 2f * a);
				pixels[y * size + x] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)Mathf.RoundToInt(a * 255f));
			}
		}
		softDot.SetPixels32(pixels);
		softDot.Apply(false, true);
		return softDot;
	}

	// ---- Ground marks ----------------------------------------------------

	private static Mesh GetQuad()
	{
		if (quad != null)
		{
			return quad;
		}
		quad = new Mesh();
		quad.name = "ModMarkQuad";
		quad.vertices = new Vector3[4]
		{
			new Vector3(-0.5f, -0.5f, 0f),
			new Vector3(0.5f, -0.5f, 0f),
			new Vector3(0.5f, 0.5f, 0f),
			new Vector3(-0.5f, 0.5f, 0f)
		};
		quad.uv = new Vector2[4]
		{
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(1f, 1f),
			new Vector2(0f, 1f)
		};
		quad.colors = new Color[4]
		{
			Color.white,
			Color.white,
			Color.white,
			Color.white
		};
		quad.triangles = new int[6] { 0, 2, 1, 0, 3, 2 };
		quad.RecalculateBounds();
		return quad;
	}

	private static void PlaceMark(Vector3 point, Vector3 normal, float size, Color color, float life)
	{
		Mark mark;
		if (marks.Count < MaxMarks)
		{
			mark = new Mark();
			mark.obj = new GameObject("ModMark");
			mark.obj.transform.SetParent(root.transform, false);
			mark.obj.AddComponent<MeshFilter>().sharedMesh = GetQuad();
			MeshRenderer meshRenderer = mark.obj.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterial = particleMaterial;
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			mark.renderer = meshRenderer;
			marks.Add(mark);
		}
		else
		{
			// Pool is full: recycle the oldest mark.
			mark = marks[nextMark];
			nextMark = (nextMark + 1) % MaxMarks;
		}
		if (mark.obj == null)
		{
			return;
		}
		Transform t = mark.obj.transform;
		t.position = point + normal * 0.03f;
		t.rotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
		t.localScale = new Vector3(size, size, size);
		mark.color = color;
		mark.life = life;
		mark.age = 0f;
		mark.active = true;
		mark.obj.SetActive(true);
		SetMarkColor(mark, 1f);
	}

	private static void SetMarkColor(Mark mark, float alphaScale)
	{
		if (mark.renderer == null)
		{
			return;
		}
		if (markBlock == null)
		{
			markBlock = new MaterialPropertyBlock();
		}
		Color c = mark.color;
		c.a *= Mathf.Clamp01(alphaScale);
		markBlock.SetColor(ColorId, c);
		mark.renderer.SetPropertyBlock(markBlock);
	}
}
