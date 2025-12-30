using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallTouchSystem : MonoBehaviour
{
    [Header("调试")]
    public bool invertSide = false; // 如果勾选这个，逻辑会强制反过来
    public enum HandSide { Left, Right }

    [Header("左右手设置")]
    public HandSide currentHand = HandSide.Right; // 选择这只手是左还是右
    [Tooltip("模拟肩膀宽度：射线发射起点的左右偏移量")]
    public float shoulderOffset = 0.2f; 

    [Header("引用")]
    public PlayerTouchWireframe2 wireframeScript; 
    public Transform handContainer;  
    public LayerMask wallLayer;      

    [Header("检测参数")]
    public float armLength = 1.0f;    
    public float sphereRadius = 0.2f; 
    [Range(0f, 90f)] public float maxDetectionAngle = 45f; // 角度可以稍微大一点了
    public float minTouchDistance = 0.3f; 

    [Header("手掌微调")]
    [Tooltip("手掌接触点（例如指尖或掌心）。如果不填则默认使用 Hand Container")]
    public Transform visualContactPoint; 
    public Vector3 handOffset = new Vector3(0, 0, 0.05f); 

    [Header("动画参数")]
    public float moveSpeed = 5f; 
    public float retractSmoothTime = 0.3f; 
    public float rotateSpeed = 10f;   

    // 内部变量
    private Vector3 _defaultPos;      
    private Quaternion _defaultRot;   
    private Vector3 _targetPos;       
    private Quaternion _targetRot;    
    private bool _isTouching = false;
    private Vector3 _velocity = Vector3.zero; 

    // 缓存主摄像机 transform，因为脚本现在挂在子物体上
    private Transform _camTransform;

    void Start()
    {
        if (handContainer == null) return;
        
        _camTransform = Camera.main.transform; // 获取摄像机
        _defaultPos = handContainer.localPosition;
        _defaultRot = handContainer.localRotation;
        
        if (wireframeScript == null) 
            wireframeScript = FindObjectOfType<PlayerTouchWireframe2>();
    }

    void Update()
    {
        if (handContainer == null || _camTransform == null) return;

        DetectWall();
        AnimateHand();
    }

    // --- 核心逻辑提取 ---

    // 获取实际生效的手（考虑反转）
    private HandSide GetEffectiveHand()
    {
        if (invertSide)
        {
            return (currentHand == HandSide.Left) ? HandSide.Right : HandSide.Left;
        }
        return currentHand;
    }

    // 获取射线起点
    private Vector3 GetRayOrigin(Transform cam, HandSide hand)
    {
        float offsetDir = (hand == HandSide.Left) ? -1f : 1f;
        return cam.position + (cam.right * shoulderOffset * offsetDir);
    }

    void DetectWall()
    {
        // 1. 获取有效手
        HandSide effectiveHand = GetEffectiveHand();
        
        // 2. 计算射线起点
        Vector3 rayOrigin = GetRayOrigin(_camTransform, effectiveHand);
        
        Ray ray = new Ray(rayOrigin, _camTransform.forward);
        RaycastHit hit;

        // 3. 物理检测
        if (Physics.SphereCast(ray, sphereRadius, out hit, armLength, wallLayer))
        {
            string handName = (effectiveHand == HandSide.Left) ? "左手" : "右手";

            // --- A. 距离检查 ---
            if (hit.distance < minTouchDistance)
            {
                // Debug.LogWarning($"[{handName}] 拒绝: 离墙太近! ({hit.distance:F2} < {minTouchDistance})");
                SetNotTouching();
                return;
            }

            // --- B. 左右分区检查 (关键逻辑) ---
            Vector3 localHitPoint = _camTransform.InverseTransformPoint(hit.point);
            
            // 左手逻辑：绝不摸右边的墙 (x > 0)
            if (effectiveHand == HandSide.Left && localHitPoint.x > 0.05f) 
            {
                Debug.LogWarning($"[{handName}] 拒绝: 我是左手，但这面墙在右边 (x={localHitPoint.x:F2})");
                SetNotTouching();
                return;
            }
            
            // 右手逻辑：绝不摸左边的墙 (x < 0)
            if (effectiveHand == HandSide.Right && localHitPoint.x < -0.05f)
            {
                Debug.LogWarning($"[{handName}] 拒绝: 我是右手，但这面墙在左边 (x={localHitPoint.x:F2})");
                SetNotTouching();
                return;
            }

            // --- C. 角度检查 ---
            Vector3 directionToHit = (hit.point - _camTransform.position).normalized;
            float angle = Vector3.Angle(_camTransform.forward, directionToHit);

            if (angle < maxDetectionAngle)
            {
                // 成功！
                _isTouching = true;

                // === 视觉位置钳制 (Visual Clamp) ===
                
                // 1. 限制 X 轴 (防止手飞出屏幕左右)
                localHitPoint.x = Mathf.Clamp(localHitPoint.x, -0.4f, 0.4f);
                
                // 2. 限制 Z 轴 (防止侧面摸墙时手掌贴在脸上)
                // 强制保持至少 0.4 米的深度
                if (localHitPoint.z < 0.4f) 
                {
                    localHitPoint.z = 0.4f;
                }
                
                // 3. 转回世界坐标
                Vector3 clampedWorldPos = _camTransform.TransformPoint(localHitPoint);

                // 4. 计算最终目标 (虚拟位置 + 法线偏移)
                _targetPos = clampedWorldPos + (hit.normal * handOffset.z);
                _targetRot = Quaternion.LookRotation(-hit.normal, Vector3.up);

                // Debug.Log($"[{handName}] 摸到了！墙在 x:{localHitPoint.x:F2}");

                // --- 贴花生成逻辑优化 (投影法) ---
                // 只有当手部视觉上非常接近墙面时 (< 10cm)，才生成贴花
                if (Vector3.Distance(handContainer.position, _targetPos) < 0.1f)
                {
                    // 1. 确定参考点 (如果没有手动指定 Tip，就用 Container)
                    Vector3 refPoint = (visualContactPoint != null) ? visualContactPoint.position : handContainer.position;

                    // 2. 构建墙面平面
                    Plane wallPlane = new Plane(hit.normal, hit.point);

                    // 3. 将手的位置投影到墙面上，这才是最精确的触碰点
                    Vector3 projectedPoint = wallPlane.ClosestPointOnPlane(refPoint);

                    if (wireframeScript != null) 
                        wireframeScript.UpdateTouchState(GetInstanceID(), true, projectedPoint, hit.normal, hit.collider);
                }
                else
                {
                    // 手还在飞行中，不需要生成
                   if (wireframeScript != null) 
                        wireframeScript.UpdateTouchState(GetInstanceID(), false, Vector3.zero, Vector3.zero, hit.collider);
                }
                
                return; 
            }
            else
            {
                // Debug.LogWarning($"[{handName}] 拒绝: 角度太大 ({angle:F1} > {maxDetectionAngle})");
            }
        }

        SetNotTouching();
    }

    void SetNotTouching()
    {
        if (_isTouching) // 只有状态改变时才通知，节省性能
        {
            _isTouching = false;
            // 注意：如果两只手公用一个 Wireframe 脚本，一只手松开可能会打断另一只手
            // 建议给每只手配一个单独的 PlayerTouchWireframe2，或者忽略这个Bug
            if (wireframeScript != null) wireframeScript.UpdateTouchState(GetInstanceID(), false, Vector3.zero, Vector3.zero, null);
        }
    }

    void AnimateHand()
    {
        // 动画逻辑不变 (MoveTowards + SmoothDamp)
        if (_isTouching)
        {
            float step = moveSpeed * Time.deltaTime; 
            handContainer.position = Vector3.MoveTowards(handContainer.position, _targetPos, step);
            _velocity = Vector3.zero;
            handContainer.rotation = Quaternion.Slerp(handContainer.rotation, _targetRot, Time.deltaTime * rotateSpeed);
        }
        else
        {
            handContainer.localPosition = Vector3.SmoothDamp(handContainer.localPosition, _defaultPos, ref _velocity, retractSmoothTime);
            handContainer.localRotation = Quaternion.Slerp(handContainer.localRotation, _defaultRot, Time.deltaTime * 5f);
        }
    }

    void OnDrawGizmos()
    {
        if (Camera.main == null) return;
        Transform cam = Camera.main.transform;

        // 【修复】Gizmos 现在会反映 invertSide 的设置，避免误导
        HandSide effectiveHand = GetEffectiveHand(); 
        
        Vector3 rayOrigin = GetRayOrigin(cam, effectiveHand);

        Gizmos.color = (effectiveHand == HandSide.Left) ? Color.yellow : Color.magenta;
        Gizmos.DrawWireSphere(rayOrigin, 0.05f); // 肩膀点
        Gizmos.DrawLine(rayOrigin, rayOrigin + cam.forward * armLength); // 射线方向
    }
}