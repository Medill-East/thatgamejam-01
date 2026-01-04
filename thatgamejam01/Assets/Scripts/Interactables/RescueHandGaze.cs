using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

public class RescueHandGaze : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform playerCameraOverride; // 【新增】强制指定玩家摄像机 (解决 Camera.main 找错的问题)
    public Transform gazeTargetOverride; // 【新增】手动指定看向的目标点 (如果不指定则默认为物体根节点)
    public Transform handVisualRoot; // The visual part of the hand
    public Light handLight;
    public AudioSource lullabySource;
    public GameObject whiteFlashCanvasPrefab;
    public AudioClip rescueSuccessSound;

    [Header("Settings")]
    public float maxGazeAngle = 30f; // Degrees. How directly must we look?
    public float rescueDuration = 3.0f; // seconds of gazing to win
    public float maxFailureTime = 10.0f; // seconds until game over if not saved (after spawn)
    public float minLightIntensity = 0.5f;
    public float maxLightIntensity = 5.0f;
    public float minVolume = 0.1f;
    public float maxVolume = 1.0f;
    public float maxHandExtension = 0.8f; // 【新增】手臂最大伸出距离 (米)
    public Vector3 reachingRotationOffset = Vector3.zero; // 【新增】伸出手时的旋转修正

    // Runtime Data injected by FallingTrigger
    [HideInInspector] public GameObject playerRef;
    [HideInInspector] public Transform respawnPointRef;
    [HideInInspector] public Transform playerHandRef; // The player's right hand
    [HideInInspector] public WallTouchSystem wtsRef; // 【新增】

    [HideInInspector] public GameObject fakeHandPrefab; // 【新增】假手预制体

    private float _timeSinceSpawn = 0f;
    private bool _isRescued = false;
    private bool _isGameOver = false;

    // Fader Smoothing
    private float _currentFaderAlpha = 0f;

    // Movement state
    private Vector3 _initialHandPos;
    private Vector3 _initialPlayerHandLocalPos;
    private Quaternion _initialPlayerHandLocalRot;
    
    // Smoothing & Hysteresis
    private Vector3 _smoothedTargetPos;
    private bool _wasLooking = false;
    
    // Fake Hand State
    private GameObject _realHandObject;
    private GameObject _spawnedFakeHand;
    
    // Distance Params
    public float maxDistance = 3.0f; // Distance where effects are 0
    public float successDistance = 0.2f; // Touch threshold
    public float fadeStartDist = 1.0f; // Start fading only when within xm
    
    // Hand Animation
    public float handMoveSpeed = 2.5f; // Increased from 1.0f
    public float handReturnSpeed = 2.0f;
    public float playerHandTriggerDistance = 1.2f; // Distance at which player hand starts reaching

    private string _lastRayHitName = ""; // Debug
    
    // Audio Isolation
    private List<AudioSource> _mutedSources = new List<AudioSource>();

    private void MuteOtherAudio()
    {
        _mutedSources.Clear();
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        foreach (var source in allSources)
        {
            // Skip our Lullaby
            if (source == lullabySource) continue;
            // Skip our Success Sound (if it were an audio source on this object, though usually it's ClipAtPoint)
            if (source.gameObject == gameObject) continue; 
            
            if (!source.mute)
            {
                source.mute = true;
                _mutedSources.Add(source);
            }
        }
    }

    private void RestoreAudio()
    {
        foreach (var source in _mutedSources)
        {
            if (source != null) source.mute = false;
        }
        _mutedSources.Clear();
    }

    private void OnEnable()
    {
        Debug.Log($"[RescueHandGaze] OnEnable. PlayerRef is assigned: {(playerRef != null)}");

        // Disable WallTouchSystem to prevent conflict
        if (wtsRef != null) wtsRef.enabled = false;
        
        // 【新增】确保视觉部分被重新启用 (因为我们在 SuccessSequence 里关闭了它)
        if (handVisualRoot != null)
        {
            handVisualRoot.gameObject.SetActive(true);
            // 额外修复：如果之前是通过禁用 component 隐藏的，现在要恢复
            foreach(var r in handVisualRoot.GetComponentsInChildren<Renderer>(true)) 
            {
                r.enabled = true;
            }
        }

        // Reset State on Enable

        _timeSinceSpawn = 0f;
        _isRescued = false;
        _isGameOver = false;
        _currentFaderAlpha = 0f;

        // Default Light Init
        if (handLight != null)
        {
            handLight.enabled = true; // Ensure component is on

            handLight.intensity = minLightIntensity;
        }

        if (lullabySource != null)
        {
            lullabySource.volume = minVolume;
            lullabySource.Play();
        }

        // Store initial positions for reset
        _initialHandPos = transform.position;

        if (playerHandRef != null)
        {
            // --- Fake Hand Logic ---
            if (fakeHandPrefab != null)
            {
                // 1. 记录真手
                _realHandObject = playerHandRef.gameObject;
                
                // 2. 生成假手 (作为真手的兄弟节点，或者父节点的子节点)
                _spawnedFakeHand = Instantiate(fakeHandPrefab, playerHandRef.parent);
                
                // 3. 对齐位置旋转
                _spawnedFakeHand.transform.localPosition = playerHandRef.localPosition;
                _spawnedFakeHand.transform.localRotation = playerHandRef.localRotation;
                _spawnedFakeHand.transform.localScale = playerHandRef.localScale;
                
                // 4. 隐藏真手
                _realHandObject.SetActive(false);
                
                // 5. 将 logic reference 指向假手，这样后续动画代码就会驱动假手
                playerHandRef = _spawnedFakeHand.transform;
                
                Debug.Log("[RescueHandGaze] Swapped to Fake Hand.");
            }
            
            _initialPlayerHandLocalPos = playerHandRef.localPosition;
            _initialPlayerHandLocalRot = playerHandRef.localRotation;
        }

        // Instantiate/Get Fader for this session
        if (whiteFlashCanvasPrefab != null && _faderInstance == null)
        {
            GameObject flashObj = Instantiate(whiteFlashCanvasPrefab);
            _faderInstance = flashObj.GetComponentInChildren<ScreenFader>();
        }

        if (_faderInstance != null)
        {
            _faderInstance.SetAlpha(0f); // Start invisible
        }

        // Auto-assign Camera Override if missing
        if (playerCameraOverride == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (camObj != null)
            {
                playerCameraOverride = camObj.transform;
            }
        }

        // Hierarchy Check: Ensure visuals move with us
        if (handVisualRoot != null && !handVisualRoot.IsChildOf(transform))
        {
            Debug.LogWarning($"[RescueHandGaze] handVisualRoot '{handVisualRoot.name}' is NOT a child of '{name}'. Reparenting strictly to ensure movement works.");
            // Record world position before reparenting to keep it in place visually if that was intended, 
            // but we usually want it at local zero. Let's just parenting.
            handVisualRoot.SetParent(transform, true); 
        }

        MuteOtherAudio();
    }

    private void OnDisable()
    {
        RestoreAudio();

        // Restore WallTouchSystem
        if (wtsRef != null) wtsRef.enabled = true;
        
        // Restore Real Hand
        if (_realHandObject != null)
        {
            _realHandObject.SetActive(true);
            playerHandRef = _realHandObject.transform; // Reset ref back to real hand just in case
            _realHandObject = null;
        }
        
        // Destroy Fake Hand
        if (_spawnedFakeHand != null)
        {
            Destroy(_spawnedFakeHand);
            _spawnedFakeHand = null;
        }
    }

    private ScreenFader _faderInstance;

    private void LateUpdate() // 【修改】改为 LateUpdate 以确保在相机移动完成后才计算
    {
        if (_isRescued || _isGameOver) return;
        
        if (playerRef == null) 
        {
            return;
        }

        _timeSinceSpawn += Time.deltaTime;

        if (_timeSinceSpawn > maxFailureTime)
        {
            Debug.Log("[RescueHandGaze] Time limit reached. Game Over.");
            TriggerGameOver();
            return;
        }

        HandleGazeLogic();
    }

    private void HandleGazeLogic()
    {
        // 0. Ensure Physics doesn't fight us
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic) rb.isKinematic = true;

        // 1. Determine "Eye Position" (Robust Method)
        // If Camera is lagging (Cinemachine), we use the Player's physical position + height offset.
        Vector3 eyePos;
        Vector3 lookDir;
        
        Camera mainCam = null;
        if (playerCameraOverride != null) mainCam = playerCameraOverride.GetComponent<Camera>(); 
        if (mainCam == null && playerRef != null) mainCam = playerRef.GetComponentInChildren<Camera>();
        if (mainCam == null) mainCam = Camera.main;

        if (playerRef != null)
        {
            // Assume Average Eye Height ~ 1.6m
            eyePos = playerRef.transform.position + Vector3.up * 1.6f;
            
            // For rotation, we still trust the camera's forward or the player's forward?
            // If Camera is lagging position, it might lag rotation too. 
            // BUT, usually FPC rotates the camera instantaneously.
            if (mainCam != null) lookDir = mainCam.transform.forward;
            else lookDir = playerRef.transform.forward;
        }
        else if (mainCam != null)
        {
            eyePos = mainCam.transform.position;
            lookDir = mainCam.transform.forward;
        }
        else
        {
            return;
        }

        // 【修改】优先使用手动指定的 gazeTargetOverride，如果没有则使用 transform.position
        Vector3 rawTargetPos = transform.position;
        if (gazeTargetOverride != null)
        {
            rawTargetPos = gazeTargetOverride.position;
        }

        // Simple smoothing
        _smoothedTargetPos = Vector3.Lerp(_smoothedTargetPos, rawTargetPos, Time.deltaTime * 10f);
        if (Vector3.Distance(_smoothedTargetPos, rawTargetPos) > 2.0f) _smoothedTargetPos = rawTargetPos;

        // Calculate based on Robust Eye Pos
        Vector3 toHand = (_smoothedTargetPos - eyePos).normalized;
        
        // Debug
        // Debug.DrawRay(eyePos, lookDir * 5f, Color.yellow);
        // Debug.DrawLine(eyePos, _smoothedTargetPos, Color.cyan);

        float angle = Vector3.Angle(lookDir, toHand);

        // 2. Determine "Looking At" status
        float activeThreshold = _wasLooking ? (maxGazeAngle * 1.5f) : maxGazeAngle;
        bool isLooking = angle < activeThreshold;
        
        // Raycast Check (Backup) using EyePos
        string debugRayHitObj = "None"; 
        Ray ray = new Ray(eyePos, lookDir);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance * 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            debugRayHitObj = hit.collider.name;
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                isLooking = true;
            }
        }
        _lastRayHitName = debugRayHitObj;
        
        _wasLooking = isLooking;
        
        // 3. Distance & Movement Logic
        float currentDist = maxDistance; // Default if no hand
        
        // Calculate raw distance first for logic checks
        Vector3 targetPoint = (playerHandRef != null) ? playerHandRef.position : eyePos;
        float rawDistanceToCam = Vector3.Distance(transform.position, eyePos);

        if (isLooking)
        {
             // Move Rescue Hand towards Camera (EyePos)
             Vector3 dirToCam = (eyePos - transform.position).normalized;
             transform.position += dirToCam * handMoveSpeed * Time.deltaTime;
             
             // Move Player Hand towards Rescue Hand (World Space Direction)
             // Only if Rescue Hand is close enough
             if (playerHandRef != null && rawDistanceToCam < playerHandTriggerDistance)
             {
                 // 1. 旋转手掌朝向目标
                 Vector3 directionToTarget = (_smoothedTargetPos - playerHandRef.position).normalized;
                 if (directionToTarget != Vector3.zero)
                 {
                     Quaternion baseLookRot = Quaternion.LookRotation(directionToTarget, Vector3.up);
                     // 【新增】应用旋转偏移，让玩家可以调整“手心朝向”或“手腕抬起”的感觉
                     Quaternion finalRot = baseLookRot * Quaternion.Euler(reachingRotationOffset);
                     
                     // 混合旋转
                     playerHandRef.rotation = Quaternion.Slerp(playerHandRef.rotation, finalRot, Time.deltaTime * 5f);
                 }

                 // 2. 向目标移动
                 playerHandRef.position += directionToTarget * (handMoveSpeed * 1.5f) * Time.deltaTime; // Slightly slower than before relative to mom

                 // 3. 【修复】基于球形距离的限制，而不是单纯 Z 轴
                 float dist = Vector3.Distance(playerHandRef.localPosition, _initialPlayerHandLocalPos);
                 if (dist > maxHandExtension)
                 {
                     Vector3 dirFromStart = (playerHandRef.localPosition - _initialPlayerHandLocalPos).normalized;
                     playerHandRef.localPosition = _initialPlayerHandLocalPos + dirFromStart * maxHandExtension;
                 }
             }
        }
        else
        {
            // Return to start
            transform.position = Vector3.MoveTowards(transform.position, _initialHandPos, handReturnSpeed * Time.deltaTime);
            
            if (playerHandRef != null)
            {
               playerHandRef.localPosition = Vector3.MoveTowards(playerHandRef.localPosition, _initialPlayerHandLocalPos, handReturnSpeed * Time.deltaTime);
               playerHandRef.localRotation = Quaternion.Slerp(playerHandRef.localRotation, _initialPlayerHandLocalRot, handReturnSpeed * Time.deltaTime);
            }
        }

        // 4. Calculate effect distance (Recalculate accurately)
        // distance between hands if possible
        if (playerHandRef != null) targetPoint = playerHandRef.position; // Ensure target is player hand
        currentDist = Vector3.Distance(handVisualRoot.position, targetPoint);
        
        // 5. Map to Intensity (Closer = Stronger)
        // Map [MaxDist, SuccessDist] -> [0, 1]
        float progress01 = Mathf.InverseLerp(maxDistance, successDistance, currentDist);
        
        if (handLight != null)
        {
            handLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, progress01);
            handLight.range = Mathf.Lerp(5f, 15f, progress01);
        }

        if (lullabySource != null)
        {
            float targetVol = Mathf.Lerp(minVolume, maxVolume, progress01);
            
            // Try to set via SmartAudioSource first
            var smartAudio = lullabySource.GetComponent<SmartAudioSource>();
            if (smartAudio != null)
            {
                smartAudio.externalVolumeMult = targetVol;
                // SmartAudioSource will calculate final volume based on this * orientation
            }
            else
            {
                lullabySource.volume = targetVol;
            }
        }
        
        if (_faderInstance != null)
        {
            // Custom fade logic: Start later, then curve up
            // User requested "Last 2s" -> Only trigger when very close
            
            
            
            if (currentDist < fadeStartDist)
            {
                // Remap distance [fadeStartDist, successDistance] -> [0, 1]
                float faderProgress = Mathf.InverseLerp(fadeStartDist, successDistance, currentDist);
                
                // Very steep curve
                float targetAlpha = faderProgress * faderProgress; 
                
                _currentFaderAlpha = Mathf.Lerp(_currentFaderAlpha, targetAlpha, Time.deltaTime * 10f);
            }
            else
            {
                _currentFaderAlpha = Mathf.Lerp(_currentFaderAlpha, 0f, Time.deltaTime * 5f);
            }
            
            _faderInstance.SetAlpha(_currentFaderAlpha);
        }

        // 6. Check Success (Touch)
        if (progress01 >= 1.0f) // distance <= successDistance
        {
            Debug.Log("[RescueHandGaze] Hand Touched! Success.");
            StartCoroutine(SuccessSequence());
        }
    }

    private void TriggerGameOver()
    {
        _isGameOver = true;
        Debug.Log("Game Over: Failed to grab the hand in time.");
        
        // Reload Scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator SuccessSequence()
    {
        _isRescued = true;
        
        // 1. Maximize effects
        if (handLight != null) handLight.intensity = maxLightIntensity;
        if (_faderInstance != null) _faderInstance.SetAlpha(1f); // Ensure full white

        // 【新增】在全白瞬间立即隐藏手，防止它们“残留”到传送后的画面
        if (handVisualRoot != null)
        {
             // 关键修复：如果 handVisualRoot 就是挂载脚本的物体本身，SetActivate(false) 会直接杀掉这个 Coroutine，导致卡在白屏
             if (handVisualRoot.gameObject == gameObject)
             {
                 // 只是禁用渲染器，不禁用物体
                 foreach(var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
             }
             else
             {
                 handVisualRoot.gameObject.SetActive(false);
             }
        }
        
        if (playerHandRef != null) playerHandRef.gameObject.SetActive(false); // Fake hand is separate, safe to disable

        // Wait a moment in full white
        yield return new WaitForSeconds(0.5f);

        // 3. Teleport
        if (playerRef != null && respawnPointRef != null)
        {
            CharacterController cc = playerRef.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            playerRef.transform.position = respawnPointRef.position;
            // playerRef.transform.rotation = respawnPointRef.rotation; // Keep rotation logic if needed, or remove if user prefers looking somewhere else

            if (cc != null) cc.enabled = true;

            // Re-enable inputs
            var input = playerRef.GetComponent<StarterAssetsInputs>();
            if (input != null)
            {
                // Reset inputs
                input.movementEnabled = true; // Re-enable movement
                input.move = Vector2.zero;
                input.look = Vector2.zero;
                input.jump = false;
                input.sprint = false;
            }
        }

        // Play Success Sound (At respawn point, so player hears it)
        if (rescueSuccessSound != null)
        {
            AudioSource.PlayClipAtPoint(rescueSuccessSound, respawnPointRef.position);
        }

        // 4. Fade Out
        if (_faderInstance != null)
        {
            yield return _faderInstance.FadeOut();
            Destroy(_faderInstance.transform.root.gameObject);
            _faderInstance = null; // Clear ref so we create new one next time
        }

        // Cleanup: Disable self instead of Destroy, so it can be reused
        
        // Reset Positions explicitly here too?
        transform.position = _initialHandPos;
        if (playerHandRef != null)
        {
            playerHandRef.localPosition = _initialPlayerHandLocalPos;
            playerHandRef.localRotation = _initialPlayerHandLocalRot;
        }
        
        gameObject.SetActive(false); 
    }

    /*
    private void OnGUI()
    {
        // Debug Display
        if (_isRescued || _isGameOver) return;
        if (playerRef == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"Rescue Hand Debug:");
        
        Camera mainCam = null;
        if (playerCameraOverride != null) mainCam = playerCameraOverride.GetComponent<Camera>();
        if (mainCam == null && playerRef != null) mainCam = playerRef.GetComponentInChildren<Camera>();
        if (mainCam == null) mainCam = Camera.main;

        if (mainCam != null)
        {
             GUILayout.Label($"Cam: {mainCam.name}");
             Vector3 targetPos = transform.position;
             Renderer[] renderers = GetComponentsInChildren<Renderer>();
             if (renderers.Length > 0)
             {
                 Bounds combinedBounds = renderers[0].bounds;
                 for (int i = 1; i < renderers.Length; i++) combinedBounds.Encapsulate(renderers[i].bounds);
                 targetPos = combinedBounds.center;
             }
        
             Vector3 toHand = (targetPos - mainCam.transform.position).normalized;
             float angle = Vector3.Angle(mainCam.transform.forward, toHand);
             GUILayout.Label($"Angle: {angle:F1} (Max: {maxGazeAngle})");
             
             // Recalc looking for display
             bool isLooking = angle < maxGazeAngle;
             // Raycast verify
             Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
             if (Physics.Raycast(ray, out RaycastHit hit, maxDistance * 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
             {
                 if (hit.transform == transform || hit.transform.IsChildOf(transform)) 
                     isLooking = true;
             }
             GUILayout.Label($"Is Looking: {isLooking}");
             
             GUILayout.Label($"Ray Hit: {_lastRayHitName}");
             GUILayout.Label($"Target (Bounds): {targetPos}");
             string state = isLooking ? "APPROACHING" : "RETURNING";
             GUILayout.Label($"State: {state} (Speed: {handMoveSpeed})");


             Vector3 targetPoint = (playerHandRef != null) ? playerHandRef.position : mainCam.transform.position;
             float dist = Vector3.Distance(handVisualRoot.position, targetPoint);
             GUILayout.Label($"Dist to Target: {dist:F2} (Success: {successDistance})");
        }
        
        GUILayout.EndArea();
    }
    */
}
