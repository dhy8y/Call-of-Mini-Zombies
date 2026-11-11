using System.Globalization;
using System.Threading;
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

	private const float GemPopupTime = 3f;

	private int gemPopup;

	private float gemPopupUntil;

	private GameUIScriptNew cachedUi;

	private float nextUiLookup;

	/// <summary>
	/// The game reads its config and saves with float.Parse, which follows the
	/// device language. On a French (or German, Spanish...) device "0.9" is not
	/// a valid number, the config fails to load and the start screen never
	/// responds. Forcing the neutral number format before anything loads fixes
	/// every such parse at once.
	/// </summary>
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void UseNeutralNumberFormat()
	{
		CultureInfo neutral = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = neutral;
		CultureInfo.DefaultThreadCurrentUICulture = neutral;
		Thread.CurrentThread.CurrentCulture = neutral;
		Thread.CurrentThread.CurrentUICulture = neutral;
	}

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

	/// <summary>Shows "+N GEMS" under the counter for a few seconds.</summary>
	public static void NotifyGemsEarned(int gems)
	{
		if (instance == null || gems <= 0)
		{
			return;
		}
		instance.gemPopup = gems;
		instance.gemPopupUntil = Time.unscaledTime + GemPopupTime;
	}

	private void Update()
	{
		ModFX.Tick(Time.deltaTime);
		UpdateInGameExtras();
		// Android's back button and back gesture arrive as the Escape key.
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			HandleBack();
		}
	}

	private float nextItemRefresh;

	private void UpdateInGameExtras()
	{
		if (!InGame())
		{
			ModSprint.Stop();
			return;
		}
		try
		{
			if (GameUIScriptNew.IsPaused)
			{
				ModSprint.UpdatePaused();
			}
			else
			{
				ModSprint.UpdateInGame(GameUIScriptNew.CanPlayerMove);
			}
			if (Time.unscaledTime >= nextItemRefresh)
			{
				nextItemRefresh = Time.unscaledTime + 0.25f;
				RefreshPowerupButtons();
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] in-game extras failed: " + ex.Message);
		}
	}

	/// <summary>
	/// The two powerup buttons only checked what the player could afford when a
	/// powerup was used, so gems earned during a game never lit them up. An
	/// empty slot is now bright as soon as its price can be paid, dim otherwise.
	/// </summary>
	private void RefreshPowerupButtons()
	{
		GameUIItemInfo info = cachedUi.itemInfo;
		GameState state = GameApp.GetInstance().GetGameState();
		if (info == null || state == null || info.itemLogo == null || !info.gameObject.activeInHierarchy)
		{
			return;
		}
		for (int i = 0; i < info.itemLogo.Length && i < info.isBuyItem.Length; i++)
		{
			if (!info.isBuyItem[i] || info.itemLogo[i] == null)
			{
				continue;
			}
			string frame = info.itemLogo[i].frameName_Accessor;
			if (string.IsNullOrEmpty(frame) || !frame.StartsWith("item_"))
			{
				continue;
			}
			ItemConfig conf = GameApp.GetInstance().GetGameConfig().GetItemConfig(Item.GetItemTypeByName(frame.Substring("item_".Length)));
			if (conf == null)
			{
				continue;
			}
			bool affordable = (!conf.isCrystalBuy) ? (state.GetCash() >= conf.price) : (state.GetCrystal() >= conf.price);
			info.itemLogo[i].color_Accessor = new Color(1f, 1f, 1f, (!affordable) ? 0.5f : 1f);
		}
	}

	// Names the game gives its "go back" buttons, most specific first: a panel
	// opened on top of a screen closes before the screen itself goes back.
	private static readonly string[] BackButtonNames = new string[5] { "Panel_Back_Button", "Credits_Back_Button", "Back_Button", "BackButton", "Back" };

	/// <summary>
	/// In menus, presses the back button of whatever is on screen. During a
	/// level the game already handles this key itself (it opens the pause menu).
	/// </summary>
	private void HandleBack()
	{
		if (InGame())
		{
			return;
		}
		try
		{
			GameObject cameraObject = GameObject.Find("TUI/TUICamera");
			Camera uiCamera = (cameraObject == null) ? null : cameraObject.GetComponent<Camera>();
			TUIControl[] controls = Object.FindObjectsOfType<TUIControl>();
			for (int n = 0; n < BackButtonNames.Length; n++)
			{
				for (int i = 0; i < controls.Length; i++)
				{
					TUIControl control = controls[i];
					if (control == null || control.name != BackButtonNames[n] || !control.gameObject.activeInHierarchy)
					{
						continue;
					}
					// Closed panels are parked off screen rather than disabled.
					if (uiCamera != null)
					{
						Vector3 view = uiCamera.WorldToViewportPoint(control.transform.position);
						if (view.x < 0f || view.x > 1f || view.y < 0f || view.y > 1f)
						{
							continue;
						}
					}
					control.PostEvent(control, TUIButtonClick.CommandClick, 0f, 0f, null);
					return;
				}
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogWarning("[Mod] back button failed: " + ex.Message);
		}
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
		string text = "POINTS  " + ModFormat.Short(points);
		DrawOutlined(new Rect(x, y, h * 1.2f, lineHeight), text, labelStyle, new Color(1f, 0.86f, 0.25f, 1f));

		float popupLeft = popupUntil - Time.unscaledTime;
		if (popupLeft > 0f && popupPoints > 0)
		{
			float width = labelStyle.CalcSize(new GUIContent(text)).x;
			float alpha = Mathf.Clamp01(popupLeft / (PopupTime * 0.5f));
			float rise = (1f - popupLeft / PopupTime) * lineHeight * 0.35f;
			DrawOutlined(new Rect(x + width + h * 0.02f, y - rise, h * 0.8f, lineHeight), "+" + ModFormat.Short(popupPoints), labelStyle, new Color(0.55f, 1f, 0.45f, alpha));
		}

		// Gems, right under the points, always up to date.
		y += lineHeight;
		string gemsText = "GEMS  " + ModFormat.Short(state.GetCrystal());
		DrawOutlined(new Rect(x, y, h * 1.2f, lineHeight), gemsText, labelStyle, new Color(0.4f, 0.9f, 1f, 1f));

		DrawSprintButton();

		int wave = ModTuning.CurrentSurvivalWave();
		float gemLeft = gemPopupUntil - Time.unscaledTime;
		if (gemLeft > 0f && gemPopup > 0)
		{
			float gemAlpha = Mathf.Clamp01(gemLeft / (GemPopupTime * 0.3f));
			string gemText = "+" + ModFormat.Short(gemPopup);
			float gemsWidth = labelStyle.CalcSize(new GUIContent(gemsText)).x;
			DrawOutlined(new Rect(x + gemsWidth + h * 0.02f, y, h * 0.8f, lineHeight), gemText, labelStyle, new Color(0.7f, 1f, 1f, gemAlpha));
		}
		if (wave > 0)
		{
			// Same multiplier the kills and pickups are paid with, so it moves as soon as a new wave starts.
			string bonus = "WAVE " + wave + "   GAINS x" + ModFormat.Multiplier(ModTuning.SurvivalMultiplier());
			DrawOutlined(new Rect(x, y + lineHeight, h * 1.4f, lineHeight), bonus, labelStyle, new Color(1f, 0.55f, 0.15f, 1f));
		}
	}

	private static Texture2D discTexture;

	/// <summary>Filled circle with a soft edge, used for the sprint button.</summary>
	private static Texture2D GetDisc()
	{
		if (discTexture != null)
		{
			return discTexture;
		}
		const int size = 128;
		discTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
		discTexture.wrapMode = TextureWrapMode.Clamp;
		Color32[] pixels = new Color32[size * size];
		float half = (float)(size - 1) * 0.5f;
		for (int py = 0; py < size; py++)
		{
			for (int px = 0; px < size; px++)
			{
				float dx = ((float)px - half) / half;
				float dy = ((float)py - half) / half;
				float d = Mathf.Sqrt(dx * dx + dy * dy);
				// Bright ring on the outside, lighter fill inside.
				float edge = Mathf.Clamp01((1f - d) * 24f);
				float ring = Mathf.Clamp01((d - 0.84f) * 16f);
				float alpha = edge * Mathf.Lerp(0.45f, 1f, ring);
				pixels[py * size + px] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)Mathf.RoundToInt(alpha * 255f));
			}
		}
		discTexture.SetPixels32(pixels);
		discTexture.Apply(false, true);
		return discTexture;
	}

	private void DrawSprintButton()
	{
		if (GameUIScriptNew.IsPaused)
		{
			Rect option = ModSprint.GetPauseOptionRect();
			Color before = GUI.color;
			GUI.color = new Color(0f, 0f, 0f, 0.7f);
			GUI.DrawTexture(option, Texture2D.whiteTexture);
			GUI.color = before;
			GUIStyle centred = new GUIStyle(labelStyle);
			centred.alignment = TextAnchor.MiddleCenter;
			string mode = (!ModSprint.HoldMode) ? "SPRINT :  TOGGLE" : "SPRINT :  HOLD";
			DrawOutlined(option, mode + "    (TAP TO CHANGE)", centred, new Color(1f, 0.75f, 0.2f, 1f));
			return;
		}
		Vector2 centre;
		float radius;
		ModSprint.GetButton(out centre, out radius);
		Rect rect = new Rect(centre.x - radius, centre.y - radius, radius * 2f, radius * 2f);
		Color previous = GUI.color;
		GUI.color = (!ModSprint.Active) ? new Color(0.75f, 0.6f, 0.2f, 0.55f) : new Color(1f, 0.55f, 0.1f, 0.95f);
		GUI.DrawTexture(rect, GetDisc());
		GUI.color = previous;
		GUIStyle small = new GUIStyle(smallStyle);
		small.alignment = TextAnchor.MiddleCenter;
		DrawOutlined(rect, "SPRINT", small, (!ModSprint.Active) ? new Color(1f, 0.9f, 0.6f, 0.9f) : Color.white);
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
