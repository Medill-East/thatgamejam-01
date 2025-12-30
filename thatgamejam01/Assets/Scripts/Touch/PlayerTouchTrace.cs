using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using Object = System.Object;

public class PlayerTouchWTrace : MonoBehaviour
{
    public float radius = 0.5f;
    public float maxDistance = 10f;

    private void Update()
    {
        // RaycastHit hit;
        // // 起点，半径，方向，输出信息，最大距离，层级
        // if (Physics.SphereCast(transform.position, radius, transform.forward, out hit, maxDistance))
        // {
        //     Debug.Log("碰到了物体: " + hit.collider.name);
        //     Debug.DrawLine(transform.position, hit.point, Color.red);
        //     Paintable p = hit.collider.GetComponent<Paintable>();
        //     if (p != null)
        //     {
        //         PaintManager.instance.paint(p, hit.point, 0.1f, 1, 1, Color.white);
        //     }
        // }
    }

    private void OnTriggerStay(Collider other)
    {
        Debug.Log(other.name);
        Vector3 collidePos = other.ClosestPoint(transform.position);
        Paintable p = other.GetComponent<Paintable>();
        Debug.Log("p:", p);
        if (p != null)
        {
            PaintManager.instance.paint(p, collidePos, 0.5f, 1, 1, Color.white);
        }
    }
}
