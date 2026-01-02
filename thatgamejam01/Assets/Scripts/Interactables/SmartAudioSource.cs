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
    public float occludedFacingFreq = 6400;
    [Tooltip("有遮挡 + 背对声源时的频率 (比如 600Hz)")]
    public float occludedBackingFreq = 3200;

    public float smoothSpeed = 10f;

    [Header("2. 朝向效果 (聚焦)")]
    public bool useDirectivity = true;
    
    [Tooltip("无遮挡 + 正对声源时的频率 (清晰 22000Hz)")]
    public float facingFreq = 25600;
    
    [Tooltip("无遮挡 + 背对声源时的频率 (闷 800Hz)")]
    public float backingFreq = 12800;

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

        // 基础计算
        // 1. 距离衰减由 AudioSource 自带的 3D Settings 负责
        
        // 2. 计算朝向因子 (1=正对, 0=背对)
        float focusFactor = 1f;
        if (useDirectivity)
        {
            // 使用 targetCam (真相机) 计算方向
            Vector3 toSource = (transform.position - targetCam.position).normalized;
            float angle = Vector3.Angle(targetCam.forward, toSource);
            
            float linearFactor = 1f - (angle / 180f); 
            focusFactor = linearFactor; 
        }

        // 3. 计算 "无遮挡情况" 下的目标值
        float clearFreq = Mathf.Lerp(backingFreq, facingFreq, focusFactor * focusFactor); 
        float baseVol = _startVolume * externalVolumeMult;
        float clearVol = Mathf.Lerp(baseVol * backingVolumeMultiplier, baseVol, focusFactor);

        // 4. 检测遮挡 + 应用遮挡逻辑
        // 传入 targetCam，保证 CheckOcclusion 也就这个相机检测
        bool isOccluded = CheckOcclusion(targetCam);

        if (isOccluded)
        {
            // --- 有遮挡 ---
            _targetCutoff = Mathf.Lerp(occludedBackingFreq, occludedFacingFreq, focusFactor * focusFactor);
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

    // 修改签名，接受 targetCam 参数
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

        RaycastHit hit;
        
        // 强制忽略 Trigger
        if (Physics.Raycast(startPoint, direction.normalized, out hit, checkDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.CompareTag("Player")) 
            {
                // 【调试信息】
                // 打印出：我是谁？我用的相机是谁？相机在哪？挡我的是谁？
                /*Debug.Log($"[SmartAudioSource: {this.name}] \n" +
                          $"  -> TargetCam: {targetCam.name} (Pos: {targetCam.position}) \n" +
                          $"  -> Occluded by: {hit.collider.name} (Tag: {hit.collider.tag})");*/
                
                Debug.DrawLine(startPoint, hit.point, Color.red);
                return true; 
            }
        }
        else 
        {
            // 如果没遮挡，画黄线
            // 为了区分不同实例，稍微改变一下颜色或者打印log
            Debug.DrawLine(startPoint, targetCam.position, Color.yellow);
        }
        return false;
    }
}