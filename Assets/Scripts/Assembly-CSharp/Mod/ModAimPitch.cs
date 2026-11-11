using UnityEngine;
using Zombie3D;

/// <summary>
/// Makes the player's upper body lean with the camera: back when aiming up,
/// forward when aiming down, so the gun points where the player is looking.
///
/// The spine bones are bent after the animation has posed the character, and
/// put back before the next frame's animation runs. That way the bend never
/// piles up, whether or not the current animation moves those bones.
/// </summary>
public class ModAimPitch : MonoBehaviour
{
	private static readonly string[] SpineBoneNames = new string[2] { "Bip01 Spine", "Bip01 Spine1" };

	private readonly Transform[] bones = new Transform[2];

	private readonly Quaternion[] saved = new Quaternion[2];

	private bool applied;

	private float currentPitch;

	private BaseCameraScript cameraScript;

	private float nextCameraLookup;

	public static void Attach(GameObject player)
	{
		if (player != null && player.GetComponent<ModAimPitch>() == null)
		{
			player.AddComponent<ModAimPitch>();
		}
	}

	private void Awake()
	{
		Transform[] all = GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < all.Length; i++)
		{
			for (int b = 0; b < SpineBoneNames.Length; b++)
			{
				if (bones[b] == null && all[i].name == SpineBoneNames[b])
				{
					bones[b] = all[i];
				}
			}
		}
	}

	private void Restore()
	{
		if (!applied)
		{
			return;
		}
		applied = false;
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] != null)
			{
				bones[i].localRotation = saved[i];
			}
		}
	}

	// Runs before the animation is evaluated for this frame.
	private void Update()
	{
		Restore();
	}

	private void OnDisable()
	{
		Restore();
	}

	// Runs after the animation has posed the character.
	private void LateUpdate()
	{
		// While the game is paused the animation does not re-pose the bones.
		if (Time.timeScale <= 0f)
		{
			return;
		}
		float target = 0f;
		GameScene scene = GameApp.GetInstance().GetGameScene();
		if (scene != null && scene.GamePlayingState == PlayingState.GamePlaying)
		{
			if (cameraScript == null && Time.unscaledTime >= nextCameraLookup)
			{
				nextCameraLookup = Time.unscaledTime + 0.5f;
				cameraScript = scene.GetCamera();
			}
			if (cameraScript != null)
			{
				target = Mathf.Clamp(cameraScript.angelV * ModTuning.AimBodyLeanFactor, -ModTuning.AimBodyLeanMax, ModTuning.AimBodyLeanMax);
			}
		}
		currentPitch = Mathf.Lerp(currentPitch, target, Mathf.Clamp01(Time.deltaTime * 12f));
		if (Mathf.Abs(currentPitch) < 0.05f)
		{
			return;
		}
		// Positive camera pitch means looking up, which is a backwards lean:
		// a negative turn around the character's right-hand axis.
		Vector3 axis = transform.right;
		float perBone = -currentPitch / (float)bones.Length;
		for (int i = 0; i < bones.Length; i++)
		{
			if (bones[i] == null)
			{
				continue;
			}
			saved[i] = bones[i].localRotation;
			bones[i].Rotate(axis, perBone, Space.World);
		}
		applied = true;
	}
}
