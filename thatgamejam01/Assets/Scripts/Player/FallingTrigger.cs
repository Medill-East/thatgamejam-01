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
    public GameObject rescueHandPrefab;
    public Transform handSpawnPoint;

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
            // Reset move input to stop walking
            inputs.move = Vector2.zero;
            inputs.jump = false;
            inputs.sprint = false;
            // Disable input processing if possible, or just overwrite in update. 
            // For StarterAssets, we might just set cursor locked state or hack the input.
            // Simplest way: Disable the CharacterController temporarily or modify the input script. 
            // We'll trust disabling CharacterController stops movement physics, but input might still rotate camera.
            // Let's assume we want to keep camera control (looking around) but stop movement.
        }

        // Disable CC to stop physics movement calculation issues if we want to freeze them later, 
        // but for falling we want them to fall.
        // If we want to disable WASD, we might need a flag in Player Logic.
        // For now, we rely on the player falling physically.
        
        // 2. Play Fall Sound
        if (fallSound != null)
        {
            AudioSource.PlayClipAtPoint(fallSound, player.transform.position); // Simple play
        }

        // 3. Gamepad Haptics
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(vibrationIntensity, vibrationIntensity);
        }

        // Wait for landing (approximate or detect ground)
        yield return new WaitForSeconds(fallDuration);

        // 4. Landing / Groan
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f); // Stop vibration
        }

        if (groanSound != null)
        {
            AudioSource.PlayClipAtPoint(groanSound, player.transform.position);
        }

        // 5. Spawn Rescue Hand
        if (rescueHandPrefab != null && handSpawnPoint != null)
        {
            // GameObject hand = Instantiate(rescueHandPrefab, handSpawnPoint.position, handSpawnPoint.rotation);
            // Pass necessary data to hand if needed
            rescueHandPrefab.SetActive(true);
        }

        Debug.Log("Player has fallen. Rescue Hand spawned.");
        
        // Reset trigger state so it can happen again if the player falls again later
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
