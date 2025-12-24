using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using Unity.Mathematics;

public class PlayerAbility : MonoBehaviour
{
    private StarterAssetsInputs _input; // 引用 StarterAssets 的输入脚本
    [SerializeField] private Transform CallLightPropSpawnPoint;
    public GameObject CallLightProp;
    
    // Start is called before the first frame update
    void Start()
    {
        _input = FindObjectOfType<StarterAssetsInputs>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_input.callLight)
        {
           Debug.Log("player call light");

           Instantiate(CallLightProp, CallLightPropSpawnPoint);
           
           _input.callLight = false;
        }
    }
}
