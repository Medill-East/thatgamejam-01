using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro; // 如果使用普通 Text，请删除此行并把 TextMeshProUGUI 改为 Text

public class LightingSwitcher : MonoBehaviour
{
    [Header("Settings")]
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
    public TextMeshProUGUI yearHintText; // 拖入屏幕中间的年份文字组件
    public float fadeInTime = 0.2f;
    public float fadeOutTime = 0.4f;

    [Header("Year Messages")]
    public string pastText = "20 年前";
    public string presentText = "20 年后";

    [Header("Environment Fog")]
    public float darkFogDensity = 0.5f;
    public Color darkFogColor = Color.black;
    
    private WindChime[] windChimes;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (playerTransform != null)
        {
            startPosition = playerTransform.position;
            startRotation = playerTransform.rotation;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;

        // 初始时隐藏年份文字
        if (yearHintText != null) SetYearTextAlpha(0);

        ApplyLighting();
        
        windChimes = FindObjectsOfType<WindChime>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.JoystickButton4))
        {
            StopAllCoroutines();
            StartCoroutine(PerformWorldSwitch());
        }
    }

    IEnumerator PerformWorldSwitch()
    {
        // 1. 屏幕逐渐变黑，同时显示年份文字
        if (yearHintText != null)
            yearHintText.text = isDark ? pastText : presentText; // 注意：由于后面执行 isDark=!isDark，这里提前判断目标状态

        float elapsed = 0;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInTime);
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = alpha;
            if (yearHintText != null) SetYearTextAlpha(alpha); // 文字随黑场一起出现
            yield return null;
        }

        // 2. 黑屏状态下：切换逻辑
        isDark = !isDark;
        TeleportPlayer();
        ApplyLighting();

        foreach (var chime in windChimes)
        {
            chime.ResetWindChime();
        }

        // 在最黑的时候停留一下，让玩家看清年份
        yield return new WaitForSeconds(1f);

        // 3. 屏幕恢复透明，年份文字消失
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

    // 辅助方法：设置文字透明度
    void SetYearTextAlpha(float alpha)
    {
        Color c = yearHintText.color;
        c.a = alpha;
        yearHintText.color = c;
    }

    void TeleportPlayer()
    {
        if (playerTransform == null) return;
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
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