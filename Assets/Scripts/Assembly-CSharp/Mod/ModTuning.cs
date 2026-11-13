using System;
using UnityEngine;
using Zombie3D;

/// <summary>
/// Every balance number of the mod lives here, so the economy can be retuned
/// without hunting through the game code.
/// </summary>
public static class ModTuning
{
	/// <summary>Shown in the corner of the menus, to tell which build is installed.</summary>
	public const string BuildLabel = "MOD BUILD 11";

	// ---- Prices ----------------------------------------------------------

	/// <summary>Weapon upgrades and avatar HP upgrades cost this many times less.</summary>
	public const float UpgradePriceDivisor = 20f;

	/// <summary>
	/// Shop prices cost this many times less: weapons and characters bought with
	/// cash, ammo refills, and every powerup (cash or gems, never below 1).
	/// </summary>
	public const float ShopPriceDivisor = 20f;

	// ---- Points per kill -------------------------------------------------

	/// <summary>An elite version of a zombie is worth this many extra points (capped at MaxZombiePoints).</summary>
	public const int EliteBonusPoints = 1;

	public const int MinZombiePoints = 1;

	public const int MaxZombiePoints = 5;

	/// <summary>
	/// Bosses and hunted preys pay in proportion to their life: this many points
	/// for each "basic zombie worth" of HP they spawn with.
	/// </summary>
	public const float BossPointsPerZombieLife = 5f;

	// ---- Pickups ---------------------------------------------------------

	public const int GoldPickupPoints = 10;

	public const int BigGoldPickupPoints = 50;

	// ---- Gems (crystals) -------------------------------------------------

	/// <summary>Campaign: clearing day 1 pays 1 gem, day 2 pays 2, day 3 pays 3... with no limit.</summary>
	public static int CampaignGems(int dayCleared)
	{
		return Mathf.Max(1, dayCleared);
	}

	/// <summary>
	/// Survival: each wave pays more gems than the one before it.
	/// The step grows with the wave: +1 per wave up to wave 9, +2 from wave 10,
	/// +3 from wave 15, +5 from wave 20 and +7 from wave 25 on.
	/// Wave 1 = 1, wave 9 = 9, wave 10 = 11, wave 15 = 22, wave 20 = 39, wave 25 = 66.
	/// </summary>
	public static int SurvivalGems(int waveCleared)
	{
		int gems = 0;
		for (int w = 1; w <= waveCleared; w++)
		{
			gems += SurvivalGemStep(w);
		}
		return Mathf.Max(1, gems);
	}

	private static int SurvivalGemStep(int wave)
	{
		if (wave >= 25) return 7;
		if (wave >= 20) return 5;
		if (wave >= 15) return 3;
		if (wave >= 10) return 2;
		return 1;
	}

	// ---- Camera and aiming -----------------------------------------------

	/// <summary>Lowest camera pitch in degrees (negative looks down).</summary>
	public const float CameraMinPitch = -70f;

	/// <summary>Highest camera pitch in degrees (positive looks up).</summary>
	public const float CameraMaxPitch = 70f;

	/// <summary>How much of the camera pitch the character's upper body follows.</summary>
	public const float AimBodyLeanFactor = 0.8f;

	/// <summary>Largest upper-body lean in degrees, either way.</summary>
	public const float AimBodyLeanMax = 55f;

	/// <summary>Move speed while sprinting, compared with normal speed.</summary>
	public const float SprintSpeedMultiplier = 1.6f;

	// ---- Dead bodies -----------------------------------------------------

	/// <summary>Seconds a dead body stays on the ground before it dissolves.</summary>
	public const float CorpseGroundTime = 5f;

	/// <summary>Seconds after death when the enemy stops counting (the original removal time).</summary>
	public const float CorpseListRemovalTime = 2f;

	/// <summary>Seconds the pixel dissolve takes.</summary>
	public const float CorpseDissolveTime = 1.2f;

