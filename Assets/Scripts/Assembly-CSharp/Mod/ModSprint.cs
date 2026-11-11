using UnityEngine;

/// <summary>
/// Sprint: an on-screen button to the left of the fire stick (Left Shift on
/// a keyboard). Two modes, chosen from the pause menu and remembered:
///   - toggle: one press starts sprinting, the next one stops it
///   - hold:   sprint only while the button is held
/// </summary>
public static class ModSprint
{
	private const string HoldModeKey = "mod_sprint_hold";

	private static bool loaded;

	private static bool holdMode;

	/// <summary>True while the player is sprinting.</summary>
	public static bool Active { get; private set; }

	public static bool HoldMode
	{
		get
		{
			Load();
			return holdMode;
		}
		set
		{
			Load();
			holdMode = value;
			Active = false;
			PlayerPrefs.SetInt(HoldModeKey, value ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	/// <summary>What the player's move speed is multiplied by right now.</summary>
	public static float SpeedMultiplier
	{
		get
		{
			return (!Active) ? 1f : ModTuning.SprintSpeedMultiplier;
		}
	}

	private static void Load()
	{
		if (!loaded)
		{
			loaded = true;
			holdMode = PlayerPrefs.GetInt(HoldModeKey, 0) == 1;
		}
	}

	public static void Stop()
	{
		Active = false;
	}

	/// <summary>Sprint button on screen: centre and radius in pixels, origin at the top-left.</summary>
	public static void GetButton(out Vector2 centre, out float radius)
	{
		float h = Screen.height;
		radius = h * 0.075f;
		centre = new Vector2((float)Screen.width - h * 0.48f, h * 0.86f);
	}

	/// <summary>Sprint mode row shown under the pause menu, origin at the top-left.</summary>
	public static Rect GetPauseOptionRect()
	{
		float h = Screen.height;
		float width = h * 0.72f;
		float height = h * 0.085f;
		return new Rect(((float)Screen.width - width) * 0.5f, h * 0.875f, width, height);
	}

	private static bool Inside(Vector2 screenPoint, Vector2 centre, float radius)
	{
		// Input positions have their origin at the bottom-left.
		Vector2 p = new Vector2(screenPoint.x, (float)Screen.height - screenPoint.y);
		return (p - centre).sqrMagnitude <= radius * radius;
	}

	private static bool Inside(Vector2 screenPoint, Rect rect)
	{
		return rect.Contains(new Vector2(screenPoint.x, (float)Screen.height - screenPoint.y));
	}

	/// <summary>Reads the sprint button and key. Called every frame during a level.</summary>
	public static void UpdateInGame(bool canMove)
	{
		Load();
		if (!canMove)
		{
			if (holdMode)
			{
				Active = false;
			}
			return;
		}
		Vector2 centre;
		float radius;
		GetButton(out centre, out radius);
		// A little slack around the button makes it easier to hit with a thumb.
		float hitRadius = radius * 1.25f;

		bool pressedNow = Input.GetKeyDown(KeyCode.LeftShift);
		bool held = Input.GetKey(KeyCode.LeftShift);
		for (int i = 0; i < Input.touchCount; i++)
		{
			Touch touch = Input.GetTouch(i);
			if (!Inside(touch.position, centre, hitRadius))
			{
				continue;
			}
			if (touch.phase == TouchPhase.Began)
			{
				pressedNow = true;
			}
			if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
			{
				held = true;
			}
		}
		if (Input.touchCount == 0 && Cursor.lockState == CursorLockMode.None && Inside(Input.mousePosition, centre, hitRadius))
		{
			if (Input.GetMouseButtonDown(0))
			{
				pressedNow = true;
			}
			if (Input.GetMouseButton(0))
			{
				held = true;
			}
		}
		if (holdMode)
		{
			Active = held;
		}
		else if (pressedNow)
		{
			Active = !Active;
		}
	}

	/// <summary>Lets the player switch mode from the pause menu. Called every frame while paused.</summary>
	public static void UpdatePaused()
	{
		Load();
		Rect rect = GetPauseOptionRect();
		bool tapped = false;
		for (int i = 0; i < Input.touchCount; i++)
		{
			Touch touch = Input.GetTouch(i);
			if (touch.phase == TouchPhase.Began && Inside(touch.position, rect))
			{
				tapped = true;
			}
		}
		if (Input.touchCount == 0 && Input.GetMouseButtonDown(0) && Inside(Input.mousePosition, rect))
		{
			tapped = true;
		}
		if (tapped)
		{
			HoldMode = !holdMode;
		}
	}
}
