using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public Button continueButton;
    public Image fadeImage;
    public CanvasGroup uiGroup;
    private string nextSceneName = "Menu";

    private void Start()
    {
        fadeImage.color = new Color(0, 0, 0, 1);
        uiGroup.alpha = 0;

        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float duration = 3.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(1f, 0f, t));
            uiGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0);
        uiGroup.alpha = 1f;
    }

    public void OnContinue()
    {
        continueButton.interactable = false;
        StartCoroutine(FadeOutAndLoadScene());
    }

    IEnumerator FadeOutAndLoadScene()
    {
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0f, 1f, t));
            uiGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 1);
        uiGroup.alpha = 0f;
        SceneManager.LoadScene(nextSceneName);
    }
}
