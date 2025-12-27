using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTouchWireframe : MonoBehaviour
{
    [SerializeField] private float touchDelay = 0.2f;
    private float currentTouchDelayTime = 0f;
    private bool canTouch = true;
    private bool isTouching = false;
    public GameObject decal;
    private Vector3 decalPos;

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
    }

    private void Touch()
    {
        Instantiate(decal,decalPos,decal.transform.rotation);
    }

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
            /*Debug.Log("触碰点坐标: " + decalPos);*/
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Wall")
        {
            isTouching = false;
        }
    }
}
