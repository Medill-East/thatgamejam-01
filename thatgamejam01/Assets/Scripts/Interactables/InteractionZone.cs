using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    [Tooltip("The WindChime (or other interactable) this zone controls.")]
    public WindChime targetChime;

    private void OnTriggerEnter(Collider other)
    {
        if (targetChime != null && other.CompareTag("Player"))
        {
            targetChime.SetPlayerInZone(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetChime != null && other.CompareTag("Player"))
        {
            targetChime.SetPlayerInZone(false);
        }
    }
}
