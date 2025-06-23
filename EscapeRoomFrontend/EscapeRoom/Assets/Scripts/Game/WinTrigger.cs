using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinTrigger : MonoBehaviour
{
    [Header("Win Settings")]
    public float fadeDuration = 2f;
    public string winnerSceneName = "WinnerScene";

    [Header("Fade Settings")]
    public Image fadeImage;
    public Color fadeColor = Color.black;

    private bool hasWon = false;

    void Start()
    {
        if (fadeImage == null)
        {
            CreateFadeOverlay();
        }

        Color startColor = fadeColor;
        startColor.a = 0f;
        fadeImage.color = startColor;
    }

    void CreateFadeOverlay()
    {
        GameObject canvasGO = new GameObject("FadeCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject fadeGO = new GameObject("FadeImage");
        fadeGO.transform.SetParent(canvasGO.transform);

        fadeImage = fadeGO.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasWon && (other.CompareTag("Player") || other.name.Contains("Camera") || other.name.Contains("Head")))
        {
            Debug.Log("Player stepped on win area!");
            TriggerWin();
        }
    }

    public void TriggerWin()
    {
        if (hasWon) return;

        if (GameTimer.FindTimer() != null)
        {
            if (GameTimer.FindTimer().HasLost())
            {
                Debug.Log("Cannot win - time has already run out!");
                return;
            }

            GameTimer.FindTimer().PlayerWon();
            Debug.Log("Final Time: " + GameTimer.FindTimer().GetFormattedTime());

            PlayerPrefs.SetInt("PlayerLost", 0);
            PlayerPrefs.SetFloat("StartTime", GameTimer.FindTimer().GetStartTime());
            PlayerPrefs.SetFloat("FinalTime", GameTimer.FindTimer().GetCurrentTime());
            PlayerPrefs.SetString("FormattedFinalTime", GameTimer.FindTimer().GetFormattedTime());
            PlayerPrefs.Save();
        }

        hasWon = true;
        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {

        yield return StartCoroutine(FadeToColor(fadeColor, fadeDuration));

        Debug.Log("Loading Winner Scene: " + winnerSceneName);

        SceneManager.LoadScene(winnerSceneName);
    }

    IEnumerator FadeToColor(Color targetColor, float duration)
    {
        Color startColor = fadeImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        fadeImage.color = targetColor;
    }
}