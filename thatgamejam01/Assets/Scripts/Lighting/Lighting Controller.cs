using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class LightingSwitcher : MonoBehaviour
{
    [Header("World Objects")]
    public GameObject waterVFX;        // 拖入你的水特效物体

    [Header("Toggle Features")]
    public bool teleportOnSwitch = true;

    [Header("Core Settings")]
    public Light sunLight;
    public Material daySkybox;
    public Camera mainCamera;
    public bool isDark = true;

    [Header("Teleport Settings")]
    public Transform playerTransform;
    private Vector3 startPosition;
    private Quaternion startRotation;

    [Header("Fade Visuals")]
    public CanvasGroup fadeCanvasGroup;
    public TextMeshProUGUI yearHintText;
    public float fadeInTime = 0.2f;
    public float fadeOutTime = 0.4f;
    [Tooltip("全黑状态下的停留时间")]
    public float stayDuration = 0.5f; // 【新增】

    [Header("Year Messages")]
    [TextArea(3, 10)]
    public string pastText = "20 年前";
    [TextArea(3, 10)]
    public string presentText = "20 年后";

    [Header("Environment Fog")]
    public float darkFogDensity = 0.5f;
    public Color darkFogColor = Color.black;

    public WindChime[] windChimes;

    [Header("UI")]
    public GameObject switchPromptUI; // 【新增】F键提示UI

    public bool canSwitchPerformWorldSwitch = true;
    private bool _isSwitching = false; // 【新增】是否正在切换中

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (playerTransform != null)
        {
            startPosition = playerTransform.position;
            startRotation = playerTransform.rotation;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (yearHintText != null) SetYearTextAlpha(0);

        windChimes = FindObjectsOfType<WindChime>(); 
        
        ApplyLighting();
    }

    void Update()
    {
        // UI Visibility Control
        if (switchPromptUI != null)
        {
            // Show only if: Can Perform Switch AND Not Currently Switching AND Not Input Blocked
            bool showPrompt = canSwitchPerformWorldSwitch && !_isSwitching && !inputBlocked;
            if (switchPromptUI.activeSelf != showPrompt)
            {
                switchPromptUI.SetActive(showPrompt);
            }
        }

        if (inputBlocked) return;

        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton4))
        {
            // Also check !isSwitching to prevent spamming F
            if (canSwitchPerformWorldSwitch && !_isSwitching)
            {
                StopAllCoroutines();
                StartCoroutine(PerformWorldSwitch());
            }
        }
    }

    public IEnumerator PerformWorldSwitch(bool? targetState = null, bool forceTeleport = false)
    {
        _isSwitching = true; // Start Transition

        if (yearHintText != null)
            yearHintText.text = isDark ? pastText : presentText;

        float elapsed = 0;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInTime);
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = alpha;
            if (yearHintText != null) SetYearTextAlpha(alpha);
            yield return null;
        }

        if (targetState.HasValue)
        {
            isDark = targetState.Value;
        }
        else
        {
            isDark = !isDark; // Default toggle behavior
        }

        if (teleportOnSwitch || forceTeleport) TeleportPlayer();

        ApplyLighting();

        foreach (var windchime in windChimes)
        {
            windchime.ResetWindChime();
        }
        
        // Use exposed parameter instead of hardcoded 0.5f
        yield return new WaitForSeconds(stayDuration);

        elapsed = 0;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (elapsed / fadeOutTime));
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = alpha;
            if (yearHintText != null) SetYearTextAlpha(alpha);
            yield return null;
        }

        _isSwitching = false; // End Transition
    }

    void SetYearTextAlpha(float alpha)
    {
        if (yearHintText == null) return;
        Color c = yearHintText.color;
        c.a = alpha;
        yearHintText.color = c;
    }

    public void SetRespawnPoint(Vector3 pos, Quaternion rot)
    {
        startPosition = pos;
        startRotation = rot;
    }

    void TeleportPlayer()
    {
        if (playerTransform == null) return;
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        playerTransform.position = startPosition;
        playerTransform.rotation = startRotation;
        
        // Ensure physics reset?
        if (cc != null) cc.enabled = true;
    }

    public event System.Action<bool> OnDayNightChange; // Event for other scripts

    void ApplyLighting()
    {
        // --- 新增：控制水特效显示 ---
        if (waterVFX != null)
        {
            waterVFX.SetActive(!isDark);
        }

        if (isDark)
        {
            if (sunLight) sunLight.intensity = 0f;
            RenderSettings.skybox = null;
            if (mainCamera)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.black;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = darkFogDensity;
            RenderSettings.fogColor = darkFogColor;
        }
        else
        {
            if (sunLight) sunLight.intensity = 1.0f;
            RenderSettings.skybox = daySkybox;
            if (mainCamera) mainCamera.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.fog = false;
        }
        DynamicGI.UpdateEnvironment();
        
        // Notify listeners
        OnDayNightChange?.Invoke(isDark);
    }

    // --- New Methods for Game Flow ---

    public bool inputBlocked = false;   // Set to true in Inspector if you want to start locked

    public void SetInputBlocked(bool blocked)
    {
        inputBlocked = blocked;
    }

    public void ForceSetNight()
    {
        StopAllCoroutines(); // Stop any switching in progress
        isDark = true;
        ApplyLighting();
        if (yearHintText != null) yearHintText.text = pastText;
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f; // Ensure fade is cleared
    }

    // 【新增】专门用于 MotherTree 的剧情切换序列，确保输入在切换后解锁
    // 将逻辑移到这里可以防止触发器物体被禁用导致协程中断
    public void TriggerStorySequence(Vector3 respawnPos, Quaternion respawnRot)
    {
        StartCoroutine(StorySequenceRoutine(respawnPos, respawnRot));
    }

    private IEnumerator StorySequenceRoutine(Vector3 respawnPos, Quaternion respawnRot)
    {
        // 1. Set Respawn
        SetRespawnPoint(respawnPos, respawnRot);

        // 2. Perform Switch (Force Night, Force Teleport)
        // We yield wait for it to finish
        yield return StartCoroutine(PerformWorldSwitch(true, true));

        // 3. Unlock Input - This is safe here because LightingSwitcher handles it
        SetInputBlocked(false);
    }
}