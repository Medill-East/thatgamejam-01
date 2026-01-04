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

    [Header("Intro Sequence")]
    [Tooltip("The main menu buttons panel to hide when starting.")]
    public GameObject mainButtonsPanel;
    public GameObject loadingOverlay; // 【新增】黑色背景遮罩
    public CanvasGroup headphonePanel;
    public CanvasGroup introPanel;
    public float fadeDuration = 1.0f;
    public float stayDuration = 3.0f;

    // Call this from the "Start" Button
    public void StartGame()
    {
        // Disable buttons immediately to prevent double clicks
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        
        StartCoroutine(PlayIntroAndLoad());
    }

    private System.Collections.IEnumerator PlayIntroAndLoad()
    {
        // 0. Enable Black Overlay to hide menu
        if (loadingOverlay != null) loadingOverlay.SetActive(true);

        // 1. Start Async Load immediately but don't activate
        AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneName);
        op.allowSceneActivation = false;

        // 2. Headphone Hint
        if (headphonePanel != null)
        {
            headphonePanel.gameObject.SetActive(true);
            headphonePanel.alpha = 0f;
            yield return FadeCanvasGroup(headphonePanel, 0f, 1f, fadeDuration);
            yield return new WaitForSeconds(stayDuration);
            yield return FadeCanvasGroup(headphonePanel, 1f, 0f, fadeDuration);
            headphonePanel.gameObject.SetActive(false);
        }

        // 3. Narrative Intro
        if (introPanel != null)
        {
            introPanel.gameObject.SetActive(true);
            introPanel.alpha = 0f;
            yield return FadeCanvasGroup(introPanel, 0f, 1f, fadeDuration);
            yield return new WaitForSeconds(stayDuration);
            yield return FadeCanvasGroup(introPanel, 1f, 0f, fadeDuration);
            introPanel.gameObject.SetActive(false);
        }
        
        // 4. Wait for Load to finish (at least 90%)
        // If load is faster than intro, it waits here instant.
        // If load is slower, we wait in black (Overlay is active).
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // 5. Activate Scene
        op.allowSceneActivation = true;
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }
        cg.alpha = end;
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
