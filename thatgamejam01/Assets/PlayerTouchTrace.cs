using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class PlayerTouchWTrace : MonoBehaviour
{
    [SerializeField] private float touchDelay = 0.2f;
    private float currentTouchDelayTime = 0f;
    private bool canTouch = true;
    private bool isTouching = false;
    
    public GameObject decal;
    private Vector3 decalPos;
    
    public float radius = 0.5f;
    public float maxDistance = 10f;
    public LayerMask targetLayer;
    private Vector3 surfaceNormal;
    private Vector3 surfaceHitPoint;

    private RenderTexture trace;
    private void Start()
    {
        trace = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGB32);
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.SetTexture("_TracePos", trace);
    }

    private void Update()
    {
        if (isTouching)
        {
            if (currentTouchDelayTime <= touchDelay)
            {
                currentTouchDelayTime += Time.deltaTime;
            }
            else
            {
                Touch();
                currentTouchDelayTime = 0f;
            }
        }
        
        RaycastHit hit;
        // 起点，半径，方向，输出信息，最大距离，层级
        if (Physics.SphereCast(transform.position, radius, transform.forward, out hit, maxDistance, targetLayer))
        {
            Debug.Log("碰到了物体: " + hit.collider.name);
            Debug.DrawLine(transform.position, hit.point, Color.red);
            surfaceNormal = hit.normal;
            surfaceHitPoint = hit.point;
            isTouching = true;
        }
        else
        {
            isTouching = false;
        }
    }
    
    private void Touch()
    {
        Vector3 spawnPos = surfaceHitPoint + surfaceNormal * 0.01f; 
    }
}
