using System.Collections;
using UnityEngine;
using System.Collections.Generic; // 必须引用这个，用于使用 List

public class RealisticWalker : MonoBehaviour
{
    [Header("核心组件")]
    public Transform cameraTransform;
    public Transform leftFoot;
    public Transform rightFoot;
    public CharacterController controller;
    public AudioSource footstepSource;

    // --- 新增：材质系统 ---
    [System.Serializable] // 让这个结构体能显示在 Inspector 里
    public struct SurfaceDefinition
    {
        public string tagName;       // 标签名 (例如 "Wood")
        public List<AudioClip> clips; // 对应的声音列表 (随机播放)
    }

    [Header("材质脚步声配置")]
    public List<SurfaceDefinition> surfaces; // 在这里配置你的材质
    public List<AudioClip> defaultClips;     // 如果没检测到材质，播放默认声音

    [Header("不对称音量 (盲人模拟)")]
    [Range(0f, 1f)] public float leftVolume = 0.3f;
    [Range(0f, 1f)] public float rightVolume = 0.8f;

    [Header("镜头晃动与节奏")]
    public float bobFrequency = 10f;
    public float bobHeight = 0.05f;
    public float bobSway = 0.05f;
    public float stepStride = 0.3f;
    public float footLift = 0.1f;

    // 内部变量
    private float _timer = 0;
    private Vector3 _startCameraPos;
    private Vector3 _startLeftFootPos;
    private Vector3 _startRightFootPos;
    private bool _hasSteppedLeft = false;
    private bool _hasSteppedRight = false;

    // 内部变量
    private Transform _targetBobTransform; // 实际用于晃动的物体 (通常是 CinemachineCameraTarget)

    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (footstepSource == null) footstepSource = GetComponent<AudioSource>();

        // 【修复】自动寻找正确的晃动目标
        // 使用 GetComponentInParent 以防脚本挂在子物体上
        var fpc = GetComponentInParent<StarterAssets.FirstPersonController>();
        if (fpc != null && fpc.CinemachineCameraTarget != null)
        {
            _targetBobTransform = fpc.CinemachineCameraTarget.transform;
        }
        else
        {
            _targetBobTransform = cameraTransform;
        }
        
        if (_targetBobTransform != null)
        {
            _startCameraPos = _targetBobTransform.localPosition;
            Debug.Log($"[RealisticWalker] Bobbing Target: {_targetBobTransform.name} (Parent: {(_targetBobTransform.parent != null ? _targetBobTransform.parent.name : "null")})");
        }
        else
        {
             Debug.LogWarning("[RealisticWalker] No Bobbing Target found!");
        }
        
        _startLeftFootPos = leftFoot.localPosition;
        _startRightFootPos = rightFoot.localPosition;
    }

    void Update()
    {
        bool isMoving = controller.velocity.magnitude > 0.1f && controller.isGrounded;

        if (isMoving)
        {
            // 1. 根据速度调整 Timer (让步频和移动速度绑定)
            // 假设 referenceSpeed (例如 4m/s) 是 "bobFrequency" 对应的标准速度
            float referenceSpeed = 4f; 
            float speedFactor = controller.velocity.magnitude / referenceSpeed;
            
            // 限制一下最小值，避免极低速时 timer 几乎不走，导致脚步声卡住
            // 只要在移动 (isMoving=true)，就至少保持一个很慢的节奏
            speedFactor = Mathf.Max(speedFactor, 0.2f); 

            _timer += Time.deltaTime * bobFrequency * speedFactor;

            // 1. 镜头晃动
            if (_targetBobTransform != null)
            {
                float yOffset = Mathf.Sin(_timer) * bobHeight;
                float xOffset = Mathf.Cos(_timer / 2) * bobSway; 
                _targetBobTransform.localPosition = new Vector3(_startCameraPos.x + xOffset, _startCameraPos.y + yOffset, _startCameraPos.z);
            }

            // 2. 获取节奏循环
            float cycle = Mathf.Sin(_timer);

            // === 左脚落地 ===
            if (cycle > 0.95f)
            {
                if (!_hasSteppedLeft)
                {
                    // 播放声音：传入左脚音量
                    PlaySurfaceSound(leftVolume);
                    _hasSteppedLeft = true;
                    _hasSteppedRight = false;
                }
            }
            // === 右脚落地 ===
            else if (cycle < -0.95f)
            {
                if (!_hasSteppedRight)
                {
                    // 播放声音：传入右脚音量
                    PlaySurfaceSound(rightVolume);
                    _hasSteppedRight = true;
                    _hasSteppedLeft = false;
                }
            }

            // 3. 脚掌模型移动 (保持之前的逻辑)
            Vector3 leftTarget = _startLeftFootPos;
            leftTarget.z += cycle * stepStride; 
            leftTarget.y += Mathf.Max(0, cycle) * footLift;

            Vector3 rightTarget = _startRightFootPos;
            rightTarget.z += -cycle * stepStride; 
            rightTarget.y += Mathf.Max(0, -cycle) * footLift;

            leftFoot.localPosition = Vector3.Lerp(leftFoot.localPosition, leftTarget, Time.deltaTime * 10f);
            rightFoot.localPosition = Vector3.Lerp(rightFoot.localPosition, rightTarget, Time.deltaTime * 10f);
        }
        else
        {
            ResetPosition();
        }
    }

    // --- 核心：材质检测与播放逻辑 ---
    void PlaySurfaceSound(float volume)
    {
        if (footstepSource == null) return;

        // 1. 射线检测：从脚底向下发射射线，看看踩到了什么
        RaycastHit hit;
        // 这里的 1.5f 是检测距离，确保能探测到脚下的地面
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 1.5f))
        {
            string hitTag = hit.collider.tag;

            // 2. 遍历我们在 Inspector 里配置的列表，寻找匹配的 Tag
            foreach (var surface in surfaces)
            {
                if (surface.tagName == hitTag)
                {
                    // 找到了！从这个材质的 clips 列表里随机挑一个播放
                    if (surface.clips.Count > 0)
                    {
                        AudioClip clip = surface.clips[Random.Range(0, surface.clips.Count)];
                        PlayClip(clip, volume);
                        return; // 播放完直接结束，不往下走了
                    }
                }
            }
        }

        // 3. 如果射线没打到东西，或者打到的东西没有 Tag，就播放默认声音
        if (defaultClips.Count > 0)
        {
            AudioClip clip = defaultClips[Random.Range(0, defaultClips.Count)];
            PlayClip(clip, volume);
        }
    }

    void PlayClip(AudioClip clip, float volume)
    {
        // 随机微调音调，让声音更自然，不机械
        footstepSource.pitch = Random.Range(0.9f, 1.1f);
        footstepSource.PlayOneShot(clip, volume);
    }

    void ResetPosition()
    {
        _timer = 0;
        _hasSteppedLeft = false; 
        _hasSteppedRight = false;

        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, _startCameraPos, Time.deltaTime * 5f);
        leftFoot.localPosition = Vector3.Lerp(leftFoot.localPosition, _startLeftFootPos, Time.deltaTime * 5f);
        rightFoot.localPosition = Vector3.Lerp(rightFoot.localPosition, _startRightFootPos, Time.deltaTime * 5f);
    }
}