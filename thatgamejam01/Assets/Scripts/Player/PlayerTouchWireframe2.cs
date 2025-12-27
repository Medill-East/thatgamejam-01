using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTouchWireframe2 : MonoBehaviour
{
    [Header("生成设置")]
    [Tooltip("摸墙多久后生成线框？")]
    public float touchDelay = 0.5f; 
    public GameObject decalPrefab; // 拖入你的 Wireframe 贴花预制体

    // 内部状态
    private float _timer = 0f;
    private bool _isTouching = false;
    private Vector3 _hitPoint;
    private Vector3 _hitNormal;

    // --- 供外部调用的接口 ---
    public void UpdateTouchState(bool isTouching, Vector3 point, Vector3 normal)
    {
        // 状态发生改变时（比如刚从没摸变成摸，或者摸的位置变了）
        if (isTouching != _isTouching)
        {
            _timer = 0f; // 重置计时器
        }

        _isTouching = isTouching;
        
        if (isTouching)
        {
            _hitPoint = point;
            _hitNormal = normal;
        }
    }

    void Update()
    {
        if (_isTouching)
        {
            _timer += Time.deltaTime;

            if (_timer >= touchDelay)
            {
                SpawnWireframe();
                _timer = 0f; // 重置，避免一帧生成一个，如果你想持续生成可以调整这里
                // 或者: _isTouching = false; // 生成一次后就停止，直到下次重新摸墙
            }
        }
        else
        {
            _timer = 0f;
        }
    }

    void SpawnWireframe()
    {
        if (decalPrefab != null)
        {
            // 生成贴花，并让它朝向墙壁法线
            Quaternion rotation = Quaternion.LookRotation(_hitNormal);
            Instantiate(decalPrefab, _hitPoint, rotation);
            
            // Debug.Log("生成线框！");
        }
    }
}