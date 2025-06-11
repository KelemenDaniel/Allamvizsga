using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class WinnerScreenDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Text congratulationsText;
    public Text finalTimeText;
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Scene Names")]
    public string gameSceneName = "GameScene";
    public string mainMenuSceneName = "MainMenu";

    [Header("VR World Space Canvas Settings")]
    public Camera targetCamera;
    public Vector3 canvasPosition = new Vector3(0, 0, 3);
    public Vector3 canvasRotation = new Vector3(0, 0, 0);
    public Vector2 canvasSize = new Vector2(8, 5);
    public float canvasScale = 0.005f;

    private string congratsMessage = "GRATULÁLOK!\nSIKERESEN KIJUTOTTÁL!";
    private string gameOverMessage = "JÁTÉK VÉGE!\nLEJÁRT AZ IDŐ!";

    void Start()
    {

        if (congratulationsText == null || finalTimeText == null)
        {
            CreateVRWinnerUI();
        }

        DisplayTimerData();
        SetupButtons();

    }

    void CreateVRWinnerUI()
    {
        GameObject canvasGO = new GameObject("VRWinnerCanvas");
        canvasGO.transform.position = canvasPosition;
        canvasGO.transform.rotation = Quaternion.Euler(canvasRotation);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = targetCamera;
        canvas.sortingOrder = 100;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize;
        canvasRect.localScale = Vector3.one * canvasScale;

        GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        GameObject panelGO = new GameObject("BackgroundPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        Image backgroundImage = panelGO.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.7f);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.sizeDelta = canvasSize * 200f;
        panelRect.anchoredPosition = Vector2.zero;

        GameObject congratsGO = new GameObject("CongratulationsText");
        congratsGO.transform.SetParent(canvasGO.transform, false);

        congratulationsText = congratsGO.AddComponent<Text>();
        congratulationsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        congratulationsText.fontSize = 42;
        congratulationsText.color = Color.yellow;
        congratulationsText.text = congratsMessage;
        congratulationsText.alignment = TextAnchor.MiddleCenter;

        Outline congratsOutline = congratsGO.AddComponent<Outline>();
        congratsOutline.effectColor = Color.black;
        congratsOutline.effectDistance = new Vector2(2, 2);

        RectTransform congratsRect = congratsGO.GetComponent<RectTransform>();
        congratsRect.anchorMin = new Vector2(0.5f, 0.8f);
        congratsRect.anchorMax = new Vector2(0.5f, 0.8f);
        congratsRect.pivot = new Vector2(0.5f, 0.5f);
        congratsRect.anchoredPosition = Vector2.zero;
        congratsRect.sizeDelta = new Vector2(700f, 120f);

        GameObject finalTimeGO = new GameObject("FinalTimeText");
        finalTimeGO.transform.SetParent(canvasGO.transform, false);

        finalTimeText = finalTimeGO.AddComponent<Text>();
        finalTimeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        finalTimeText.fontSize = 26;
        finalTimeText.color = Color.white;
        finalTimeText.alignment = TextAnchor.MiddleCenter;

        Outline timeOutline = finalTimeGO.AddComponent<Outline>();
        timeOutline.effectColor = Color.black;
        timeOutline.effectDistance = new Vector2(1, 1);

        RectTransform timeRect = finalTimeGO.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 0.6f);
        timeRect.anchorMax = new Vector2(0.5f, 0.6f);
        timeRect.pivot = new Vector2(0.5f, 0.5f);
        timeRect.anchoredPosition = new Vector2(0f, -100f);
        timeRect.sizeDelta = new Vector2(600f, 80f);

        CreateVRButton(canvasGO.transform, "Play Again", new Vector2(-120f, -200f), PlayAgain, ref playAgainButton);
        CreateVRButton(canvasGO.transform, "Main Menu", new Vector2(120f, -200f), MainMenu, ref mainMenuButton);
    }

    void CreateVRButton(Transform parent, string buttonText, Vector2 position, System.Action onClick, ref Button buttonRef)
    {
        GameObject buttonGO = new GameObject(buttonText + "Button");
        buttonGO.transform.SetParent(parent, false);

        buttonRef = buttonGO.AddComponent<Button>();
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.3f, 0.8f, 0.8f);

        ColorBlock colors = buttonRef.colors;
        colors.normalColor = new Color(0.2f, 0.3f, 0.8f, 0.8f);
        colors.highlightedColor = new Color(0.4f, 0.5f, 1f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.2f, 0.6f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        buttonRef.colors = colors;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 24;
        text.color = Color.white;
        text.text = buttonText;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.3f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.3f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(180f, 60f);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        buttonRef.onClick.AddListener(delegate { onClick(); });
    }

    void DisplayTimerData()
    {
        bool hasTimerData = PlayerPrefs.HasKey("FinalTime");
        bool playerLost = PlayerPrefs.GetInt("PlayerLost", 0) == 1;

        if (congratulationsText != null)
        {
            if (playerLost)
            {
                congratulationsText.text = gameOverMessage;
                congratulationsText.color = Color.red;
            }
            else
            {
                congratulationsText.text = congratsMessage;
                congratulationsText.color = Color.yellow;
            }
        }

        if (hasTimerData && finalTimeText != null)
        {
            float startTime = PlayerPrefs.GetFloat("StartTime", 0f);
            float finalTime = PlayerPrefs.GetFloat("FinalTime", 0f);
            string displayText = "";

            if (playerLost)
            {
                displayText = "Kifutottál az időből!\nSok sikert következő alkalommal!";
                finalTimeText.color = Color.red;
            }
            else
            {
                float playtime = startTime - finalTime;
                string playtimeFormatted = FormatTime(playtime);
                displayText = "Teljesítési idő: " + playtimeFormatted;
                finalTimeText.color = Color.white;
            }

            finalTimeText.text = displayText;
            Debug.Log("Displayed timer data - Player lost: " + playerLost);
        }
        else if (finalTimeText != null)
        {
            finalTimeText.text = "";
        }
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void SetupButtons()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(delegate { PlayAgain(); });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(delegate { MainMenu(); });
        }
    }

    public void PlayAgain()
    {
        Debug.Log("Loading game scene: " + gameSceneName);
        ClearTimerData();
        Application.LoadLevel(gameSceneName);
    }

    public void MainMenu()
    {
        Debug.Log("Loading main menu: " + mainMenuSceneName);
        ClearTimerData();
        Application.LoadLevel(mainMenuSceneName);
    }

    public void ClearTimerData()
    {
        PlayerPrefs.DeleteKey("StartTime");
        PlayerPrefs.DeleteKey("FinalTime");
        PlayerPrefs.DeleteKey("FormattedFinalTime");
        PlayerPrefs.DeleteKey("WasCountingUp");
        PlayerPrefs.DeleteKey("PlayerLost");
        PlayerPrefs.Save();
    }
}