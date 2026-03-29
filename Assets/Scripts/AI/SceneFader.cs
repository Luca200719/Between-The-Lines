using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to any GameObject alongside ScenarioManager.
/// Assign your existing full-screen black CanvasGroup to fadeCanvasGroup.
/// Call SceneFader.Instance.FadeToScene(2) or FadeToScene("End Scene") to trigger.
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Your existing full-screen black CanvasGroup")]
    public CanvasGroup fadeCanvasGroup;

    [Header("Settings")]
    public float fadeDuration = 0.6f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Make sure it starts fully transparent
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>Fade to black, then load the scene by name.</summary>
    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeAndLoadByName(sceneName));
    }

    /// <summary>Fade to black, then load the scene by build index.</summary>
    public void FadeToScene(int sceneIndex)
    {
        StartCoroutine(FadeAndLoadByIndex(sceneIndex));
    }

    // ── Coroutines ────────────────────────────────────────────────────

    private IEnumerator FadeAndLoadByName(string sceneName)
    {
        yield return StartCoroutine(FadeToBlack());
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeAndLoadByIndex(int sceneIndex)
    {
        yield return StartCoroutine(FadeToBlack());
        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator FadeToBlack()
    {
        fadeCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }
}