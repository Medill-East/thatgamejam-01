using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinZoneTrigger : MonoBehaviour
{
    [Header("Settings")]
    public float requiredDuration = 3.0f;
    public string mainMenuSceneName = "MainMenu"; // Ensure this matches your menu scene name

    public float fadeDuration = 3.0f;
    public float winWaitDuration = 5.0f;

    [Header("Dependencies")]
    public LightingSwitcher lightingSwitcher;
    public GameObject whiteFlashCanvasPrefab; 

    private float _timer = 0f;
    private bool _isPlayerInside = false;
    private bool _hasTriggered = false;
    private ScreenFader _faderInstance;

    private void Start()
    {
        if (lightingSwitcher == null)
        {
            lightingSwitcher = FindObjectOfType<LightingSwitcher>();
            if (lightingSwitcher == null) Debug.LogWarning("[WinZoneTrigger] LightingSwitcher not found in scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = true;
            _timer = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = false;
            _timer = 0f;
        }
    }

    private void Update()
    {
        if (_hasTriggered) return;
        if (!_isPlayerInside) return;

        // Condition: Must be NIGHT (isDark == true)
        if (lightingSwitcher != null && !lightingSwitcher.isDark)
        {
            _timer = 0f; // Reset if it becomes day
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= requiredDuration)
        {
            StartCoroutine(WinSequence());
        }
    }

    private IEnumerator WinSequence()
    {
        _hasTriggered = true;
        Debug.Log("[WinZoneTrigger] Win Condition Met! Returning to Menu...");

        // Fade Out if possible
        if (whiteFlashCanvasPrefab != null)
        {
            GameObject faderObj = Instantiate(whiteFlashCanvasPrefab);
            _faderInstance = faderObj.GetComponentInChildren<ScreenFader>(); 
            
            if (_faderInstance != null)
            {
                // Ensure content is hidden initially
                if (_faderInstance.finalContent != null) _faderInstance.finalContent.SetActive(false);

                _faderInstance.fadeDuration = fadeDuration; // Set exposed param
                _faderInstance.SetAlpha(0f);
                yield return _faderInstance.FadeIn(); // Fade to color
                
                // Now show the content (Thanks text)
                if (_faderInstance.finalContent != null) _faderInstance.finalContent.SetActive(true);
            }
            else
            {
                yield return new WaitForSeconds(fadeDuration);
            }
        }
        else
        {
            // Fallback wait
             yield return new WaitForSeconds(fadeDuration);
        }
        
        // Wait on the white screen (with text now visible)
        yield return new WaitForSeconds(winWaitDuration);

        // Load Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
