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

    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            _hasTriggered = true;
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
            inputs.jump = false;
            inputs.sprint = false;
        }

        // Disable CC to stop physics movement calculation issues if we want to freeze them later, 
        // but for falling we want them to fall.
        // If we want to disable WASD, we might need a flag in Player Logic.
        // For now, we rely on the player falling physically.
        
        // 2. Sequence
        
        // Initial Fall delay (maybe air wind?)
        yield return new WaitForSeconds(timeBeforeGasp);

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
                    playerRightHand = wts.handContainer;
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
        _hasTriggered = false;
    }

    private void OnDisable()
    {
        // Safety: Stop vibration if disabled mid-fall
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }
}
