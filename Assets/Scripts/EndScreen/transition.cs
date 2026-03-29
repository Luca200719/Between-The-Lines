using System.Collections;
using UnityEngine;

public class transition : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float fadeDuration = 2f; // seconds

    private float timer = 0f;

    void Start()
    {
        canvasGroup.alpha = 1f; // start fully visible
        StartCoroutine(FadeCanvas());
    }

    IEnumerator FadeCanvas() {
        yield return new WaitForSeconds(5f);

        //WHATVER CONDITION NEEDS TO BE MET TO FADE OUT
        // while condition is not met yield return null;

        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= 0.05f;
            yield return null;
        }
    }
}