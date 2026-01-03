using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AudioProfile
{
    [Header("1. 穿墙效果 (遮挡)")]
    [Tooltip("有遮挡时的音量倍率 (叠加在朝向音量之上)")]
    [Range(0f, 1f)] public float occludedVolume;
    
    [Tooltip("有遮挡 + 正对声源时的频率 (比如 6400Hz)")]
    public float occludedFacingFreq;
    [Tooltip("有遮挡 + 背对声源时的频率 (比如 3200Hz)")]
    public float occludedBackingFreq;

    [Header("2. 朝向效果 (聚焦)")]
    public bool useDirectivity;
    
    [Tooltip("无遮挡 + 正对声源时的频率 (清晰 25600Hz)")]
    public float facingFreq;
    
    [Tooltip("无遮挡 + 背对声源时的频率 (闷 12800Hz)")]
    public float backingFreq;

    [Range(0f, 1f)] 
    [Tooltip("背对声源时的音量倍率 (0.15表示只有15%音量)")]
    public float backingVolumeMultiplier;

    // Constructor for easy default setup
    public static AudioProfile GetDayDefault()
    {
        return new AudioProfile
        {
            occludedVolume = 0.6f,          // Day: Higher volume through walls
            occludedFacingFreq = 12000f,    // Day: Clearer through walls
            occludedBackingFreq = 8000f,    // Day: Clearer through walls
            useDirectivity = false,         // Day: Less directional focus
            facingFreq = 22000f,
            backingFreq = 20000f,           // Day: Almost same as facing
            backingVolumeMultiplier = 0.8f  // Day: Minimal volume drop when turning away
        };
    }

    public static AudioProfile GetNightDefault()
    {
        return new AudioProfile
        {
            occludedVolume = 0.4f,          // Night: Strict occlusion
            occludedFacingFreq = 6400f,
            occludedBackingFreq = 3200f,
            useDirectivity = true,          // Night: Strict directionality
            facingFreq = 25600f,
            backingFreq = 12800f,
            backingVolumeMultiplier = 0.25f // Night: Big drop when turning away
        };
    }
}

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class SmartAudioSource : MonoBehaviour
{
    [Header("核心引用")]
    public Transform playerCamera;
    public LayerMask obstacleLayer; 

    [Header("Day Profile (Relaxed)")]
    public AudioProfile dayProfile = AudioProfile.GetDayDefault();

    [Header("Night Profile (Strict)")]
    public AudioProfile nightProfile = AudioProfile.GetNightDefault();

    public float smoothSpeed = 10f;
    [Space(10)]
    public float externalVolumeMult = 1.0f; // External control

    // 内部变量
    private AudioSource _audioSource;
    private AudioLowPassFilter _lpf;
    private float _startVolume; 
    
    private float _currentCutoff;
    private float _targetCutoff;
    private float _targetVolume;
    
    private LightingSwitcher _lightSwitcher;

    void Reset()
    {
        // 默认包含 Wall 层
        obstacleLayer = LayerMask.GetMask("Wall");
    }

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _lpf = GetComponent<AudioLowPassFilter>();
        _startVolume = _audioSource.volume; 
        
        // Init cutoff
        _currentCutoff = _lpf.cutoffFrequency;
        
        if (playerCamera == null) 
        {
            if (Camera.main != null)
                playerCamera = Camera.main.transform;
            else
                Debug.LogWarning("[SmartAudioSource] No Main Camera found!");
        }

        // Only search for LightSwitcher if we don't have one (though usually we find it dynamically)
        // Try finding by name first
        GameObject lightObj = GameObject.Find("Directional Light");
        if (lightObj != null) _lightSwitcher = lightObj.GetComponent<LightingSwitcher>();
        if (_lightSwitcher == null) _lightSwitcher = FindObjectOfType<LightingSwitcher>();

        // 如果没有设置任何遮挡层 (Value为0)，则自动添加默认和墙
        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default", "Wall");
        }
    }

    void LateUpdate()
    {
        // 【核心修复】每一帧都获取当前的 Camera.main，确保跟对人
        Transform targetCam = playerCamera; // Fallback
        if (Camera.main != null) 
        {
            targetCam = Camera.main.transform;
            playerCamera = targetCam; // sync back just in case
        }
        
        if (targetCam == null) return;

        // Determine which profile to use
        AudioProfile activeProfile = dayProfile;
        if (_lightSwitcher != null && _lightSwitcher.isDark)
        {
            activeProfile = nightProfile;
        }

        // 基础计算
        // 1. 距离衰减由 AudioSource 自带的 3D Settings 负责
        
        // 2. 计算朝向因子 (1=正对, 0=背对)
        float focusFactor = 1f;
        if (activeProfile.useDirectivity)
        {
            // 使用 targetCam (真相机) 计算方向
            Vector3 toSource = (transform.position - targetCam.position).normalized;
            float angle = Vector3.Angle(targetCam.forward, toSource);
            
            float linearFactor = 1f - (angle / 180f); 
            focusFactor = linearFactor; 
        }

        // 3. 计算 "无遮挡情况" 下的目标值
        float clearFreq = Mathf.Lerp(activeProfile.backingFreq, activeProfile.facingFreq, focusFactor * focusFactor); 
        float baseVol = _startVolume * externalVolumeMult;
        float clearVol = Mathf.Lerp(baseVol * activeProfile.backingVolumeMultiplier, baseVol, focusFactor);

        // 4. 检测遮挡 + 应用遮挡逻辑
        bool isOccluded = CheckOcclusion(targetCam);

        if (isOccluded)
        {
            // --- 有遮挡 ---
            _targetCutoff = Mathf.Lerp(activeProfile.occludedBackingFreq, activeProfile.occludedFacingFreq, focusFactor * focusFactor);
            _targetVolume = clearVol * activeProfile.occludedVolume;
        }
        else
        {
            // --- 无遮挡 ---
            _targetCutoff = clearFreq;
            _targetVolume = clearVol;
        }

        // 5. 应用平滑
        _currentCutoff = Mathf.Lerp(_currentCutoff, _targetCutoff, Time.deltaTime * smoothSpeed);
        _audioSource.volume = Mathf.Lerp(_audioSource.volume, _targetVolume, Time.deltaTime * smoothSpeed);
        _lpf.cutoffFrequency = _currentCutoff;
    }

    bool CheckOcclusion(Transform targetCam)
    {
        if (targetCam == null) return false;

        Vector3 direction = targetCam.position - transform.position;
        float fullDistance = direction.magnitude;
        
        float checkDistance = fullDistance - 0.8f;
        if (checkDistance <= 0.1f) return false;

        // 起点偏移
        Vector3 startOffset = direction.normalized * 0.1f;
        Vector3 startPoint = transform.position + startOffset; 
        checkDistance -= 0.1f;
        
        if (checkDistance <= 0) return false;

        // 强制忽略 Trigger
        if (Physics.Raycast(startPoint, direction.normalized, out _, checkDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            // Simple obstruction check
            return true; 
        }
        
        return false;
    }
}