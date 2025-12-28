using System.Collections;
using UnityEngine;
using StarterAssets;

public class RescueHandInteract : MonoBehaviour, IInteractable
{
    [Header("Respawn Settings")]
    public Transform respawnPoint; // Assign or find automatically
    public GameObject whiteFlashCanvasPrefab; // Prefab with ScreenFader

    [Header("Audio")]
    public AudioClip rescueSound;

    private bool _isInteracting = false;

    // Optional: Auto-find respawn point on Start if not assigned
    void Start()
    {
        // Try to find the checkpoint system if it exists, otherwise rely on manual assignment
        if (respawnPoint == null)
        {
            GameObject deadZone = GameObject.FindObjectOfType<DeadZone>()?.gameObject;
            if (deadZone != null)
            {
                respawnPoint = deadZone.GetComponent<DeadZone>().respawnPoint;
            }
        }
    }

    void OnEnable()
    {
        _isInteracting = false;
        
        // Restart the looping help sound if it exists
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void OnInteract()
    {
        if (_isInteracting) return;
        _isInteracting = true;

        StartCoroutine(RescueSequence());
    }

    IEnumerator RescueSequence()
    {
        // 1. Play Rescue Sound
        if (rescueSound != null)
        {
            AudioSource.PlayClipAtPoint(rescueSound, transform.position);
        }

        // 2. Spawn White Flash
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

        // 3. Teleport Player
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
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
                // Unset whatever flags falling set
            }
        }

        // 4. Fade Out
        // 4. Fade Out
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

        // 5. Cleanup
        gameObject.SetActive(false);
    }
}