	// ---- Campaign days ---------------------------------------------------

	public const int CampaignFirstDayZombies = 10;

	public const int CampaignExtraZombiesPerDay = 3;

	/// <summary>From this day on, days stop getting more zombies.</summary>
	public const int CampaignLastGrowingDay = 50;

	/// <summary>Most zombies on the map at once during a campaign day.</summary>
	public const int CampaignMaxZombiesOnMap = 25;

	/// <summary>Seconds between two zombies of the same group appearing (campaign).</summary>
	public const float CampaignSpawnInterval = 0.12f;

	/// <summary>Waits between groups of zombies in a campaign day are multiplied by this.</summary>
	public const float CampaignGroupDelayScale = 0.35f;

	/// <summary>Day 1 has 10 zombies, day 2 has 13, day 3 has 16... up to day 50 (157), then stays there.</summary>
	public static int CampaignZombieCount(int day)
	{
		int growingDays = Mathf.Clamp(day, 1, CampaignLastGrowingDay) - 1;
		return CampaignFirstDayZombies + CampaignExtraZombiesPerDay * growingDays;
	}

	// ---- Survival pacing -------------------------------------------------

	/// <summary>False: waves have no countdown and the game never ends on "time up".</summary>
	public const bool SurvivalHasTimeLimit = false;

	/// <summary>Seconds before the very first zombies appear.</summary>
	public const float SurvivalFirstSpawnDelay = 2f;

	/// <summary>Longest wait before a new wave starts spawning.</summary>
	public const float SurvivalMaxWaveStartDelay = 3f;

	/// <summary>Waits between groups of zombies inside a wave are multiplied by this.</summary>
	public const float SurvivalGroupDelayScale = 0.4f;

	/// <summary>Seconds between two zombies of the same group appearing (survival).</summary>
	public const float SurvivalSpawnInterval = 0.15f;

	/// <summary>A survival wave never has more zombies than this (bosses come on top).</summary>
	public const int SurvivalMaxZombiesPerWave = 40;

	/// <summary>Most zombies on the map at once in survival (the original game allowed 8).</summary>
	public static int SurvivalMaxZombiesOnMap(int wave)
	{
		float limit = 16f;
		if (wave >= 15)
		{
			limit *= 1.5f;
		}
		if (wave >= 25)
		{
			limit *= 1.5f;
		}
		return Mathf.RoundToInt(limit);
	}

	public static float SurvivalSpawnDelay(float originalDelay, int wave, bool firstGroupOfWave)
	{
		if (firstGroupOfWave)
		{
			if (wave <= 1)
			{
				return SurvivalFirstSpawnDelay;
			}
			return Mathf.Min(originalDelay, SurvivalMaxWaveStartDelay);
		}
		return originalDelay * SurvivalGroupDelayScale;
	}

	// ---- Ammo ------------------------------------------------------------

	/// <summary>Refilling a weapon that has no bullet left costs nothing.</summary>
	public static bool IsAmmoFree(Weapon weapon)
	{
		return weapon != null && weapon.BulletCount <= 0;
	}

	// ---- Survival mode ---------------------------------------------------

	/// <summary>From wave 8 on, every wave multiplies survival earnings by this once more.</summary>
	public const float SurvivalWaveMultiplier = 1.25f;

	/// <summary>Keeps one reward far away from the int limit at very high waves.</summary>
	public const int MaxPointsPerReward = 20000000;

	/// <summary>Cap on the points counted in one game (same cap the wallet already uses).</summary>
	public const int MaxPointsPerGame = 99999999;

	public static int DiscountUpgradePrice(int price)
	{
		if (price <= 0)
		{
			return price;
		}
		return Mathf.Max(1, Mathf.RoundToInt((float)price / UpgradePriceDivisor));
	}

	public static int DiscountShopPrice(int price)
	{
		if (price <= 0)
		{
			return price;
		}
		return Mathf.Max(1, Mathf.RoundToInt((float)price / ShopPriceDivisor));
	}

