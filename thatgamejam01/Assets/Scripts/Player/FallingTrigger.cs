using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class FallingTrigger : MonoBehaviour
{
    [Header("Falling Effects")]
    public AudioClip fallSound;
    public AudioClip groanSound;
    [Range(0f, 1f)] public float vibrationIntensity = 0.5f;
    public float fallDuration = 2.0f; // Approx time until landing

    [Header("Rescue Setup")]
    public GameObject rescueHandSceneInstance; // Drag the SCENE OBJECT here, not a prefab
    public GameObject fakeHandPrefab; // 【新增】用于展示的假手预制体
    public Transform optimalViewingPoint; // 【新增】最佳观察点 (空物体)
    // public Transform handSpawnPoint; // Not needed if we use the scene object's position, or we can move it.
    // Let's assume the scene object is already placed where it should be, or we move it here?
    // User requested "Activate" so likely it's already placed.


    [Header("Sequence Timings")]
    public float timeBeforeGasp = 0.5f;
    public float timeAfterGasp = 1.0f; // Gap between gasp and splash
    public float timeAfterSplash = 2.0f; // Waiting in darkness before hand appears

    [Header("Audio")]
    public AudioClip gaspSound;
    public AudioClip splashSound;
    // Note: Lullaby is on the Hand Prefab

    [Header("Respawn")]
    public Transform respawnPoint;

    [Header("Hand Reference")]
    public Transform playerRightHand; // Assign in Inspector (e.g. from WallTouchSystem or hierarchy)

    private float _lastTriggerTime = -999f; // 【修改】使用时间戳代替布尔锁
    
    public LightingSwitcher lightingSwitcher;
    public GameObject whiteFlashCanvasPrefab;

    private void Start()
    {
        if (lightingSwitcher == null)
        {
            lightingSwitcher = FindObjectOfType<LightingSwitcher>();
            if (lightingSwitcher == null) Debug.LogError("[FallingTrigger] LightingSwitcher not found in scene!");
        }

        // Fix: Ensure Rescue Hand is disabled at start so it doesn't Mute audio
        if (rescueHandSceneInstance != null && rescueHandSceneInstance.activeSelf)
        {
            rescueHandSceneInstance.SetActive(false);
            Debug.Log("[FallingTrigger] Auto-disabled active RescueHandSceneInstance at Start.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 5秒防抖，防止短时间内重复触发，或者物理引擎的不稳定
        if (Time.time - _lastTriggerTime < 5.0f) return;

        if (other.CompareTag("Player"))
        {
            _lastTriggerTime = Time.time;
            StartCoroutine(FallRoutine(other.gameObject));
        }
    }

    IEnumerator FallRoutine(GameObject player)
    {
        // ... (Same as before) ...
        var characterController = player.GetComponent<CharacterController>();
        var inputs = player.GetComponent<StarterAssetsInputs>();
        var walker = player.GetComponent<RealisticWalker>(); 

        if (inputs != null)
        {
            inputs.movementEnabled = false;
            inputs.move = Vector2.zero;
            inputs.sprint = false;
        }

        yield return new WaitForSeconds(timeBeforeGasp);

        if (lightingSwitcher.isDark)
        {
            // Night Logic
            if (gaspSound != null) AudioSource.PlayClipAtPoint(gaspSound, player.transform.position);
            yield return new WaitForSeconds(timeAfterGasp);

            if (splashSound != null) AudioSource.PlayClipAtPoint(splashSound, player.transform.position);
            if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);

            yield return new WaitForSeconds(timeAfterSplash);

            // ... Auto Find Hand Logic omitted for brevity, logic remains same ...
            // (Assumed unmodified upper part of method)
            // ...

            // 0. Try Auto-Find Hand if missing
            if (playerRightHand == null)
            {
                 var wtsList = player.GetComponentsInChildren<WallTouchSystem>();
                 foreach (var wts in wtsList) { if (wts.currentHand == WallTouchSystem.HandSide.Right) { playerRightHand = wts.handModel; break; } }
                 if (playerRightHand == null) { var t = GameObject.FindGameObjectWithTag("RightHand"); if (t!=null) playerRightHand=t.transform; }
                 if (playerRightHand == null) { Transform t = player.transform.Find("RightHandContainer"); if (t!=null) playerRightHand=t; }
            }

            if (rescueHandSceneInstance != null)
            {
                if (rescueHandSceneInstance.activeSelf) rescueHandSceneInstance.SetActive(false);

                var handScript = rescueHandSceneInstance.GetComponent<RescueHandGaze>();
                if (handScript != null)
                {
                    handScript.playerRef = player;
                    handScript.playerHandRef = playerRightHand; 
                    // Re-get WTS
                    var wts = (playerRightHand!=null) ? playerRightHand.GetComponentInParent<WallTouchSystem>() : null;
                    if (wts==null && playerRightHand!=null) wts = playerRightHand.GetComponent<WallTouchSystem>();
                    handScript.wtsRef = wts; 
                    handScript.fakeHandPrefab = fakeHandPrefab; 
                    
                    if (respawnPoint != null) handScript.respawnPointRef = respawnPoint;
                }

                var fpsController = player.GetComponent<FirstPersonController>();
                if (fpsController != null)
                {
                    if (optimalViewingPoint != null) fpsController.ForceSetPositionAndRotation(optimalViewingPoint);
                    else fpsController.ForceLookAt(rescueHandSceneInstance.transform.position);
                }

                rescueHandSceneInstance.SetActive(true);
            }
        }
        else
        {
            // Day Logic
            ScreenFader fader = null;
            if (whiteFlashCanvasPrefab != null)
            {
                GameObject flashObj = Instantiate(whiteFlashCanvasPrefab);
                fader = flashObj.GetComponentInChildren<ScreenFader>();
                if (fader != null) yield return fader.FadeIn();
            }
            else yield return new WaitForSeconds(1.0f);
            
            if (player != null && respawnPoint != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = respawnPoint.position;
                player.transform.rotation = respawnPoint.rotation;
                if (cc != null) cc.enabled = true;
                
                var input = player.GetComponent<StarterAssetsInputs>();
                if (input != null) input.movementEnabled = true;
            }
            
            if (fader != null)
            {
                yield return fader.FadeOut();
                Destroy(fader.transform.root.gameObject);
            }
        }
    }

    // OnTriggerExit 移除：不再依赖退出事件重置状态，改用时间戳自动恢复
    // private void OnTriggerExit(Collider other) { ... }

    private void OnDisable()
    {
        // Safety: Stop vibration if disabled mid-fall
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }
}
