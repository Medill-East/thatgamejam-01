using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro; // 如果使用普通 Text，请删除此行并将 TextMeshProUGUI 改为 Text

public class LightingSwitcher : MonoBehaviour
{
    [Header("Toggle Features")]
    [Tooltip("如果勾选，按下 F 切换时会传送到起点；如果不勾选，则原地切换。")]
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

    [Header("Year Messages")]
    public string pastText = "20 年前";
    public string presentText = "20 年后";

    [Header("Environment Fog")]
    public float darkFogDensity = 0.5f;
    public Color darkFogColor = Color.black;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // 记录游戏启动时玩家的位置
        if (playerTransform != null)
        {
            startPosition = playerTransform.position;
            startRotation = playerTransform.rotation;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (yearHintText != null) SetYearTextAlpha(0);

        ApplyLighting();
    }

    void Update()
    {
        // 同时支持键盘 F 和手柄 LB
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton4))
        {
            StopAllCoroutines();
            StartCoroutine(PerformWorldSwitch());
        }
    }

    IEnumerator PerformWorldSwitch()
    {
        // 1. 变黑并显示年份
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

        // 2. 状态切换
        isDark = !isDark;

        // --- 核心逻辑修改：检查 Checkbox 是否勾选 ---
        if (teleportOnSwitch)
        {
            TeleportPlayer();
        }

        ApplyLighting();

        yield return new WaitForSeconds(0.5f);

        // 3. 恢复透明
        elapsed = 0;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (elapsed / fadeOutTime));
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = alpha;
            if (yearHintText != null) SetYearTextAlpha(alpha);
            yield return null;
        }
    }

    void SetYearTextAlpha(float alpha)
    {
        if (yearHintText == null) return;
        Color c = yearHintText.color;
        c.a = alpha;
        yearHintText.color = c;
    }

    void TeleportPlayer()
    {
        if (playerTransform == null) return;
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // 禁用控制器防止传送失败

        playerTransform.position = startPosition;
        playerTransform.rotation = startRotation;

        if (cc != null) cc.enabled = true;
    }

    void ApplyLighting()
    {
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
    }
}