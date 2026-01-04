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
        // 1. Disable Movement
        var characterController = player.GetComponent<CharacterController>();
        var inputs = player.GetComponent<StarterAssetsInputs>();
        var walker = player.GetComponent<RealisticWalker>(); // Optional: Stop footstep FX

        if (inputs != null)
        {
            // Disable movement via our new flag
            inputs.movementEnabled = false;
            
            // Reset current input values
            inputs.move = Vector2.zero;
            inputs.sprint = false;
        }

        // Disable CC to stop physics movement calculation issues if we want to freeze them later, 
        // but for falling we want them to fall.
        // If we want to disable WASD, we might need a flag in Player Logic.
        // For now, we rely on the player falling physically.
        
        // 2. Sequence
        
        // Initial Fall delay (maybe air wind?)
        yield return new WaitForSeconds(timeBeforeGasp);


        if (lightingSwitcher.isDark)
        {
            //黑天执行以下逻辑：玩家大叫bark -> 落地声 -> recue hand
            
            // Gasp
            if (gaspSound != null) AudioSource.PlayClipAtPoint(gaspSound, player.transform.position);
        
            yield return new WaitForSeconds(timeAfterGasp);

            // Splash (landing?)
            if (splashSound != null) AudioSource.PlayClipAtPoint(splashSound, player.transform.position);

            // Stop Vibration
            if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);

            yield return new WaitForSeconds(timeAfterSplash);

            // 0. Try Auto-Find Hand if missing
            if (playerRightHand == null)
            {
                // A. Try finding by WallTouchSystem Component (Most Robust)
                var wallTouchSystems = player.GetComponentsInChildren<WallTouchSystem>();
                foreach (var wts in wallTouchSystems)
                {
                    if (wts.currentHand == WallTouchSystem.HandSide.Right)
                    {
                        playerRightHand = wts.handModel;
                        Debug.Log("[FallingTrigger] Auto-found Right Hand via WallTouchSystem.");
                        break;
                    }
                }

                // B. Try finding by Tag (User Request)
                if (playerRightHand == null)
                {
                    var taggedObj = GameObject.FindGameObjectWithTag("RightHand"); // Tag must be added!
                    if (taggedObj != null) 
                    {
                        playerRightHand = taggedObj.transform;
                        Debug.Log("[FallingTrigger] Auto-found Right Hand via Tag 'RightHand'.");
                    }
                }

                // C. Try finding by Path (User specific structure)
                if (playerRightHand == null)
                {
                    // Path: PlayerRelated-MainCamera-RightHandLogic-RightHandContainer
                    // Assuming 'player' is 'PlayerRelated' (Root) or close to it.
                    // Let's try searching specifically for "MainCamera/RightHandLogic/RightHandContainer"
                    Transform t = player.transform.Find("MainCamera/RightHandLogic/RightHandContainer");
                    if (t == null) t = player.transform.Find("RightHandContainer"); // Fallback check
                    
                    if (t != null)
                    {
                        playerRightHand = t;
                        Debug.Log("[FallingTrigger] Auto-found Right Hand via Hierarchy Path.");
                    }
                }
            }

                // 3. Activate Hand
                if (rescueHandSceneInstance != null)
                {
                    // 【重要修复】确保先关闭再开启，以触发 OnEnable 重置逻辑
                    // 如果上次玩家走了但手还在，或者手已经是激活状态，直接 SetActive(true) 不会有任何反应
                    if (rescueHandSceneInstance.activeSelf)
                    {
                            rescueHandSceneInstance.SetActive(false);
                    }

                    // Update position if needed? User didn't ask, but let's assume it's fixed in scene.
                    // If we still want to move it to a spawn point, we can.
                    // But user said: "Simply SetActive is enough?". Let's trust that.
                    
                    // Pass Data BEFORE activating to ensure Start/OnEnable has data?
                    // Actually, GetComponent works on inactive objects if we specify includeInactive=true, 
                    // OR strictly speaking we just get it from the field.
            
                    var handScript = rescueHandSceneInstance.GetComponent<RescueHandGaze>();
                    if (handScript != null)
                    {
                        handScript.playerRef = player;
                        handScript.playerHandRef = playerRightHand; // Pass the hand ref
                
                        // 【新增】传递 WallTouchSystem 引用，以便在救援时禁用它
                        var wts = playerRightHand.GetComponentInParent<WallTouchSystem>(); 
                        if (wts == null) wts = playerRightHand.GetComponent<WallTouchSystem>(); // 也可以直接挂在 Hand 上
                        // 如果之前是在 FallingTrigger 里自动找到的，也可以重用那个逻辑，但这里重新获取比较安全
                
                        handScript.wtsRef = wts; 
                        handScript.fakeHandPrefab = fakeHandPrefab; // 【新增】传递假手

                        Debug.Log($"[FallingTrigger] Assigned PlayerRef to hand: {player.name}");
                
                        if (respawnPoint != null)
                        {
                            handScript.respawnPointRef = respawnPoint;
                        }
                        else
                        {
                            var dz = FindObjectOfType<DeadZone>();
                            if (dz != null) handScript.respawnPointRef = dz.respawnPoint;
                        }
                    }
                    else
                    {
                        Debug.LogError("[FallingTrigger] Assigned Scene Object does NOT have RescueHandGaze component!");
                    }
            
                    // 【修改】先传送玩家，再激活手，确保第一帧逻辑就是正确的
                    var fpsController = player.GetComponent<FirstPersonController>();
                    if (fpsController != null)
                    {
                        if (optimalViewingPoint != null)
                        {
                            // 优先使用传送：直接传送到最佳观察点
                            fpsController.ForceSetPositionAndRotation(optimalViewingPoint);
                            Debug.Log("[FallingTrigger] Teleported player to Optimal Viewing Point.");
                        }
                        else
                        {
                            // 降级方案：原地扭头
                            fpsController.ForceLookAt(rescueHandSceneInstance.transform.position);
                        }
                    }

                    rescueHandSceneInstance.SetActive(true);
            
                    Debug.Log("Player has fallen. Rescue Hand Activated.");
                }
                else
                {
                     Debug.LogError("[FallingTrigger] rescueHandSceneInstance is NOT assigned!");
                }
                // Removed: _hasTriggered = false; // We probably don't want to trigger this multiple times per fall?
                // Actually, if we reset, we might need to reset the Hand too.
                // For now, keep the trigger logic as is (it sets _hasTriggered=true at start).
                // If we want it repeatable, we need to ensure the Hand resets itself on Disable/Enable.
                // _hasTriggered = false; // 【修改】不再重置，防止玩家呆在 Trigger 里反复触发    
        }
        else
        {
            //如果玩家白天坠落悬崖，播放白光渐出
            ScreenFader fader = null;
            if (whiteFlashCanvasPrefab != null)
            {
                GameObject flashObj = Instantiate(whiteFlashCanvasPrefab);
            
                // Try to find fader on the root or any child
                fader = flashObj.GetComponentInChildren<ScreenFader>();
            
                if (fader != null)
                {
                    yield return fader.FadeIn();
                }
            }
            else
            {
                // Fallback delay if no visual
                yield return new WaitForSeconds(1.0f);
            }
            
            // 白光播放完毕后 teleport玩家到最近的复活点
            if (player != null && respawnPoint != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = respawnPoint.position;
                player.transform.rotation = respawnPoint.rotation;

                if (cc != null) cc.enabled = true;
            
                // Re-enable inputs if they were disabled (assumes StarterAssetsInputs)
                var input = player.GetComponent<StarterAssetsInputs>();
                if (input != null)
                {
                    input.movementEnabled = true;
                    // Unset whatever flags falling set
                }
            }
            
            //白光fadeout & 清理游戏物体
            if (fader != null)
            {
                yield return fader.FadeOut();
            
                // Cleanup the whole canvas object, not just the fader script
                // If fader is on a child, destroying fader.gameObject only deletes the child
                // We want to delete the root that we instantiated.
                // Since we didn't store the root "flashObj" in a wider scope, we can infer it
                // or just rely on fader.transform.root (if it wasn't parented) or just destroy fader's root.
            
                // Safe approach: Destroy the root of the fader (which should be the prefab instance)
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
