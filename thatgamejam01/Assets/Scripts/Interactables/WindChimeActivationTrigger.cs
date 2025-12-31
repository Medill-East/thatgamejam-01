using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindChimeActivationTrigger : MonoBehaviour
{
    private LightingSwitcher _lightSwitcher;
    [SerializeField] private SmartAudioSource chimeAudio;
    
    void Start()
    {
        _lightSwitcher = GameObject.Find("Directional Light").GetComponent<LightingSwitcher>();
    }

    private void OnTriggerEnter(Collider other)
    {
        //只有在白天才能触发激活风铃的声音
        if (other.CompareTag("Player") & !_lightSwitcher.isDark)
        {
            chimeAudio.externalVolumeMult = 1;
            
            //风铃声音激活之后关闭trigger 以及对应触发器
            gameObject.GetComponent<SphereCollider>().enabled = false;
            gameObject.GetComponent<WindChimeActivationTrigger>().enabled = false;
        }
    }
}