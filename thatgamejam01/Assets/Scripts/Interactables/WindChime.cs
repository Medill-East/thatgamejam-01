using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindChime : MonoBehaviour, IInteractable
{
    private bool _isInteracting = false;
    public Material ignoreFogMaterial;

    private bool _hasTriggered = false;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnInteract()
    {
        if (!_hasTriggered) return;
        
        Debug.Log("player touch wind chime");

        //交互后风铃一直显示
        gameObject.GetComponent<MeshRenderer>().material = ignoreFogMaterial;
        
        //关闭当前smart audio
        gameObject.GetComponent<SmartAudioSource>().enabled = false;

        //风铃只能交互一次
        _hasTriggered = true;
        
        if (_isInteracting) return;
        _isInteracting = true;
    }
}