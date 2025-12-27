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
    public float occludedFreq = 600f;  
    [Range(0f, 1f)] public float occludedVolume = 0.4f; 
    public float smoothSpeed = 10f;

    [Header("2. 朝向效果 (聚焦)")]
    public bool useDirectivity = true;
    
    [Tooltip("正对声源时的频率 (清晰)")]
    public float facingFreq = 22000f;
    
    [Tooltip("背对声源时的频率 (闷)")]
    public float backingFreq = 2500f;

    [Space(10)] // 空一行方便阅读
    [Range(0f, 1f)] 
    [Tooltip("背对声源时的音量倍率 (0.5表示只有一半音量)")]
    public float backingVolumeMultiplier = 0.3f; // 【新功能】默认背对时只有30%音量

    // 内部变量
    private AudioSource _audioSource;
    private AudioLowPassFilter _lpf;
    private float _startVolume; 
    
    private float _currentCutoff;
    private float _targetCutoff;
    private float _targetVolume;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _lpf = GetComponent<AudioLowPassFilter>();
        _startVolume = _audioSource.volume; 
        _currentCutoff = _lpf.cutoffFrequency;

        if (playerCamera == null) playerCamera = Camera.main.transform;
    }

    void Update()
    {
        if (playerCamera == null) return;

        bool isOccluded = CheckOcclusion();

        // 优先级逻辑：
        // 1. 如果有墙 (Occluded)：直接应用穿墙设置（最闷、最轻），无视朝向。
        // 2. 如果没墙：根据朝向调整音量和频率。

        if (isOccluded)
        {
            // --- 有墙挡住 ---
            _targetCutoff = occludedFreq;       
            _targetVolume = _startVolume * occludedVolume; 
        }
        else
        {
            // --- 空气传播 ---
            if (useDirectivity)
            {
                // 计算朝向系数 (1=正对, 0=背对)
                Vector3 toSource = (transform.position - playerCamera.position).normalized;
                float angle = Vector3.Angle(playerCamera.forward, toSource);
                float focusFactor = 1f - (angle / 180f); 

                // A. 频率变化 (闷不闷)
                _targetCutoff = Mathf.Lerp(backingFreq, facingFreq, focusFactor);

                // B. 音量变化 (大不大) 【这里是新加的逻辑】
                // 当 focusFactor 为 1 (正对) 时，音量 = 原始音量
                // 当 focusFactor 为 0 (背对) 时，音量 = 原始音量 * 倍率
                _targetVolume = Mathf.Lerp(_startVolume * backingVolumeMultiplier, _startVolume, focusFactor);
            }
            else
            {
                // 不启用朝向功能时，保持原样
                _targetCutoff = 22000f;
                _targetVolume = _startVolume;
            }
        }

        // 平滑过渡 (防止突然回头声音突变)
        _currentCutoff = Mathf.Lerp(_currentCutoff, _targetCutoff, Time.deltaTime * smoothSpeed);
        _lpf.cutoffFrequency = _currentCutoff;
        _audioSource.volume = Mathf.Lerp(_audioSource.volume, _targetVolume, Time.deltaTime * smoothSpeed);
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