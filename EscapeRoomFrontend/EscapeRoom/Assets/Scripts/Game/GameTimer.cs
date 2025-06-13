using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float baseTime = 300f;

    private string loseSceneName = "EndScene";

    [Header("Difficulty Time Multipliers")]
    public float easyTimeMultiplier = 1.5f;
    public float mediumTimeMultiplier = 1.0f;
    public float hardTimeMultiplier = 0.7f;

    [Header("VR World Space Settings")]
    public float canvasDistance = 0.1f;
    public float canvasScale = 0.0004f;
    public Vector3 canvasOffset = new Vector3(0.7f, 0.4f, 0f);

    private Canvas timerCanvas;
    private Text timerText;
    private Text timerLabelText;

    [Header("Camera Assignment")]
    public Camera playerCamera;

    private string timeFormat = "mm:ss";
    private string timerLabel = "Hátralévő idő";

    private UnityEngine.Events.UnityEvent OnTimerComplete;
    private UnityEngine.Events.UnityEvent OnTimeRunsOut;

    private float currentTime;
    private float startTime;
    private bool isRunning = false;
    private bool isComplete = false;
    private bool hasLost = false;

    void Start()
    {
        CreateTimerUI();

        CalculateStartTimeFromDifficulty();

        currentTime = startTime;
        UpdateTimerDisplay();

        StartTimer();
    }

    void CalculateStartTimeFromDifficulty()
    {
        string difficulty = PlayerPrefs.GetString("SelectedDifficulty", "közepes");

        switch (difficulty.ToLower())
        {
            case "könnyű":
                startTime = baseTime * easyTimeMultiplier;
                Debug.Log($"Easy difficulty selected. Timer set to {startTime} seconds ({baseTime} * {easyTimeMultiplier})");
                break;
            case "közepes":
                startTime = baseTime * mediumTimeMultiplier;
                Debug.Log($"Medium difficulty selected. Timer set to {startTime} seconds ({baseTime} * {mediumTimeMultiplier})");
                break;
            case "nehéz":
                startTime = baseTime * hardTimeMultiplier;
                Debug.Log($"Hard difficulty selected. Timer set to {startTime} seconds ({baseTime} * {hardTimeMultiplier})");
                break;
            default:
                startTime = baseTime * mediumTimeMultiplier;
                Debug.Log($"Unknown difficulty '{difficulty}'. Defaulting to medium. Timer set to {startTime} seconds");
                break;
        }
    }

    void CreateTimerUI()
    {
        GameObject canvasGO = new GameObject("TimerCanvas");
        timerCanvas = canvasGO.AddComponent<Canvas>();
        timerCanvas.renderMode = RenderMode.WorldSpace;

        timerCanvas.sortingOrder = 32767;
        timerCanvas.sortingLayerName = "Default";
        canvasGO.layer = 5;

        RectTransform canvasRect = timerCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 200);
        canvasGO.transform.localScale = Vector3.one * canvasScale;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1;

        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject labelGO = new GameObject("TimerLabel");
        labelGO.transform.SetParent(canvasGO.transform);
        labelGO.layer = 5;

        timerLabelText = labelGO.AddComponent<Text>();
        timerLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        timerLabelText.fontSize = 32;
        timerLabelText.color = Color.white;
        timerLabelText.text = timerLabel;
        timerLabelText.alignment = TextAnchor.MiddleCenter;
        timerLabelText.transform.localScale = new Vector3(2f, 2f, 2f);

        Outline labelOutline = labelGO.AddComponent<Outline>();
        labelOutline.effectColor = Color.black;
        labelOutline.effectDistance = new Vector2(2, 2);

        RectTransform labelRect = timerLabelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.7f);
        labelRect.anchorMax = new Vector2(0.5f, 0.7f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(350f, 40f);

        GameObject timerGO = new GameObject("TimerText");
        timerGO.transform.SetParent(canvasGO.transform);
        timerGO.layer = 5;

        timerText = timerGO.AddComponent<Text>();
        timerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        timerText.fontSize = 48;
        timerText.color = Color.white;
        timerText.text = "00:00";
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.transform.localScale = new Vector3(2f, 2f, 2f);

        Outline outline = timerGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);

        RectTransform rectTransform = timerText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(300f, 60f);

        UpdateCanvasPosition();
    }
    void Update()
    {
        UpdateCanvasPosition();

        if (isRunning && !isComplete && !hasLost)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                TimeRunsOut();
            }

            UpdateTimerDisplay();

            if (currentTime <= 30f && timerText != null)
            {
                timerText.color = Color.red;
                if (timerLabelText != null)
                {
                    timerLabelText.color = Color.red;
                }
            }
        }
    }

    void UpdateCanvasPosition()
    {
        if (timerCanvas == null || playerCamera == null) return;

        Vector3 cameraForward = playerCamera.transform.forward;
        Vector3 cameraRight = playerCamera.transform.right;
        Vector3 cameraUp = playerCamera.transform.up;

        Vector3 finalPosition = playerCamera.transform.position
            + cameraForward * 0.5f
            + cameraRight * canvasOffset.x
            + cameraUp * canvasOffset.y;

        timerCanvas.transform.position = finalPosition;

        timerCanvas.transform.LookAt(playerCamera.transform);
        timerCanvas.transform.Rotate(0, 180, 0);
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        string timeString = FormatTime(currentTime);
        timerText.text = timeString;
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);

        switch (timeFormat.ToLower())
        {
            case "mm:ss.ff":
                return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
            case "mm:ss":
            default:
                return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void StartTimer()
    {
        isRunning = true;
        isComplete = false;
        Debug.Log("Timer Started");
    }

    public void StopTimer()
    {
        isRunning = false;
        Debug.Log("Timer Stopped at: " + FormatTime(currentTime));
    }

    public void PauseTimer()
    {
        isRunning = false;
        Debug.Log("Timer Paused");
    }

    public void ResumeTimer()
    {
        if (!isComplete)
        {
            isRunning = true;
            Debug.Log("Timer Resumed");
        }
    }

    public void ResetTimer()
    {
        currentTime = startTime;
        isRunning = false;
        isComplete = false;
        hasLost = false;

        if (timerText != null)
        {
            timerText.color = Color.white;
        }
        if (timerLabelText != null)
        {
            timerLabelText.color = Color.white;
        }

        UpdateTimerDisplay();
        Debug.Log("Timer Reset");
    }

    public void SetTime(float time)
    {
        currentTime = time;
        UpdateTimerDisplay();
    }

    public float GetCurrentTime()
    {
        return currentTime;
    }

    public string GetFormattedTime()
    {
        return FormatTime(currentTime);
    }

    public float GetStartTime()
    {
        return startTime;
    }

    public bool IsRunning()
    {
        return isRunning;
    }

    public bool HasLost()
    {
        return hasLost;
    }

    public void PlayerWon()
    {
        if (!hasLost)
        {
            isRunning = false;
            isComplete = true;
            Debug.Log("Player Won! Timer stopped at: " + FormatTime(currentTime));
        }
    }

    void TimeRunsOut()
    {
        isRunning = false;
        hasLost = true;
        Debug.Log("Time ran out! Player lost!");

        PlayerPrefs.SetInt("PlayerLost", 1);
        PlayerPrefs.SetFloat("StartTime", startTime);
        PlayerPrefs.SetString("FormattedFinalTime", "00:00");
        PlayerPrefs.Save();

        if (OnTimeRunsOut != null)
        {
            OnTimeRunsOut.Invoke();
        }

        if (!string.IsNullOrEmpty(loseSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(loseSceneName);
        }
    }

    void TimerComplete()
    {
        isRunning = false;
        isComplete = true;
        Debug.Log("Timer Complete!");

        if (OnTimerComplete != null)
        {
            OnTimerComplete.Invoke();
        }
    }

    public static GameTimer Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}