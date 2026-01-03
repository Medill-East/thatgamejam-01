using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindChime : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionSpeed = 0.5f; // Time to complete = 1 / speed
    public float decaySpeed = 0.3f;
    public bool requireNight = true; // If true, can only interact when LightingSwitcher.isDark is true
    
    [Header("Visual Feedback")]
    public Light[] feedbackLights; // The lights to brighten
    public float maxLightIntensity = 2.0f;
    public AnimationCurve lightIntensityCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Header("Legacy / Existing")]
    public Material ignoreFogMaterial;
    public bool _hasTriggered = false;
    private Material defaultMaterial;
    private SmartAudioSource smartAudioSource;
    private AudioSource audioSource;
    public GameObject[] windChimeHeads;
    public Material[] windChimeMaterials;
    public Animator chimeAnimator;
    
    private float _currentProgress = 0f;
    private bool _isComplete = false;
    private LightingSwitcher _lightSwitcher;
    private bool _isPlayerInZone = false;

    // Start is called before the first frame update
    void Start()
    {
        defaultMaterial = GetComponent<MeshRenderer>().material;
        
        // Find LightingSwitcher
        GameObject lightObj = GameObject.Find("Directional Light");
        if (lightObj != null)
        {
            _lightSwitcher = lightObj.GetComponent<LightingSwitcher>();
        }
        else
        {
            Debug.LogWarning("WindChime: Could not find 'Directional Light' for LightingSwitcher. Day/Night check may fail.");
            // Fallback: try finding type
            _lightSwitcher = FindObjectOfType<LightingSwitcher>();
        }

        // Safety check for parent hierarchy structure logic from original code
        if (transform.parent != null && transform.parent.childCount > 0)
        {
            var audioObj = transform.parent.GetChild(0).gameObject;
            if(audioObj != null) 
            {
                smartAudioSource = audioObj.GetComponent<SmartAudioSource>();
                audioSource = audioObj.GetComponent<AudioSource>();
            }
        }

        // Initialize Lights
        if (feedbackLights != null)
        {
            foreach(var light in feedbackLights)
            {
                if(light != null)
                {
                    light.intensity = 0f;
                    light.gameObject.SetActive(true);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_isComplete) return;

        UpdateInteractionLogic();
    }
    
    void UpdateInteractionLogic()
    {
        bool canInteract = _isPlayerInZone;

        // Day/Night Requirement Check
        if (requireNight && _lightSwitcher != null && !_lightSwitcher.isDark)
        {
            canInteract = false;
        }

        if (canInteract)
        {
            _currentProgress += interactionSpeed * Time.deltaTime;
        }
        else
        {
            _currentProgress -= decaySpeed * Time.deltaTime;
        }

        _currentProgress = Mathf.Clamp01(_currentProgress);

        UpdateVisuals();

        if (_currentProgress >= 1.0f)
        {
            CompleteInteraction();
        }
    }

    public void SetPlayerInZone(bool status)
    {
        _isPlayerInZone = status;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInZone = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInZone = false;
        }
    }

    void UpdateVisuals()
    {
        if (feedbackLights != null)
        {
            float targetIntensity = lightIntensityCurve.Evaluate(_currentProgress) * maxLightIntensity;
            foreach(var light in feedbackLights)
            {
                if(light != null) light.intensity = targetIntensity;
            }
        }
    }
    
    void CompleteInteraction()
    {
        if (_hasTriggered) return;
        
        _isComplete = true;
        _hasTriggered = true;
        
        Debug.Log("Wind chime interaction complete");

        // Interaction complete - keep lights fully on
        if (feedbackLights != null)
        {
            foreach(var light in feedbackLights)
            {
                if(light != null) light.intensity = maxLightIntensity;
            }
        }

        // Apply legacy completion logic
        if (GetComponent<MeshRenderer>() != null)
            GetComponent<MeshRenderer>().material = ignoreFogMaterial;
            
        if (windChimeHeads != null)
        {
            foreach (var windChiemHead in windChimeHeads)
            {
                if (windChiemHead != null)
                    windChiemHead.GetComponent<MeshRenderer>().material = ignoreFogMaterial;
            }
        }
        
        // Stop audio
        if (smartAudioSource != null) smartAudioSource.enabled = false;
        if (audioSource != null) audioSource.enabled = false;
        
        // Stop animation
        if (chimeAnimator != null) chimeAnimator.SetBool("IsActivated", false);
    }

    public void ResetWindChime()
    {
        _currentProgress = 0f;
        _isComplete = false;
        _hasTriggered = false;
        
        // Reset all lights
        if (feedbackLights != null)
        {
            foreach(var light in feedbackLights)
            {
                if(light != null) light.intensity = 0f;
            }
        }

        if (GetComponent<MeshRenderer>() != null)
            GetComponent<MeshRenderer>().material = defaultMaterial;
            
        if (windChimeHeads != null && windChimeMaterials != null)
        {
            for (int i = 0; i < windChimeHeads.Length; i++)
            {
                 if (windChimeHeads[i] != null && i < windChimeMaterials.Length)
                    windChimeHeads[i].GetComponent<MeshRenderer>().material = windChimeMaterials[i];
            }
        }
        
        if (audioSource != null) audioSource.enabled = true;
        if (smartAudioSource != null) smartAudioSource.enabled = true;
        
        if (chimeAnimator != null) chimeAnimator.SetBool("IsActivated", true);
    }
}