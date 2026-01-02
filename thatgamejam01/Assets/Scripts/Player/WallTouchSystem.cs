using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallTouchSystem : MonoBehaviour
{
    public enum HandSide { Left, Right }
    public HandSide currentHand;

    public float touchRadius = 0.3f;
    public float touchStrength = 0.4f;
    public Color touchColor = Color.red;

    [Header("Core Settings")]
    [Tooltip("The actual hand model to move.")]
    public Transform handModel;
    [Tooltip("The shoulder reference point for the raycast (optional). If null, calculates relative to camera.")]
    public Transform shoulderTransform;
    [Tooltip("Which layers count as 'wall' to touch.")]
    public LayerMask touchableLayers;

    [Header("Raycast Settings")]
    [Tooltip("How far the player can reach.")]
    public float reachDistance = 1.0f;
    [Tooltip("Height offset for the shoulder relative to camera if no transform provided.")]
    public Vector3 shoulderOffset = new Vector3(0.2f, -0.2f, 0f); // Default: Right side, slightly down

    [Header("Hand Settings")]
    [Tooltip("Offset from the wall surface to prevent clipping.")]
    public float surfaceOffset = 0.05f;
    [Tooltip("Rotation offset to align the palm correctly with the surface.")]
    public Vector3 rotationOffset = Vector3.zero;
    [Tooltip("Speed of hand movement.")]
    public float movementSpeed = 10f;
    [Tooltip("Speed of hand rotation.")]
    public float rotationSpeed = 15f;
    
    [Header("Resting State")]
    [Tooltip("Local position when not touching anything.")]
    public Vector3 restingLocalPosition = new Vector3(0.2f, -0.2f, 0.4f);
    [Tooltip("Local rotation when not touching anything.")]
    public Vector3 restingLocalRotation = Vector3.zero;

    private Transform _camTransform;
    private bool _isTouching = false;

    // For debugging/gizmos
    private Vector3 _debugRayOrigin;
    private Vector3 _debugRayEnd;

    void Start()
    {
        _camTransform = Camera.main != null ? Camera.main.transform : transform;
        
        // Ensure we have a default resting state if not set (or use current)
        if (handModel != null)
        {
             // Optional: Initialize resting state from current transform if user didn't set values
             // restingLocalPosition = handModel.localPosition;
             // restingLocalRotation = handModel.localEulerAngles;
        }
        else
        {
             Debug.LogError("WallTouchSystem: Hand Model is not assigned!");
        }
    }

    void Update()
    {
        if (handModel == null || _camTransform == null) return;

        HandleTouchLogic();
    }

    void HandleTouchLogic()
    {
        // 1. Determine Ray Origin (Shoulder)
        Vector3 rayOrigin = shoulderTransform != null 
            ? shoulderTransform.position 
            : _camTransform.TransformPoint(shoulderOffset);

        Vector3 rayDirection = _camTransform.forward;
        _debugRayOrigin = rayOrigin;
        _debugRayEnd = rayOrigin + rayDirection * reachDistance;

        RaycastHit hit;
        bool hitSomething = Physics.Raycast(rayOrigin, rayDirection, out hit, reachDistance, touchableLayers);

        if (hitSomething)
        {
            // Debug: Check if we are facing the wall
             // Basic check: Angle between LookDir and Key Surface Normal roughly opposite? 
             // Or just simple distance trigger. User logic: "When ray hits, reach out."
             
             TouchWall(hit);
        }
        else
        {
            RetractHand();
        }
    }

    private Vector3 _currentSmoothedNormal = Vector3.forward;

    void TouchWall(RaycastHit hit)
    {
        _isTouching = true;

        // Target Position: Hit Point + Surface Normal * Offset
        Vector3 targetPos = hit.point + (hit.normal * surfaceOffset);

        // Smooth the normal to prevent jitter on sharp edges
        // If we were not touching before, snap to the new normal immediately
        // Otherwise, lerp towards it
        float normalSmoothSpeed = 10f; // Can be exposed if needed
        _currentSmoothedNormal = Vector3.Lerp(_currentSmoothedNormal, hit.normal, Time.deltaTime * normalSmoothSpeed);

        // Target Rotation: Palm facing the wall
        Quaternion baseRot = Quaternion.LookRotation(-_currentSmoothedNormal, Vector3.up);
        Quaternion targetRot = baseRot * Quaternion.Euler(rotationOffset);

        // Move Hand
        handModel.position = Vector3.Lerp(handModel.position, targetPos, Time.deltaTime * movementSpeed);
        handModel.rotation = Quaternion.Slerp(handModel.rotation, targetRot, Time.deltaTime * rotationSpeed);

        TouchTrace(targetPos, hit.collider);
    }

    void RetractHand()
    {
        _isTouching = false;
        // Reset smoothed normal when not touching, so next touch starts fresh (logic handled in TouchWall check if needed, 
        // but lerping from old normal might be okay or we can reset to forward relative to player)
        // actually, let's keep it but maybe drift it back to neutral? not strictly necessary.
        
        // Return to local resting position relative to parent (assuming parent moves with player)
        // Note: Using LocalPosition because the hand container moves with the player controller.
        handModel.localPosition = Vector3.Lerp(handModel.localPosition, restingLocalPosition, Time.deltaTime * movementSpeed);
        handModel.localRotation = Quaternion.Slerp(handModel.localRotation, Quaternion.Euler(restingLocalRotation), Time.deltaTime * rotationSpeed);
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = _isTouching ? Color.green : Color.red;
            Gizmos.DrawLine(_debugRayOrigin, _debugRayEnd);
            if(_isTouching) Gizmos.DrawWireSphere(_debugRayEnd, 0.05f);
        }
        else
        {
            // Editor preview
            if (Camera.main != null)
            {
                Transform t = shoulderTransform != null ? shoulderTransform : Camera.main.transform;
                Vector3 origin = shoulderTransform != null ? shoulderTransform.position : t.TransformPoint(shoulderOffset);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, origin + t.forward * reachDistance);
            }
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