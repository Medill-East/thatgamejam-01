using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

public class RescueHandGaze : MonoBehaviour
{
    [Header("Dependencies")]
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

    // Runtime Data injected by FallingTrigger
    [HideInInspector] public GameObject playerRef;
    [HideInInspector] public Transform respawnPointRef;
    [HideInInspector] public Transform playerHandRef; // The player's right hand

    private float _timeSinceSpawn = 0f;
    private bool _isRescued = false;
    private bool _isGameOver = false;

    // Fader Smoothing
    private float _currentFaderAlpha = 0f;

    // Movement state
    private Vector3 _initialHandPos;
    private Vector3 _initialPlayerHandLocalPos;
    private Quaternion _initialPlayerHandLocalRot;
    
    // Distance Params
    public float handReachDistance = 0.5f; 
    public float maxDistance = 3.0f; // Distance where effects are 0
    public float successDistance = 0.2f; // Touch threshold
    
    // Hand Animation
    public float handMoveSpeed = 1.0f;
    public float handReturnSpeed = 2.0f;

    private void OnEnable()
    {
        Debug.Log($"[RescueHandGaze] OnEnable. PlayerRef is assigned: {(playerRef != null)}");

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
    }

    private ScreenFader _faderInstance;

    // Removed Start() as Setup is now in OnEnable logic is safer for SetActive/Deactive flows
    // private void Start() { ... }

    private void Update()
    {
        if (_isRescued || _isGameOver) return;
        
        if (playerRef == null) 
        {
            // Debug.LogWarning("[RescueHandGaze] Waiting for PlayerRef..."); // Spammy
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
        // 1. Calculate Gaze alignment
        Camera mainCam = Camera.main;
        if (mainCam == null) 
        {
            Debug.LogError("[RescueHandGaze] Camera.main is NULL!");
            return;
        }

        Vector3 toHand = (handVisualRoot.position - mainCam.transform.position).normalized;
        Vector3 playerLook = mainCam.transform.forward;

        float angle = Vector3.Angle(playerLook, toHand);
        
        // Debug Log (Throttle this if needed, or just watch console)
        // Debug.Log($"[RescueHandGaze] Angle: {angle}, Progress: {_currentRescueProgress}");

        // 2. Determine "Looking At" status
        bool isLooking = angle < maxGazeAngle;
        
        // 3. Distance & Movement Logic
        float currentDist = maxDistance; // Default if no hand
        
        if (isLooking)
        {
             // Move Rescue Hand towards Camera
             Vector3 dirToCam = (mainCam.transform.position - transform.position).normalized;
             transform.position += dirToCam * handMoveSpeed * Time.deltaTime;
             
             // Move Player Hand towards Rescue Hand
             if (playerHandRef != null)
             {
                 playerHandRef.localPosition += Vector3.forward * (handMoveSpeed * 2.0f) * Time.deltaTime; 
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

        // 4. Calculate effect distance
        // If we have a hand ref, use distance between hands. Else distance to camera.
        Vector3 targetPoint = (playerHandRef != null) ? playerHandRef.position : mainCam.transform.position;
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
            // "Screen white logic: Start slow then fast, only after certain distance"
            
            float fadeStartDist = maxDistance * 0.6f; // Start fading when 60% closer (or custom value)
            
            // Remap distance [fadeStartDist, successDistance] -> [0, 1]
            float faderProgress = Mathf.InverseLerp(fadeStartDist, successDistance, currentDist);
            
            // Apply easing (Quadratic or Cubic) for "Slow then Fast"
            float targetAlpha = faderProgress * faderProgress * faderProgress; // Cubic Ease-In
            
            // Smooth the alpha to prevent flickering
            _currentFaderAlpha = Mathf.Lerp(_currentFaderAlpha, targetAlpha, Time.deltaTime * 5f);
            
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
}
