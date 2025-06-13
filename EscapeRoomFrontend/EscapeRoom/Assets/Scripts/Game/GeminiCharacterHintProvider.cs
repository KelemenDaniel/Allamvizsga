using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Text;

public class GeminiCharacterHintProvider : MonoBehaviour, IPointerClickHandler
{
    [Header("Room Assignment")]
    [SerializeField] private PuzzleFetcher roomPuzzleFetcher;

    [Header("Gemini AI Settings")]
    [SerializeField, HideInInspector] private string geminiApiKey; 
    private string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    [Header("VR World Space Settings")]
    [SerializeField] private Transform playerCamera;

    private float canvasDistance = 2.0f;
    private float canvasHeight = 1.5f;
    private Vector2 canvasSize = new Vector2(2.0f, 1.5f);
    private float canvasScale = 0.005f;
    private bool facePlayer = true;

    [Header("UI Settings")]
    public Font uiFont;

    private Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    private Color textColor = Color.white;
    private Color buttonColor = new Color(0.3f, 0.3f, 0.8f, 1f);
    private float hintDisplayTime = 10f;

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

    private GameObject canvasGameObject;
    private Vector3 canvasBasePosition;
    private Quaternion canvasBaseRotation;

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
        string envKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            SetGeminiApiKey(envKey);
            Debug.Log("Gemini API key loaded from environment variable.");
        }
        else
        {
            Debug.LogWarning("Environment variable 'GEMINI_API_KEY' not found. Using fallback hints.");
        }

        if (roomPuzzleFetcher != null)
        {
            puzzleFetcher = roomPuzzleFetcher;
            Debug.Log($"Using assigned PuzzleFetcher for room: {roomPuzzleFetcher.name}");
        }
        else
        {
            puzzleFetcher = FindPuzzleFetcherInRoom();
            if (puzzleFetcher == null)
            {
                Debug.LogWarning("No PuzzleFetcher assigned and none found in room. Using first available.");
                puzzleFetcher = FindObjectOfType<PuzzleFetcher>();
            }
        }

        if (playerCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
            {
                if (cam.gameObject.name.Contains("Eye") || cam.gameObject.name.Contains("Camera") || cam.CompareTag("MainCamera"))
                {
                    playerCamera = cam.transform;
                    Debug.Log($"Auto-found player camera: {playerCamera.name}");
                    break;
                }
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main?.transform;
                Debug.Log("Using Main Camera as player camera");
            }
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("Created EventSystem for UI interaction");
        }

        CreateVRUI();
    }

    void Update()
    {
        if (facePlayer && canvasGameObject != null && canvasGameObject.activeInHierarchy && playerCamera != null)
        {
            if (playerCamera != null && canvasGameObject != null)
            {
                Vector3 directionToCamera = (playerCamera.transform.position - canvasGameObject.transform.position).normalized;
                canvasGameObject.transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }
    }

    private PuzzleFetcher FindPuzzleFetcherInRoom()
    {
        if (transform.parent != null)
        {
            PuzzleFetcher parentFetcher = transform.parent.GetComponentInChildren<PuzzleFetcher>();
            if (parentFetcher != null)
            {
                Debug.Log($"Found PuzzleFetcher in parent: {parentFetcher.name}");
                return parentFetcher;
            }
        }

        Transform rootTransform = transform.root;
        PuzzleFetcher rootFetcher = rootTransform.GetComponentInChildren<PuzzleFetcher>();
        if (rootFetcher != null)
        {
            Debug.Log($"Found PuzzleFetcher in root: {rootFetcher.name}");
            return rootFetcher;
        }

        return null;
    }

    public void SetRoomPuzzleFetcher(PuzzleFetcher fetcher)
    {
        roomPuzzleFetcher = fetcher;
        puzzleFetcher = fetcher;
        Debug.Log($"PuzzleFetcher set for room: {fetcher.name}");
    }

    private void CreateVRUI()
    {
        canvasGameObject = new GameObject("HintCanvas_WorldSpace");

        Vector3 characterForward = transform.forward;
        canvasBasePosition = transform.position + characterForward * canvasDistance + Vector3.up * canvasHeight;

        if (playerCamera != null)
        {
            Vector3 directionToCamera = (playerCamera.transform.position - canvasBasePosition).normalized;
            canvasBaseRotation = Quaternion.LookRotation(directionToCamera);
        }
        else
        {
            canvasBaseRotation = Quaternion.LookRotation(-characterForward);
        }

        canvasGameObject.transform.position = canvasBasePosition;
        canvasGameObject.transform.rotation = canvasBaseRotation;

        uiCanvas = canvasGameObject.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.WorldSpace;
        uiCanvas.worldCamera = playerCamera?.GetComponent<Camera>();

        RectTransform canvasRect = canvasGameObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize * 150f;
        canvasRect.localScale = Vector3.one * canvasScale * 1.5f;


        CanvasScaler scaler = canvasGameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        GraphicRaycaster raycaster = canvasGameObject.AddComponent<GraphicRaycaster>();

        Debug.Log($"Created VR WorldSpace HintCanvas at position: {canvasBasePosition}");

        CreateHintPanel();
        CreateLoadingIndicator();

        Debug.Log("VR UI generated successfully!");
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
        panelRect.anchorMin = new Vector2(0.02f, 0.02f);
        panelRect.anchorMax = new Vector2(0.98f, 0.98f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.transform.localRotation = Quaternion.Euler(0, 180, 0);
        panelRect.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

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
        titleRect.anchorMin = new Vector2(0.02f, 0.88f);
        titleRect.anchorMax = new Vector2(0.98f, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = "Segítség";
        titleText.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 18;
        titleText.color = textColor;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        titleText.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        titleText.transform.localRotation = Quaternion.identity;

        Shadow titleShadow = titleGO.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0, 0, 0, 0.8f);
        titleShadow.effectDistance = new Vector2(2, -2);

        GameObject hintTextGO = new GameObject("HintText");
        hintTextGO.transform.SetParent(generatedHintPanel.transform, false);

        RectTransform hintTextRect = hintTextGO.AddComponent<RectTransform>();
        hintTextRect.anchorMin = new Vector2(0f, 0.15f);
        hintTextRect.anchorMax = new Vector2(1f, 0.98f);
        hintTextRect.offsetMin = Vector2.zero;
        hintTextRect.offsetMax = Vector2.zero;

        generatedHintText = hintTextGO.AddComponent<Text>();
        generatedHintText.text = "";
        generatedHintText.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        generatedHintText.fontSize = 22;
        generatedHintText.color = textColor;
        generatedHintText.alignment = TextAnchor.UpperLeft;
        generatedHintText.horizontalOverflow = HorizontalWrapMode.Wrap;
        generatedHintText.verticalOverflow = VerticalWrapMode.Overflow;
        generatedHintText.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        Shadow hintShadow = hintTextGO.AddComponent<Shadow>();
        hintShadow.effectColor = new Color(0, 0, 0, 0.6f);
        hintShadow.effectDistance = new Vector2(1, -1);

        GameObject closeButtonGO = new GameObject("CloseButton");
        closeButtonGO.transform.SetParent(generatedHintPanel.transform, false);

        RectTransform closeButtonRect = closeButtonGO.AddComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(0.25f, 0.02f);
        closeButtonRect.anchorMax = new Vector2(0.75f, 0.14f);
        closeButtonRect.offsetMin = Vector2.zero;
        closeButtonRect.offsetMax = Vector2.zero;

        generatedCloseButton = closeButtonGO.AddComponent<Button>();
        Image closeButtonImage = closeButtonGO.AddComponent<Image>();
        closeButtonImage.color = buttonColor;
        generatedCloseButton.transform.localScale = Vector3.one;
        generatedCloseButton.transform.localRotation = Quaternion.identity;

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
        closeText.fontSize = 16;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.fontStyle = FontStyle.Bold;
        closeText.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        closeText.transform.localRotation = Quaternion.identity;

        generatedCloseButton.onClick.AddListener(CloseHint);

        ColorBlock colors = generatedCloseButton.colors;
        colors.highlightedColor = new Color(buttonColor.r * 1.3f, buttonColor.g * 1.3f, buttonColor.b * 1.3f, 1f);
        colors.pressedColor = new Color(buttonColor.r * 0.7f, buttonColor.g * 0.7f, buttonColor.b * 0.7f, 1f);
        colors.selectedColor = buttonColor;
        generatedCloseButton.colors = colors;

        generatedHintPanel.SetActive(false);

        Debug.Log("VR Hint panel created with maximum width text field");
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
        loadingRect.anchorMin = new Vector2(0.2f, 0.35f);
        loadingRect.anchorMax = new Vector2(0.8f, 0.65f);
        loadingRect.offsetMin = Vector2.zero;
        loadingRect.offsetMax = Vector2.zero;
        loadingRect.transform.localRotation = Quaternion.Euler(0, 180, 0);
        loadingRect.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);



        Image loadingImage = generatedLoadingIndicator.AddComponent<Image>();
        loadingImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        Shadow loadingShadow = generatedLoadingIndicator.AddComponent<Shadow>();
        loadingShadow.effectColor = new Color(0, 0, 0, 0.8f);
        loadingShadow.effectDistance = new Vector2(8, -8);

        GameObject loadingTextGO = new GameObject("LoadingText");
        loadingTextGO.transform.SetParent(generatedLoadingIndicator.transform, false);

        RectTransform loadingTextRect = loadingTextGO.AddComponent<RectTransform>();
        loadingTextRect.anchorMin = new Vector2(0.05f, 0.05f);
        loadingTextRect.anchorMax = new Vector2(0.95f, 0.95f);
        loadingTextRect.offsetMin = Vector2.zero;
        loadingTextRect.offsetMax = Vector2.zero;


        generatedLoadingText = loadingTextGO.AddComponent<Text>();
        generatedLoadingText.text = "AI gondolkodik...";
        generatedLoadingText.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        generatedLoadingText.fontSize = 16;
        generatedLoadingText.color = Color.white;
        generatedLoadingText.alignment = TextAnchor.MiddleCenter;
        generatedLoadingText.fontStyle = FontStyle.Bold;
        generatedLoadingText.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

        Shadow loadingTextShadow = loadingTextGO.AddComponent<Shadow>();
        loadingTextShadow.effectColor = new Color(0, 0, 0, 0.7f);
        loadingTextShadow.effectDistance = new Vector2(3, -3);

        StartCoroutine(AnimateLoadingText());

        generatedLoadingIndicator.SetActive(false);

        Debug.Log("VR Loading indicator created in world space");
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

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Character clicked for AI hint!");

        if (!isRequestingHint)
            StartCoroutine(RequestGeminiHint());
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

        string prompt = CreateHintPrompt(puzzleInfo);

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

        UnityWebRequest www = new UnityWebRequest(geminiApiUrl + "?key=" + geminiApiKey, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        ShowLoadingIndicator(false);

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
        if (generatedLoadingIndicator != null)
        {
            generatedLoadingIndicator.SetActive(show);
            if (show)
            {
                generatedLoadingIndicator.transform.SetAsLastSibling();
            }
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
        Text targetHintText = generatedHintText;
        GameObject targetHintPanel = generatedHintPanel ;

        if (targetHintPanel == null || targetHintText == null)
        {
            Debug.LogWarning("Hint panel or text not available!");
            return;
        }

        targetHintText.text = hint;
        targetHintPanel.SetActive(true);

        if (canvasGameObject != null)
        {
            UpdateCanvasPosition();
        }

        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        if (hintDisplayTime > 0)
            autoCloseCoroutine = StartCoroutine(AutoCloseHint());

        Debug.Log("Showing VR hint: " + hint);
    }

    private void UpdateCanvasPosition()
    {
        if (canvasGameObject == null) return;

        Vector3 characterForward = transform.forward;
        Vector3 newPosition = transform.position + characterForward * canvasDistance + Vector3.up * canvasHeight;

        canvasGameObject.transform.position = newPosition;

        if (!facePlayer)
        {
            canvasGameObject.transform.rotation = Quaternion.LookRotation(characterForward);
        }
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
        GameObject targetHintPanel = generatedHintPanel;

        if (targetHintPanel != null)
            targetHintPanel.SetActive(false);

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        Debug.Log("VR Hint panel closed");
    }

    public void SetGeminiApiKey(string apiKey)
    {
        geminiApiKey = apiKey;
    }

    public void SetVRCanvasSettings(float distance, float height, Vector2 size, float scale)
    {
        canvasDistance = distance;
        canvasHeight = height;
        canvasSize = size;
        canvasScale = scale;

        if (canvasGameObject != null)
        {
            UpdateCanvasPosition();
            RectTransform canvasRect = canvasGameObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = canvasSize * 100f;
            canvasRect.localScale = Vector3.one * canvasScale;
        }
    }

    public void SetPlayerCamera(Transform camera)
    {
        playerCamera = camera;
        if (uiCanvas != null)
            uiCanvas.worldCamera = camera.GetComponent<Camera>();
    }

    public void SetUIColors(Color panelCol, Color textCol, Color buttonCol)
    {
        panelColor = panelCol;
        textColor = textCol;
        buttonColor = buttonCol;

        if (generatedHintPanel != null)
        {
            generatedHintPanel.GetComponent<Image>().color = panelColor;
            generatedHintText.color = textColor;
            generatedCloseButton.GetComponent<Image>().color = buttonColor;
        }
    }

    [ContextMenu("Test Fallback Hint")]
    public void TestFallbackHint()
    {
        ShowFallbackHint();
    }

    [ContextMenu("Validate UI Setup")]
    public void ValidateUISetup()
    {
        Debug.Log("=== UI Setup Validation ===");

        if (uiCanvas == null)
            Debug.LogError("No Canvas found!");
        else
            Debug.Log($"Canvas found: {uiCanvas.name}, Render Mode: {uiCanvas.renderMode}");

        Debug.Log($"Generated Hint Panel: {(generatedHintPanel != null ? "OK" : "NULL")}");
        Debug.Log($"Generated Hint Text: {(generatedHintText != null ? "OK" : "NULL")}");
        Debug.Log($"Generated Close Button: {(generatedCloseButton != null ? "OK" : "NULL")}");
        Debug.Log($"Generated Loading Indicator: {(generatedLoadingIndicator != null ? "OK" : "NULL")}");

        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        Debug.Log($"EventSystem: {(eventSystem != null ? "OK" : "MISSING")}");

        Debug.Log($"Room PuzzleFetcher: {(roomPuzzleFetcher != null ? roomPuzzleFetcher.name : "NULL")}");
        Debug.Log($"Active PuzzleFetcher: {(puzzleFetcher != null ? puzzleFetcher.name : "NULL")}");
    }
}

public class GazeActivateCharacter : MonoBehaviour
{
}