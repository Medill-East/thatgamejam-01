using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class PlayerTouchWireframe : MonoBehaviour
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
    
    /*
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        // 起点球
        Gizmos.DrawWireSphere(transform.position, radius);
        // 终点球 (假设没碰到物体)
        Vector3 endPos = transform.position + transform.forward * maxDistance;
        Gizmos.DrawWireSphere(endPos, radius);
    }
    */


    private void Touch()
    {
        Vector3 spawnPos = surfaceHitPoint + surfaceNormal * 0.01f; 
        Instantiate(decal, spawnPos, Quaternion.LookRotation(-surfaceNormal));
    }

    /*
    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Wall")
        {
            isTouching = true;
        }
        Vector3 direction = other.transform.position - transform.position;
        Ray ray = new Ray(transform.position, direction);
        RaycastHit hit;

        if (other.Raycast(ray, out hit, direction.magnitude)) 
        {
            decalPos = hit.point;
            Debug.Log("触碰点坐标: " + decalPos);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Wall")
        {
            isTouching = false;
        }
    }
    */
}
