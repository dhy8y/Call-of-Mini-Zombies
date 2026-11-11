using UnityEngine;
using Zombie3D;

/// <summary>
/// One object that lives for the whole session. It keeps the mod's effects
/// ticking and draws the points counter in the top-left corner during a game.
/// </summary>
public class ModRuntime : MonoBehaviour
{
	private const float PopupTime = 0.9f;

	private static ModRuntime instance;

	private GUIStyle labelStyle;

	private GUIStyle smallStyle;

	private int styleHeight = -1;

	private int popupPoints;

	private float popupUntil;

	private GameUIScriptNew cachedUi;

	private float nextUiLookup;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (instance != null)
		{
			return;
		}
		GameObject obj = new GameObject("ModRuntime");
		DontDestroyOnLoad(obj);
		instance = obj.AddComponent<ModRuntime>();
	}

	/// <summary>Shows a short "+N" next to the counter. Gains landing close together add up.</summary>
	public static void NotifyPointsEarned(int points)
	{
		if (instance == null || points <= 0)
		{
			return;
		}
		if (Time.unscaledTime < instance.popupUntil)
		{
			instance.popupPoints = Mathf.Min(ModTuning.MaxPointsPerGame, instance.popupPoints + points);
		}
		else
		{
			instance.popupPoints = points;
		}
		instance.popupUntil = Time.unscaledTime + PopupTime;
	}

	private void Update()
	{
		ModFX.Tick(Time.deltaTime);
	}

	/// <summary>True while a level is being played (not in menus, loading or the shop).</summary>
	private bool InGame()
	{
		// Looking the HUD up by name is slow, so only retry a few times per second.
		if (cachedUi == null && Time.unscaledTime >= nextUiLookup)
		{
			nextUiLookup = Time.unscaledTime + 0.5f;
			cachedUi = GameUIScriptNew.GetGameUIScript();
		}
		if (cachedUi == null || !cachedUi.uiInited)
		{
			return false;
		}
		GameScene scene = GameApp.GetInstance().GetGameScene();
		return scene != null && scene.GetPlayer() != null;
	}

	private void EnsureStyles()
	{
		if (labelStyle != null && styleHeight == Screen.height)
		{
			return;
		}
		styleHeight = Screen.height;
		labelStyle = new GUIStyle(GUI.skin.label);
		labelStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt((float)Screen.height * 0.042f));
		labelStyle.fontStyle = FontStyle.Bold;
		labelStyle.alignment = TextAnchor.UpperLeft;
		labelStyle.wordWrap = false;
		labelStyle.richText = false;
		labelStyle.normal.textColor = Color.white;
		smallStyle = new GUIStyle(labelStyle);
		smallStyle.fontSize = Mathf.Max(11, Mathf.RoundToInt((float)Screen.height * 0.028f));
	}

	private void OnGUI()
	{
		if (Event.current.type != EventType.Repaint || !InGame())
		{
			return;
		}
		GameState state = GameApp.GetInstance().GetGameState();
		if (state == null)
		{
			return;
		}
		EnsureStyles();

		float h = Screen.height;
		// Below the avatar and health bar that already sit in the top-left corner.
		float x = h * 0.02f;
		float y = h * 0.19f;
		float lineHeight = (float)labelStyle.fontSize * 1.35f;

		int points = state.loot_cash;
		string text = "POINTS  " + points.ToString("N0");
		DrawOutlined(new Rect(x, y, h * 1.2f, lineHeight), text, labelStyle, new Color(1f, 0.86f, 0.25f, 1f));

		float popupLeft = popupUntil - Time.unscaledTime;
		if (popupLeft > 0f && popupPoints > 0)
		{
			float width = labelStyle.CalcSize(new GUIContent(text)).x;
			float alpha = Mathf.Clamp01(popupLeft / (PopupTime * 0.5f));
			float rise = (1f - popupLeft / PopupTime) * lineHeight * 0.35f;
			DrawOutlined(new Rect(x + width + h * 0.02f, y - rise, h * 0.8f, lineHeight), "+" + popupPoints.ToString("N0"), labelStyle, new Color(0.55f, 1f, 0.45f, alpha));
		}

		int wave = ModTuning.CurrentSurvivalWave();
		if (wave > 0)
		{
			string bonus = "BONUS x" + ModTuning.SurvivalMultiplier().ToString((wave <= 12) ? "0.00" : "N0");
			DrawOutlined(new Rect(x, y + lineHeight, h * 1.2f, lineHeight), bonus, smallStyle, new Color(1f, 1f, 1f, 0.9f));
		}
	}

	/// <summary>Text with a dark edge so it stays readable over any background.</summary>
	private static void DrawOutlined(Rect rect, string text, GUIStyle style, Color color)
	{
		float o = Mathf.Max(1f, (float)style.fontSize * 0.06f);
		Color previous = GUI.color;
		GUI.color = new Color(0f, 0f, 0f, color.a * 0.85f);
		GUI.Label(new Rect(rect.x - o, rect.y, rect.width, rect.height), text, style);
		GUI.Label(new Rect(rect.x + o, rect.y, rect.width, rect.height), text, style);
		GUI.Label(new Rect(rect.x, rect.y - o, rect.width, rect.height), text, style);
		GUI.Label(new Rect(rect.x, rect.y + o, rect.width, rect.height), text, style);
		GUI.color = color;
		GUI.Label(rect, text, style);
		GUI.color = previous;
	}
}
