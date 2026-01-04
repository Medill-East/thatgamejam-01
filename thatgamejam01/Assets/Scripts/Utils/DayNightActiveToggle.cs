using UnityEngine;

public class DayNightActiveToggle : MonoBehaviour
{
    [Header("Activity Settings")]
    [Tooltip("Should this object be active during the Day?")]
    public bool activeInDay = true;

    [Tooltip("Should this object be active during the Night?")]
    public bool activeInNight = false;

    private LightingSwitcher _switcher;

    private void Awake()
    {
        // Find the switcher
        _switcher = FindObjectOfType<LightingSwitcher>();
        
        if (_switcher != null)
        {
            // Subscribe immediately so we catch events even if we are disabled later
            _switcher.OnDayNightChange += OnDayNightChange;
        }
        else
        {
            Debug.LogWarning($"[DayNightActiveToggle] No LightingSwitcher found for {gameObject.name}");
        }
    }

    private void Start()
    {
        // Initial Sync
        if (_switcher != null)
        {
            UpdateState(_switcher.isDark);
        }
    }

    private void OnDestroy()
    {
        // Cleanup to prevent memory leaks or errors
        if (_switcher != null)
        {
            _switcher.OnDayNightChange -= OnDayNightChange;
        }
    }

    private void OnDayNightChange(bool isDark)
    {
        UpdateState(isDark);
    }

    private void UpdateState(bool isDark)
    {
        bool shouldBeActive = isDark ? activeInNight : activeInDay;
        
        // Only set if different to avoid overhead
        if (gameObject.activeSelf != shouldBeActive)
        {
            gameObject.SetActive(shouldBeActive);
        }
    }
}
