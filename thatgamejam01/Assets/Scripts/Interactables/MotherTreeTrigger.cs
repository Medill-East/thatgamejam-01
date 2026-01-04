using System.Collections;
using UnityEngine;

public class MotherTreeTrigger : MonoBehaviour
{
    [Header("Settings")]
    public float requiredDuration = 3.0f;

    [Header("Dependencies")]
    public LightingSwitcher lightingSwitcher;

    private float _timer = 0f;
    private bool _isPlayerInside = false;
    private bool _hasTriggered = false;

    private void Start()
    {
        if (lightingSwitcher == null)
        {
            lightingSwitcher = FindObjectOfType<LightingSwitcher>();
            if (lightingSwitcher == null) Debug.LogError("[MotherTreeTrigger] LightingSwitcher not found in scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = true;
            _timer = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = false;
            _timer = 0f;
        }
    }

    private void Update()
    {
        if (_hasTriggered) return;
        if (!_isPlayerInside) return;

        _timer += Time.deltaTime;

        if (_timer >= requiredDuration)
        {
            TriggerSequence();
        }
    }

    private void TriggerSequence()
    {
        _hasTriggered = true;

        if (lightingSwitcher != null)
        {
            // Find start pos
            Vector3 targetPos = Vector3.zero;
            Quaternion targetRot = Quaternion.identity;
            
            GameObject startObj = GameObject.FindGameObjectWithTag("Start");
            if (startObj != null)
            {
                targetPos = startObj.transform.position;
                targetRot = startObj.transform.rotation;
            }
            else
            {
                // Fallback to current player pos? Or just keep current respawn.
                // Assuming Start exists is safer for logic coherence.
                 Debug.LogWarning("[MotherTreeTrigger] Start tag not found.");
            }

            // Hand off logic to Persistent Controller
            lightingSwitcher.TriggerStorySequence(targetPos, targetRot);
            
            // Disable self immediately
            gameObject.SetActive(false);
        }
    }
}
