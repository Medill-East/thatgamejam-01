using UnityEngine;

public class LightingSwitcher : MonoBehaviour
{
    [Header("Settings")]
    public Light sunLight;             // 拖入你的 Directional Light
    public Material daySkybox;         // 拖入你原本的天空盒材质
    public Camera mainCamera;          // 拖入你的主相机
    public bool isDark = true;         // 初始状态

    [Header("Fog Settings")]
    public float darkFogDensity = 0.9f;
    public Color darkFogColor = Color.black;

    void Start()
    {
        // 如果没有手动拖入，尝试自动获取
        if (mainCamera == null) mainCamera = Camera.main;
        ApplyLighting();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isDark = !isDark;
            ApplyLighting();
        }
    }

    void ApplyLighting()
    {
        if (isDark)
        {
            // --- 切换为：20年后（黑暗） ---
            if (sunLight) sunLight.intensity = 0f;

            // 移除天空盒并设置相机背景为纯黑
            RenderSettings.skybox = null;
            if (mainCamera)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.black;
            }

            // 环境光设为黑色
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            // 开启浓雾
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = darkFogDensity;
            RenderSettings.fogColor = darkFogColor;
        }
        else
        {
            // --- 切换为：回忆中（明亮） ---
            if (sunLight) sunLight.intensity = 1.0f;

            // 还原天空盒和相机设置
            RenderSettings.skybox = daySkybox;
            if (mainCamera)
            {
                mainCamera.clearFlags = CameraClearFlags.Skybox;
            }

            // 还原环境光为天空盒照明
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;

            // 关闭雾气
            RenderSettings.fog = false;
        }

        // 强制刷新环境光设置
        DynamicGI.UpdateEnvironment();
    }
}