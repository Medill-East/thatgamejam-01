using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    public float fadeDuration = 1.0f;

    void Awake()
    {
        // Create Canvas Setup programmatically if simple, or assume prefab has it.
        // Let's assume this script is on the Canvas GameObject with a CanvasGroup.
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            _canvasGroup.alpha = timer / fadeDuration;
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeOut()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            _canvasGroup.alpha = 1f - (timer / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
    }
}
