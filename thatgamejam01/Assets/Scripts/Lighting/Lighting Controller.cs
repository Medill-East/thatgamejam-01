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

    [Header("Year Messages")]
    public string pastText = "20 年前";
    public string presentText = "20 年后";

    [Header("Environment Fog")]
    public float darkFogDensity = 0.5f;
    public Color darkFogColor = Color.black;

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

        ApplyLighting();
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

        isDark = !isDark;

        if (teleportOnSwitch) TeleportPlayer();

        ApplyLighting();

        yield return new WaitForSeconds(0.5f);

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
        if (cc != null) cc.enabled = false;
        playerTransform.position = startPosition;
        playerTransform.rotation = startRotation;
        if (cc != null) cc.enabled = true;
    }

    void ApplyLighting()
    {
        // --- 新增：控制水特效显示 ---
        if (waterVFX != null)
        {
            // 如果 isDark 为 false（白天），则 SetActive(true)
            // 如果 isDark 为 true（黑暗），则 SetActive(false)
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
    }
}