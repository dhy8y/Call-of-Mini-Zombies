using UnityEngine;
using Zombie3D;

/// <summary>
/// The only entry points the original game code calls into the mod.
/// Every hook swallows its own errors: a visual extra must never break a game.
/// </summary>
public static class ModHooks
{
	// Regular hit: short white-hot flash.
	private static readonly Color HitGlow = new Color(1f, 0.92f, 0.6f, 1f);

	// Area damage (rockets, grenades, explosions): orange glow that lingers.
	private static readonly Color BlastGlow = new Color(1f, 0.55f, 0.1f, 1f);

	// Fire: red glow that lingers.
	private static readonly Color BurnGlow = new Color(1f, 0.3f, 0.05f, 1f);

	private const float HitGlowTime = 0.18f;

	private const float BlastGlowTime = 0.7f;

	private const float BurnGlowTime = 0.5f;

	public static bool IsAreaWeapon(WeaponType type)
	{
		switch (type)
		{
		case WeaponType.RocketLauncher:
		case WeaponType.M32:
		case WeaponType.ElectricGun:
		case WeaponType.SuicideGun:
		case WeaponType.Crossbow:
			return true;
		default:
			return false;
		}
	}

	/// <summary>Weapons that already spawn their blood at the exact bullet impact.</summary>
	private static bool HasPreciseImpact(WeaponType type)
	{
		return type == WeaponType.AssaultRifle || type == WeaponType.MachineGun;
	}

	public static void OnEnemySpawned(Enemy enemy)
	{
		try
		{
			GameObject obj = enemy.GetEnemyObject();
			if (obj != null)
			{
				ZombieOutline.Attach(obj);
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] outline setup failed: " + ex.Message);
		}
	}

	/// <summary>A player character was created, in a level or in a menu that shows it.</summary>
	public static void OnAvatarCreated(GameObject avatar)
	{
		try
		{
			if (avatar != null)
			{
				ZombieOutline.Attach(avatar);
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] character outline failed: " + ex.Message);
		}
	}

	public static void OnEnemyHit(Enemy enemy, Vector3 hitForce, WeaponType weaponType)
	{
		// NoGun is the game removing an enemy by itself (fell out of the map, crushed by a boss).
		if (weaponType == WeaponType.NoGun)
		{
			return;
		}
		try
		{
			GameObject obj = enemy.GetEnemyObject();
			if (obj == null)
			{
				return;
			}
			bool area = IsAreaWeapon(weaponType);
			bool fire = weaponType == WeaponType.FireGun;
			ZombieOutline outline = obj.GetComponent<ZombieOutline>();
			if (outline != null)
			{
				if (area)
				{
					outline.Flash(BlastGlow, BlastGlowTime);
				}
				else if (fire)
				{
					outline.Flash(BurnGlow, BurnGlowTime);
				}
				else
				{
					outline.Flash(HitGlow, HitGlowTime);
				}
				// Fast weapons hit many times per second: keep the blood readable.
				if (!outline.TryConsumeBloodSlot())
				{
					return;
				}
			}
			if (fire || HasPreciseImpact(weaponType))
			{
				return;
			}
			Vector3 chest = enemy.GetPosition() + Vector3.up * 1.1f;
			Vector3 dir = hitForce;
			dir.y = 0f;
			if (dir.sqrMagnitude < 0.0001f)
			{
				dir = Vector3.up;
			}
			ModFX.BloodSpray(chest, dir, (!area) ? 1f : 2.2f);
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] hit effect failed: " + ex.Message);
		}
	}

	/// <summary>A bullet reached an enemy at this exact point.</summary>
	public static void OnBulletHitEnemy(Vector3 point, Vector3 bulletDirection)
	{
		try
		{
			ModFX.BloodSpray(point, bulletDirection, 1f);
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] blood effect failed: " + ex.Message);
		}
	}

	/// <summary>A bullet reached the ground, a wall or a prop.</summary>
	public static void OnBulletHitSurface(Vector3 point, Vector3 normal)
	{
		try
		{
			ModFX.SurfaceImpact(point, normal);
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] impact effect failed: " + ex.Message);
		}
	}

	/// <summary>
	/// An explosive went off. The game passes the spot where it puts its own
	/// explosion decal, which floats 0.1 above the ground.
	/// </summary>
	public static void OnExplosion(Vector3 decalPosition)
	{
		try
		{
			ModFX.Explosion(decalPosition + Vector3.down * 0.08f);
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] explosion effect failed: " + ex.Message);
		}
	}

	/// <summary>Survival: pays the gems for the wave that was just cleared.</summary>
	public static void OnSurvivalWaveCleared(int wave)
	{
		try
		{
			int gems = ModTuning.SurvivalGems(wave);
			GameApp.GetInstance().GetGameState().AddCrystal(gems);
			ModRuntime.NotifyGemsEarned(gems);
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] wave gems failed: " + ex.Message);
		}
	}

	public static void OnPointsEarned(int points)
	{
		ModRuntime.NotifyPointsEarned(points);
	}
}
