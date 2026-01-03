using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class PlayerInteractRaycast : MonoBehaviour
{
    
    [Header("Settings")]
    public float interactDistance = 3f; // 交互距离
    public LayerMask interactLayer;     // 这一层是可以交互的物体

    [Header("UI")]
    public GameObject interactText;     // 比如屏幕上显示 "按 E 交互"

    private Camera _cam;
    private StarterAssetsInputs _input; // 引用 StarterAssets 的输入脚本

    private int finalMask;
    
    private LightingSwitcher _lightSwitcher;

    void Start()
    {
        /// 1. 获取摄像机
        _cam = Camera.main;

        // 2. 【修改这里】暴力查找场景里唯一的输入脚本
        // 既然相机不是玩家的孩子，那我们就直接在全场景里搜这个脚本
        _input = FindObjectOfType<StarterAssetsInputs>();

        // 安全检查
        if (_input == null)
        {
            Debug.LogError("还是找不到！请确认你的 PlayerCapsule 物体上挂了 StarterAssetsInputs 脚本，且物体是激活的。");
        }
        
        // 射线检测只检测特定的层，避开玩家层
        finalMask = ~LayerMask.GetMask("Player", "UI", "IgnoreRaycast");
        
        _lightSwitcher = GameObject.Find("Directional Light").GetComponent<LightingSwitcher>();
    }

    void Update()
    {
        // 如果输入脚本没找到，就停止运行，防止报错刷屏
        if (_input == null) return;

        // --- 射线检测逻辑 ---
        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
        RaycastHit hit;

        // 发射射线
        if (Physics.Raycast(ray, out hit, interactDistance,finalMask))
        {
            //Debug.Log(hit.transform.gameObject.name);
            // 检查是不是看向了"Interactable"标签的物体
            if (hit.collider.CompareTag("Interactable"))
            {
                //如果交互对象是风铃
                if (hit.collider.gameObject.GetComponent<WindChime>() != null)
                {
                    Debug.Log("hit windchime");
                    //当风铃已经被交互过 或者 当前不是黑天 均无法交互
                    if (hit.collider.gameObject.GetComponent<WindChime>()._hasTriggered || !_lightSwitcher.isDark)
                    {
                        Debug.Log("cant interact with wind chime");
                        return;
                    }
                }
                
                // A. 显示提示文字
                if (interactText != null) interactText.SetActive(true);

                // B. 检测输入 (这里读取的是 StarterAssetsInputs 里的 interact 变量)
                if (_input.interact)
                {
                    // 获取物体上的 IInteractable 接口 (支持在父物体上)
                    IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                    
                    if (interactable != null)
                    {
                        // 执行交互
                        interactable.OnInteract();
                        
                        // 【关键一步】
                        // 因为 Update 每帧运行，为了防止按一次键触发几十次交互，
                        // 我们用完这个信号后，必须立马手动把它关掉
                        _input.interact = false;
                    }
                }
            }
            else
            {
                // 打到了墙壁等其他东西，隐藏文字
                if (interactText != null) interactText.SetActive(false);
            }
        }
        else
        {
            // 没打到任何东西，隐藏文字
            if (interactText != null) interactText.SetActive(false);
        }
    }
}
