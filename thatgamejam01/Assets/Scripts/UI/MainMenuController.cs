using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Credits Panel object to Start/Close.")]
    public GameObject creditsPanel;

    [Header("Scene Settings")]
    [Tooltip("Name of the game scene to load.")]
    public string gameSceneName = "01-room";

    // Call this from the "Start" Button
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Call this from the "Credits" Button
    public void OpenCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
    }

    // Call this from the "X" / "Close" Button inside Credits
    public void CloseCredits()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    // Call this from the "Exit" Button
    public void ExitGame()
    {
        Debug.Log("Exit Game Requested");
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
