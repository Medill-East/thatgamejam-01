using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BedInteract : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 这里写交互后的逻辑
    public void OnInteract()
    {
        Debug.Log("你和床交互了！"); // 测试用

        // --- 恐怖游戏常用功能 (二选一) ---

        // 情况A：存档/睡觉
        // SaveGame(); 
        Debug.Log("Interact with bed");
        
        // 情况B：躲到床底 (简单的瞬移逻辑示例)
        // 注意：这需要你根据场景调整坐标，并禁用玩家移动
        // Debug.Log("躲进了床底...");
    }
}
