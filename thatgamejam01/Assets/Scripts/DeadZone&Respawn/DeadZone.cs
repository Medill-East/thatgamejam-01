using UnityEngine;

public class DeadZone : MonoBehaviour
{
    public Transform respawnPoint; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TeleportPlayer(other.gameObject);
        }
    }

    void TeleportPlayer(GameObject player)
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        
        cc.enabled = false;
            
        player.transform.position = respawnPoint.position;
        player.transform.rotation = respawnPoint.rotation; // 设置旋转
            
        cc.enabled = true;
    }
}