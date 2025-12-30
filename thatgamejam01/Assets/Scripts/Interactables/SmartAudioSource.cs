using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class SmartAudioSource : MonoBehaviour
{
    [Header("核心引用")]
    public Transform playerCamera;
    public LayerMask obstacleLayer; 

    [Header("1. 穿墙效果 (遮挡)")]
    [Tooltip("有遮挡时的音量倍率 (叠加在朝向音量之上)")]
    [Range(0f, 1f)] public float occludedVolume = 0.4f; 
    
    [Tooltip("有遮挡 + 正对声源时的频率 (比如 1600Hz)")]
    public float occludedFacingFreq = 3200;
    [Tooltip("有遮挡 + 背对声源时的频率 (比如 600Hz)")]
    public float occludedBackingFreq = 600f;

    public float smoothSpeed = 10f;

    [Header("2. 朝向效果 (聚焦)")]
    public bool useDirectivity = true;
    
    [Tooltip("无遮挡 + 正对声源时的频率 (清晰 22000Hz)")]
    public float facingFreq = 22000f;
    
    [Tooltip("无遮挡 + 背对声源时的频率 (闷 800Hz)")]
    public float backingFreq = 800f;

    [Space(10)]
    [Range(0f, 1f)] 
    [Tooltip("背对声源时的音量倍率 (0.15表示只有15%音量)")]
    public float backingVolumeMultiplier = 0.25f; 

    [Space(10)]
    public float externalVolumeMult = 1.0f; // External control

    // 内部变量
    private AudioSource _audioSource;
    private AudioLowPassFilter _lpf;
    private float _startVolume; 
    
    private float _currentCutoff;
    private float _targetCutoff;
    private float _targetVolume;

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

        // 如果没有设置任何遮挡层 (Value为0)，则自动添加默认和墙
        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Default", "Wall");
        }
    }

    void Update()
    {
        if (playerCamera == null) return;

        // 基础计算
        // 1. 距离衰减由 AudioSource 自带的 3D Settings 负责 (Logarithmic / Linear Rolloff)
        
        // 2. 计算朝向因子 (1=正对, 0=背对)
        float focusFactor = 1f;
        if (useDirectivity)
        {
            Vector3 toSource = (transform.position - playerCamera.position).normalized;
            float angle = Vector3.Angle(playerCamera.forward, toSource);
            // 简单线性映射 0~180 -> 1~0
            float linearFactor = 1f - (angle / 180f); 
            // 平方处理，让变化曲线更自然 (中间区域变化平滑，两头陡)
            // focusFactor = linearFactor * linearFactor; 
            focusFactor = linearFactor; // 暂时用线性试听效果，或者用平方
        }

        // 3. 计算 "无遮挡情况" 下的目标值
        // 频率
        float clearFreq = Mathf.Lerp(backingFreq, facingFreq, focusFactor * focusFactor); // 频率通常用非线性插值听感更好
        // 音量基数 (外部控制 * 初始音量) in case we need external scaler
        float baseVol = _startVolume * externalVolumeMult;
        // 朝向音量
        float clearVol = Mathf.Lerp(baseVol * backingVolumeMultiplier, baseVol, focusFactor);

        // 4. 检测遮挡 + 应用遮挡逻辑
        bool isOccluded = CheckOcclusion();

        if (isOccluded)
        {
            // --- 有遮挡 ---
            // 4.1 频率：在 "遮挡频率范围" 内根据朝向变化
            // 如果正对墙(音源)，稍微清晰一点(1600)，背对墙，更闷(600)
            _targetCutoff = Mathf.Lerp(occludedBackingFreq, occludedFacingFreq, focusFactor * focusFactor);

            // 4.2 音量：在 "朝向修正后的音量" 基础上，再乘一个遮挡系数
            _targetVolume = clearVol * occludedVolume;
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

    bool CheckOcclusion()
    {
        RaycastHit hit;
        Vector3 direction = playerCamera.position - transform.position;
        float distance = direction.magnitude;
        Vector3 startPoint = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(startPoint, direction, out hit, distance, obstacleLayer))
        {
            if (!hit.collider.CompareTag("Player")) 
            {
                return true; 
            }
        }
        return false;
    }
}