	/// <summary>1 for the weakest zombie, up to 5 for the toughest.</summary>
	public static int BasePointsFor(EnemyType type)
	{
		switch (type)
		{
		case EnemyType.E_ZOMBIE:
		case EnemyType.E_NURSE:
			return 1;
		case EnemyType.E_DOG:
		case EnemyType.E_BOOMER:
		case EnemyType.E_DILO:
			return 2;
		case EnemyType.E_SWAT:
		case EnemyType.E_POLICE:
		case EnemyType.E_VELOCI:
			return 3;
		case EnemyType.E_TANK:
		case EnemyType.E_HELL_FIRER:
		case EnemyType.E_SUPER_DINO:
			return 5;
		default:
			return 1;
		}
	}

	/// <summary>
	/// Points an enemy is worth before the survival multiplier.
	/// zombieLifeHp is the HP of a basic zombie at the current difficulty.
	/// </summary>
	public static int PointsFor(EnemyType type, bool isElite, bool isBossOrPrey, double maxHp, float zombieLifeHp)
	{
		int points = BasePointsFor(type);
		if (isElite)
		{
			points += EliteBonusPoints;
		}
		points = Mathf.Clamp(points, MinZombiePoints, MaxZombiePoints);
		if (isBossOrPrey && zombieLifeHp > 0f)
		{
			double lives = maxHp / (double)zombieLifeHp;
			int bossPoints = ClampReward(lives * (double)BossPointsPerZombieLife);
			points = Mathf.Max(points, bossPoints);
		}
		return points;
	}

	/// <summary>Wave currently being played in survival mode, 0 in every other mode.</summary>
	public static int CurrentSurvivalWave()
	{
		GameApp app = GameApp.GetInstance();
		GameState state = app.GetGameState();
		GameScene scene = app.GetGameScene();
		if (state == null || scene == null || state.gameMode != GameMode.Instance)
		{
			return 0;
		}
		if (scene.ArenaTrigger_Instance == null)
		{
			return 0;
		}
		return Mathf.Max(1, scene.ArenaTrigger_Instance.CurrentWave);
	}

	/// <summary>Survival multiplier for waves 1 to 7, set by hand.</summary>
	private static readonly double[] SurvivalEarlyMultipliers = new double[7] { 1.0, 2.0, 4.0, 10.0, 15.0, 20.0, 25.0 };

	/// <summary>
	/// x1 on wave 1, then x2, x4, x10, x15, x20, x25 on wave 7, then x1.25 more
	/// for every wave after that. x1 outside survival.
	/// </summary>
	public static double SurvivalMultiplier()
	{
		return SurvivalMultiplierForWave(CurrentSurvivalWave());
	}

	public static double SurvivalMultiplierForWave(int wave)
	{
		if (wave <= 1)
		{
			return 1.0;
		}
		int last = SurvivalEarlyMultipliers.Length;
		if (wave <= last)
		{
			return SurvivalEarlyMultipliers[wave - 1];
		}
		return SurvivalEarlyMultipliers[last - 1] * Math.Pow(SurvivalWaveMultiplier, wave - last);
	}

	public static int ApplySurvivalMultiplier(int basePoints)
	{
		if (basePoints <= 0)
		{
			return 0;
		}
		return ClampReward((double)basePoints * SurvivalMultiplier());
	}

	public static int PickupPoints(bool big)
	{
		return ApplySurvivalMultiplier((!big) ? GoldPickupPoints : BigGoldPickupPoints);
	}

	private static int ClampReward(double value)
	{
		if (double.IsNaN(value) || value <= 0.0)
		{
			return 0;
		}
		if (value >= (double)MaxPointsPerReward)
		{
			return MaxPointsPerReward;
		}
		return Math.Max(1, (int)Math.Round(value));
	}
}
