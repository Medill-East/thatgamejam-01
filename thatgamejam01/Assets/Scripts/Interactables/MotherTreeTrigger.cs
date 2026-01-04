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
            StartCoroutine(SequenceWrapper());
        }
        else
        {
            Debug.LogError("[MotherTreeTrigger] Cannot trigger sequence, LightingSwitcher is missing.");
        }
    }

    private IEnumerator SequenceWrapper()
    {
        // 1. Find correct "Start" point to enforce teleport destination
        GameObject startObj = GameObject.FindGameObjectWithTag("Start");
        if (startObj != null)
        {
            // Explicitly update the respawn point in the manager so the usage calls to default logic use THIS point.
            lightingSwitcher.SetRespawnPoint(startObj.transform.position, startObj.transform.rotation);
        }
        else
        {
            Debug.LogWarning("[MotherTreeTrigger] Could not find object with tag 'Start'. LightingSwitcher will use default/current start position.");
        }

        // 2. Call the standard switch with Force Teleport enabled
        // targetState = true (Dark), forceTeleport = true
        // 【关键修复】必须在 LightingSwitcher 上启动协程！
        // 因为本脚本(MotherTreeTrigger)挂载了 DayNightActiveToggle，切换到黑夜瞬间会被 disable
        // 如果协程跑在这里，就会被腰斩，导致 Fader 卡住无法 FadeOut
        yield return lightingSwitcher.StartCoroutine(lightingSwitcher.PerformWorldSwitch(true, true));

        // 3. Unlock input
        lightingSwitcher.SetInputBlocked(false);

        // 4. Disable self
        gameObject.SetActive(false);
    }
}
