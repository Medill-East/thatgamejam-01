using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindChime : MonoBehaviour, IInteractable
{
    private bool _isInteracting = false;
    public Material ignoreFogMaterial;

    public bool _hasTriggered = false;
    private Material defaultMaterial;
    private SmartAudioSource smarAudioSource;
    private AudioSource audioSource;
    
    
    // Start is called before the first frame update
    void Start()
    {
        defaultMaterial = GetComponent<MeshRenderer>().material;
        smarAudioSource = gameObject.transform.parent.GetChild(0).gameObject.GetComponent<SmartAudioSource>();
        audioSource = gameObject.transform.parent.GetChild(0).gameObject.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnInteract()
    {
        if (_hasTriggered) return;
        
        Debug.Log("player touch wind chime");

        //交互后风铃一直显示
        gameObject.GetComponent<MeshRenderer>().material = ignoreFogMaterial;
        
        //关闭当前smart audio 和 audio source
        smarAudioSource.enabled = false;
        audioSource.enabled = false;

        //风铃只能交互一次
        _hasTriggered = true;
        
        if (_isInteracting) return;
        _isInteracting = true;
    }

    public void ResetWindChime()
    {
        gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
        audioSource.enabled = true;
        _hasTriggered = false;
    }
}