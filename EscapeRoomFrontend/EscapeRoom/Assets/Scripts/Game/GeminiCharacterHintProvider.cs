using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Text;

public class GeminiCharacterHintProvider : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Room Assignment")]
    [SerializeField] private PuzzleFetcher roomPuzzleFetcher; // Direct reference to this room's PuzzleFetcher

    [Header("Gemini AI Settings")]
    [SerializeField, HideInInspector] private string geminiApiKey; // No hardcoded default
    private string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    [Header("UI Settings")]
    public bool autoGenerateUI = true;
    public Font uiFont;
    public Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color textColor = Color.white;
    public Color buttonColor = new Color(0.3f, 0.3f, 0.8f, 1f);
    public float hintDisplayTime = 10f;

    [Header("Manual UI Assignment (if not auto-generating)")]
    public GameObject hintPanel;
    public Text hintText;
    public Button closeButton;
    public GameObject loadingIndicator;

    [Header("Character Interaction")]
    public Outline characterOutline;
    public AudioSource audioSource;
    public AudioClip clickSound;

    [Header("Fallback Hints")]
    public string[] fallbackHints =
    {
        "Gondold át alaposan a kérdést!",
        "A válasz a részletekben rejlik.",
        "Nézd meg újra a lehetőségeket!",
        "Minden szónak jelentősége van.",
        "A logika vezéreljen, ne a sejtés!"
    };

    private Canvas uiCanvas;
    private GameObject generatedHintPanel;
    private Text generatedHintText;
    private Button generatedCloseButton;
    private GameObject generatedLoadingIndicator;
    private Text generatedLoadingText;

    private PuzzleFetcher puzzleFetcher;
    private Coroutine autoCloseCoroutine;
    private bool isRequestingHint = false;

    [System.Serializable]
    public class GeminiRequest
    {
        public GeminiContent[] contents;
    }

    [System.Serializable]
    public class GeminiContent
    {
        public GeminiPart[] parts;
    }

    [System.Serializable]
    public class GeminiPart
    {
        public string text;
    }

    [System.Serializable]
    public class GeminiResponse
    {
        public GeminiCandidate[] candidates;
    }

    [System.Serializable]
    public class GeminiCandidate
    {
        public GeminiContent content;
    }

    void Start()
    {
        // Use the assigned room PuzzleFetcher, or find one if not assigned
        if (roomPuzzleFetcher != null)
        {
            puzzleFetcher = roomPuzzleFetcher;
            Debug.Log($"Using assigned PuzzleFetcher for room: {roomPuzzleFetcher.name}");
        }
        else
        {
            // Fallback: try to find PuzzleFetcher in the same room (same parent or nearby)
            puzzleFetcher = FindPuzzleFetcherInRoom();
            if (puzzleFetcher == null)
            {
                Debug.LogWarning("No PuzzleFetcher assigned and none found in room. Using first available.");
                puzzleFetcher = FindObjectOfType<PuzzleFetcher>();
            }
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("Created EventSystem for UI interaction");
        }

        if (autoGenerateUI)
        {
            CreateUI();
        }
        else
        {
            SetupManualUI();
        }

        if (characterOutline == null)
            characterOutline = GetComponent<Outline>();

        if (characterOutline != null)
            characterOutline.enabled = false;
    }

    // Method to find PuzzleFetcher in the same room
    private PuzzleFetcher FindPuzzleFetcherInRoom()
    {
        // Option 1: Look for PuzzleFetcher in the same parent GameObject
        if (transform.parent != null)
        {
            PuzzleFetcher parentFetcher = transform.parent.GetComponentInChildren<PuzzleFetcher>();
            if (parentFetcher != null)
            {
                Debug.Log($"Found PuzzleFetcher in parent: {parentFetcher.name}");
                return parentFetcher;
            }
        }

        // Option 2: Look for PuzzleFetcher in the same root GameObject
        Transform rootTransform = transform.root;
        PuzzleFetcher rootFetcher = rootTransform.GetComponentInChildren<PuzzleFetcher>();
        if (rootFetcher != null)
        {
            Debug.Log($"Found PuzzleFetcher in root: {rootFetcher.name}");
            return rootFetcher;
        }

        return null;
    }

    // Public method to set the room's PuzzleFetcher (can be called from outside)
    public void SetRoomPuzzleFetcher(PuzzleFetcher fetcher)
    {
        roomPuzzleFetcher = fetcher;
        puzzleFetcher = fetcher;
        Debug.Log($"PuzzleFetcher set for room: {fetcher.name}");
    }

    private void CreateUI()
    {
        GameObject canvasGO = new GameObject("HintCanvas");
        uiCanvas = canvasGO.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 1000;
        uiCanvas.planeDistance = 0.1f;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();

        Debug.Log("Created dedicated HintCanvas with high sorting order");

        CreateHintPanel();
        CreateLoadingIndicator();

        Debug.Log("UI generated successfully!");
    }

    private void CreateHintPanel()
    {
        generatedHintPanel = new GameObject("HintPanel");
        generatedHintPanel.transform.SetParent(uiCanvas.transform, false);

        CanvasGroup canvasGroup = generatedHintPanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        RectTransform panelRect = generatedHintPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.25f);
        panelRect.anchorMax = new Vector2(0.6f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = generatedHintPanel.AddComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = true;

        Shadow shadow = generatedHintPanel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.7f);
        shadow.effectDistance = new Vector2(8, -8);

        Outline outline = generatedHintPanel.AddComponent<Outline>();
        outline.effectColor = new Color(1, 1, 1, 0.3f);
        outline.effectDistance = new Vector2(2, 2);

        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(generatedHintPanel.transform, false);

        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.85f);
        titleRect.anchorMax = new Vector2(0.95f, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "💡 Segítség";
        titleText.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.color = textColor;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;

        Shadow titleShadow = titleGO.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0, 0, 0, 0.8f);
        titleShadow.effectDistance = new Vector2(2, -2);

        GameObject hintTextGO = new GameObject("HintText");
        hintTextGO.transform.SetParent(generatedHintPanel.transform, false);

        RectTransform hintTextRect = hintTextGO.AddComponent<RectTransform>();
        hintTextRect.anchorMin = new Vector2(0.08f, 0.18f);
        hintTextRect.anchorMax = new Vector2(0.92f, 0.8f);
        hintTextRect.offsetMin = Vector2.zero;
        hintTextRect.offsetMax = Vector2.zero;

        generatedHintText = hintTextGO.AddComponent<Text>();
        generatedHintText.text = "";
        generatedHintText.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        generatedHintText.fontSize = 20;
        generatedHintText.color = textColor;
        generatedHintText.alignment = TextAnchor.MiddleCenter;
        generatedHintText.horizontalOverflow = HorizontalWrapMode.Wrap;
        generatedHintText.verticalOverflow = VerticalWrapMode.Overflow;

        Shadow hintShadow = hintTextGO.AddComponent<Shadow>();
        hintShadow.effectColor = new Color(0, 0, 0, 0.6f);
        hintShadow.effectDistance = new Vector2(1, -1);

        GameObject closeButtonGO = new GameObject("CloseButton");
        closeButtonGO.transform.SetParent(generatedHintPanel.transform, false);

        RectTransform closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(0.3f, 0.02f);
        closeButtonRect.anchorMax = new Vector2(0.7f, 0.15f);
        closeButtonRect.offsetMin = Vector2.zero;
        closeButtonRect.offsetMax = Vector2.zero;

        generatedCloseButton = closeButtonGO.AddComponent<Button>();
        Image closeButtonImage = closeButtonGO.AddComponent<Image>();
        closeButtonImage.color = buttonColor;

        Shadow buttonShadow = closeButtonGO.AddComponent<Shadow>();
        buttonShadow.effectColor = new Color(0, 0, 0, 0.5f);
        buttonShadow.effectDistance = new Vector2(3, -3);

        GameObject closeTextGO = new GameObject("CloseText");
        closeTextGO.transform.SetParent(closeButtonGO.transform, false);

        RectTransform closeTextRect = closeTextGO.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        Text closeText = closeTextGO.AddComponent<Text>();
        closeText.text = "Bezárás";
        closeText.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 18;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.fontStyle = FontStyle.Bold;

        generatedCloseButton.onClick.AddListener(CloseHint);

        ColorBlock colors = generatedCloseButton.colors;
        colors.highlightedColor = new Color(buttonColor.r * 1.3f, buttonColor.g * 1.3f, buttonColor.b * 1.3f, 1f);
        colors.pressedColor = new Color(buttonColor.r * 0.7f, buttonColor.g * 0.7f, buttonColor.b * 0.7f, 1f);
        colors.selectedColor = buttonColor;
        generatedCloseButton.colors = colors;

        generatedHintPanel.SetActive(false);

        Debug.Log("Hint panel created and positioned in front of character");
    }

    private void CreateLoadingIndicator()
    {
        generatedLoadingIndicator = new GameObject("LoadingIndicator");
        generatedLoadingIndicator.transform.SetParent(uiCanvas.transform, false);

        CanvasGroup loadingCanvasGroup = generatedLoadingIndicator.AddComponent<CanvasGroup>();
        loadingCanvasGroup.alpha = 1f;
        loadingCanvasGroup.interactable = false;
        loadingCanvasGroup.blocksRaycasts = true;

        RectTransform loadingRect = generatedLoadingIndicator.AddComponent<RectTransform>();
        loadingRect.anchorMin = new Vector2(0.25f, 0.4f);
        loadingRect.anchorMax = new Vector2(0.55f, 0.6f);
        loadingRect.offsetMin = Vector2.zero;
        loadingRect.offsetMax = Vector2.zero;

        Image loadingImage = generatedLoadingIndicator.AddComponent<Image>();
        loadingImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        Shadow loadingShadow = generatedLoadingIndicator.AddComponent<Shadow>();
        loadingShadow.effectColor = new Color(0, 0, 0, 0.8f);
        loadingShadow.effectDistance = new Vector2(5, -5);

        GameObject loadingTextGO = new GameObject("LoadingText");
        loadingTextGO.transform.SetParent(generatedLoadingIndicator.transform, false);

        RectTransform loadingTextRect = loadingTextGO.AddComponent<RectTransform>();
        loadingTextRect.anchorMin = new Vector2(0.05f, 0.05f);
        loadingTextRect.anchorMax = new Vector2(0.95f, 0.95f);
        loadingTextRect.offsetMin = Vector2.zero;
        loadingTextRect.offsetMax = Vector2.zero;

        generatedLoadingText = loadingTextGO.AddComponent<Text>();
        generatedLoadingText.text = "AI gondolkodik... 🤔";
        generatedLoadingText.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        generatedLoadingText.fontSize = 18;
        generatedLoadingText.color = Color.white;
        generatedLoadingText.alignment = TextAnchor.MiddleCenter;
        generatedLoadingText.fontStyle = FontStyle.Bold;

        Shadow loadingTextShadow = loadingTextGO.AddComponent<Shadow>();
        loadingTextShadow.effectColor = new Color(0, 0, 0, 0.7f);
        loadingTextShadow.effectDistance = new Vector2(2, -2);

        StartCoroutine(AnimateLoadingText());

        generatedLoadingIndicator.SetActive(false);

        Debug.Log("Loading indicator created and positioned near character");
    }

    private IEnumerator AnimateLoadingText()
    {
        string[] loadingStates = { "AI gondolkodik", "AI gondolkodik.", "AI gondolkodik..", "AI gondolkodik..." };
        int index = 0;

        while (true)
        {
            if (generatedLoadingText != null && generatedLoadingIndicator != null && generatedLoadingIndicator.activeInHierarchy)
            {
                generatedLoadingText.text = loadingStates[index] + " 🤔";
                index = (index + 1) % loadingStates.Length;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SetupManualUI()
    {
        if (hintPanel != null)
            hintPanel.SetActive(false);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseHint);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Character clicked for AI hint!");

        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);

        if (!isRequestingHint)
            StartCoroutine(RequestGeminiHint());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (characterOutline != null)
            characterOutline.enabled = true;

        Debug.Log("Character hovered");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (characterOutline != null)
            characterOutline.enabled = false;
    }

    private IEnumerator RequestGeminiHint()
    {
        if (string.IsNullOrEmpty(geminiApiKey) || geminiApiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            Debug.LogError("Gemini API key not set! Using fallback hint.");
            ShowFallbackHint();
            yield break;
        }

        isRequestingHint = true;

        ShowLoadingIndicator(true);

        string puzzleInfo = GetCurrentPuzzleInfo();

        // Create prompt for Gemini
        string prompt = CreateHintPrompt(puzzleInfo);

        // Create request
        GeminiRequest request = new GeminiRequest
        {
            contents = new GeminiContent[]
            {
                new GeminiContent
                {
                    parts = new GeminiPart[]
                    {
                        new GeminiPart { text = prompt }
                    }
                }
            }
        };

        string jsonData = JsonUtility.ToJson(request);
        Debug.Log("Sending request to Gemini: " + jsonData);

        // Send request to Gemini - Unity 2019 compatible
        UnityWebRequest www = new UnityWebRequest(geminiApiUrl + "?key=" + geminiApiKey, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        // Hide loading
        ShowLoadingIndicator(false);

        // Unity 2019 compatible error checking
        if (!www.isNetworkError && !www.isHttpError)
        {
            try
            {
                Debug.Log("Gemini response: " + www.downloadHandler.text);

                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(www.downloadHandler.text);

                if (response.candidates != null && response.candidates.Length > 0 &&
                    response.candidates[0].content.parts != null && response.candidates[0].content.parts.Length > 0)
                {
                    string hint = response.candidates[0].content.parts[0].text.Trim();
                    ShowHint(hint);
                }
                else
                {
                    Debug.LogWarning("Invalid response from Gemini API");
                    ShowFallbackHint();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing Gemini response: " + e.Message);
                Debug.LogError("Raw response: " + www.downloadHandler.text);
                ShowFallbackHint();
            }
        }
        else
        {
            Debug.LogError("Gemini API request failed. Network Error: " + www.isNetworkError +
                          ", HTTP Error: " + www.isHttpError);
            Debug.LogError("Error: " + www.error);
            Debug.LogError("Response: " + www.downloadHandler.text);
        }

        www.Dispose();
        isRequestingHint = false;
    }

    private void ShowLoadingIndicator(bool show)
    {
        if (autoGenerateUI && generatedLoadingIndicator != null)
        {
            generatedLoadingIndicator.SetActive(show);
            if (show)
            {
                generatedLoadingIndicator.transform.SetAsLastSibling();
            }
        }
        else if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(show);
        }
    }

    private string GetCurrentPuzzleInfo()
    {
        if (puzzleFetcher == null)
        {
            Debug.LogWarning("No PuzzleFetcher available for this room!");
            return "Játékos segítségre van szüksége egy puzzle megoldásához.";
        }

        try
        {
            string info = puzzleFetcher.GetCurrentPuzzleInfoForHint();
            Debug.Log($"Got puzzle info from {puzzleFetcher.name}: {info}");
            return info;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error getting puzzle info from {puzzleFetcher.name}: {e.Message}");
            return "Játékos segítségre van szüksége a jelenlegi puzzleval kapcsolatban.";
        }
    }

    private string CreateHintPrompt(string puzzleInfo)
    {
        return $@"Te egy okos és barátságos karakter vagy egy escape room játékban. 
A játékos rád kattintott, hogy segítséget kérjen a jelenlegi puzzle megoldásához.

Puzzle információ: {puzzleInfo}

Kérlek, adj egy rövid, hasznos tippet magyarul, amely:
1. NEM árulja el a teljes választ
2. Segít a gondolkodásban vagy iránymutatást ad
3. Maximum 2-3 mondat hosszú
4. Barátságos és bátorító hangnemű
5. Konkrét, de nem túl nyilvánvaló

Válaszolj csak a tippel, egyéb szöveg nélkül.";
    }

    private void ShowHint(string hint)
    {
        Text targetHintText = autoGenerateUI ? generatedHintText : hintText;
        GameObject targetHintPanel = autoGenerateUI ? generatedHintPanel : hintPanel;

        if (targetHintPanel == null || targetHintText == null)
        {
            Debug.LogWarning("Hint panel or text not available!");
            return;
        }

        targetHintText.text = hint;
        targetHintPanel.SetActive(true);

        // Ensure the panel is on top
        if (autoGenerateUI && generatedHintPanel != null)
        {
            generatedHintPanel.transform.SetAsLastSibling();
        }

        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        if (hintDisplayTime > 0)
            autoCloseCoroutine = StartCoroutine(AutoCloseHint());

        Debug.Log("Showing hint: " + hint);
    }

    private void ShowFallbackHint()
    {
        string hint = fallbackHints[Random.Range(0, fallbackHints.Length)];
        Debug.Log("Using fallback hint: " + hint);
        ShowHint(hint);
    }

    private IEnumerator AutoCloseHint()
    {
        yield return new WaitForSeconds(hintDisplayTime);
        CloseHint();
    }

    public void CloseHint()
    {
        GameObject targetHintPanel = autoGenerateUI ? generatedHintPanel : hintPanel;

        if (targetHintPanel != null)
            targetHintPanel.SetActive(false);

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        Debug.Log("Hint panel closed");
    }

    // Method to manually set API key (for security)
    public void SetGeminiApiKey(string apiKey)
    {
        geminiApiKey = apiKey;
    }

    // Method to customize UI colors at runtime
    public void SetUIColors(Color panelCol, Color textCol, Color buttonCol)
    {
        panelColor = panelCol;
        textColor = textCol;
        buttonColor = buttonCol;

        if (autoGenerateUI && generatedHintPanel != null)
        {
            // Update existing UI colors
            generatedHintPanel.GetComponent<Image>().color = panelColor;
            generatedHintText.color = textColor;
            generatedCloseButton.GetComponent<Image>().color = buttonColor;
        }
    }

    // Method to test fallback hints without API call
    [ContextMenu("Test Fallback Hint")]
    public void TestFallbackHint()
    {
        ShowFallbackHint();
    }

    // Validation method to check UI setup
    [ContextMenu("Validate UI Setup")]
    public void ValidateUISetup()
    {
        Debug.Log("=== UI Setup Validation ===");

        if (uiCanvas == null)
            Debug.LogError("No Canvas found!");
        else
            Debug.Log($"Canvas found: {uiCanvas.name}, Render Mode: {uiCanvas.renderMode}");

        if (autoGenerateUI)
        {
            Debug.Log($"Generated Hint Panel: {(generatedHintPanel != null ? "OK" : "NULL")}");
            Debug.Log($"Generated Hint Text: {(generatedHintText != null ? "OK" : "NULL")}");
            Debug.Log($"Generated Close Button: {(generatedCloseButton != null ? "OK" : "NULL")}");
            Debug.Log($"Generated Loading Indicator: {(generatedLoadingIndicator != null ? "OK" : "NULL")}");
        }
        else
        {
            Debug.Log($"Manual Hint Panel: {(hintPanel != null ? "OK" : "NULL")}");
            Debug.Log($"Manual Hint Text: {(hintText != null ? "OK" : "NULL")}");
            Debug.Log($"Manual Close Button: {(closeButton != null ? "OK" : "NULL")}");
            Debug.Log($"Manual Loading Indicator: {(loadingIndicator != null ? "OK" : "NULL")}");
        }

        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        Debug.Log($"EventSystem: {(eventSystem != null ? "OK" : "MISSING")}");

        // Debug room assignment
        Debug.Log($"Room PuzzleFetcher: {(roomPuzzleFetcher != null ? roomPuzzleFetcher.name : "NULL")}");
        Debug.Log($"Active PuzzleFetcher: {(puzzleFetcher != null ? puzzleFetcher.name : "NULL")}");
    }
}

// Add this helper script to make the character work with your gaze system
public class GazeActivateCharacter : MonoBehaviour
{
    // This empty class allows your GazeAutoClick to detect this character
    // as a valid gaze target, just like GazeActivatePostit
}