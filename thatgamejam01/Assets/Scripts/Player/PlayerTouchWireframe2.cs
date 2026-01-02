using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTouchWireframe2 : MonoBehaviour
{
    [Header("生成设置")]
    [Tooltip("摸墙多久后生成线框？")]
    public float touchDelay = 0.5f; 
    [Tooltip("最小生成间距（米），防止原地生成太多")]
    public float minSpawnDistance = 0.2f;
    public GameObject decalPrefab; // 拖入你的 Wireframe 贴花预制体
    
    public float touchRadius = 0.3f;
    public float touchStrength = 0.4f;
    public Color touchColor = Color.red;

    // 内部状态类
    private class TouchSource
    {
        public float timer;
        public Vector3 point;
        public Vector3 normal;
        public bool isTouching;
        public Vector3 lastSpawnPoint = Vector3.negativeInfinity; // 记录上次生成的位置
        public Collider collider;
    }

    // 使用字典管理多路输入 (Key: SourceID, Value: State)
    private Dictionary<int, TouchSource> _sources = new Dictionary<int, TouchSource>();

    // --- 供外部调用的接口 ---
    public void UpdateTouchState(int sourceID, bool isTouching, Vector3 point, Vector3 normal, Collider collider)
    {
        if (!_sources.ContainsKey(sourceID))
        {
            _sources[sourceID] = new TouchSource();
        }

        TouchSource source = _sources[sourceID];

        // 状态发生改变时重置计时器
        if (source.isTouching != isTouching)
        {
            source.timer = 0f;
        }

        source.isTouching = isTouching;
        
        if (isTouching)
        {
            source.point = point;
            source.normal = normal;
            source.collider = collider;
        }
    }

    void Update()
    {
        Debug.Log("logging");
        // 遍历所有输入源
        foreach (var kvp in _sources)
        {
            TouchSource source = kvp.Value;
            Debug.Log(source.isTouching);

            if (source.isTouching)
            {
                source.timer += Time.deltaTime;

                // 条件：时间到了 && 距离上次生成点够远
                // if (source.timer >= touchDelay && Vector3.Distance(source.point, source.lastSpawnPoint) > minSpawnDistance)
                // {
                //     //SpawnWireframe(source.point, source.normal);
                //     TouchTrace(source.point, source.collider);
                //     source.lastSpawnPoint = source.point; // 更新生成点
                //     source.timer = 0f; 
                // }
                             
                TouchTrace(source.point, source.collider);
            }
            else
            {
                source.timer = 0f;
            }
        }
    }

    void SpawnWireframe(Vector3 point, Vector3 normal)
    {
        if (decalPrefab != null)
        {
            Quaternion rotation = Quaternion.LookRotation(normal);
            Instantiate(decalPrefab, point, rotation);
        }
    }

    void TouchTrace(Vector3 point, Collider collider)
    {
        Paintable p = collider.GetComponent<Paintable>();
        if (p != null)
        {
            PaintManager.instance.paint(p, point, touchRadius, 0.1f, touchStrength, touchColor);
        }
    }
}