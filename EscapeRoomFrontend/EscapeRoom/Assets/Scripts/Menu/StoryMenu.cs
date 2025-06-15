using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using SimpleJSON;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StoryMenu : MonoBehaviour
{
    public Transform storyButtonContainer;
    public Button storyButtonPrefab;
    public Dropdown typeDropdown, difficultyDropdown;
    public Text outputText;

    private string baseUrl = "https://escaperoom-fastapi.azurewebsites.net/";

    private Dictionary<string, string> typeMapReverse = new Dictionary<string, string>
    {
        { "mathematics", "Matematika" },
        { "literature", "Magyar irodalom" },
        { "informatics", "Informatika" }
    };

    private Dictionary<string, string> typeMap = new Dictionary<string, string>
    {
        { "Matematika", "mathematics" },
        { "Magyar irodalom", "literature" },
        { "Informatika", "informatics" }
    };

    private Dictionary<string, string> difficultyMap = new Dictionary<string, string>
    {
        { "Könnyű", "könnyű" },
        { "Közepes", "közepes" },
        { "Nehéz", "nehéz" }
    };

    private Dictionary<int, string> storyTexts = new Dictionary<int, string>();
    private Dictionary<int, string> storyDifficulties = new Dictionary<int, string>();

    void Start()
    {
        GetRandomStories();
        outputText.enabled = false;
    }

    public void GetRandomStories()
    {
        StartCoroutine(Get("/stories/random", DisplayStoriesWithClear));
    }

    public void GenerateStory()
    {
        string selectedTypeHu = typeDropdown.options[typeDropdown.value].text;
        string selectedDifficultyHu = difficultyDropdown.options[difficultyDropdown.value].text;

        if (!typeMap.ContainsKey(selectedTypeHu) || !difficultyMap.ContainsKey(selectedDifficultyHu))
        {
            outputText.enabled = true;
            outputText.text = "Ismeretlen típus vagy nehézség.";
            StartCoroutine(HideOutputAfterSeconds(5));
            return;
        }

        string type = typeMap[selectedTypeHu];
        string difficulty = difficultyMap[selectedDifficultyHu];

        StartCoroutine(GenerateAndUploadStory(type, difficulty));
    }

    IEnumerator GenerateAndUploadStory(string type, string difficulty)
    {
        string endpoint = $"/generate/{type}/{difficulty}";

        UnityWebRequest genRequest = UnityWebRequest.Get(baseUrl + endpoint);
        yield return genRequest.SendWebRequest();

        if (genRequest.isNetworkError || genRequest.isHttpError)
        {
            outputText.enabled = true;
            outputText.text = "Hiba a történet generálásakor: " + genRequest.error;
            StartCoroutine(HideOutputAfterSeconds(5));
            yield break;
        }

        string rawResponse = genRequest.downloadHandler.text.Trim();

        if (rawResponse.StartsWith("\"") && rawResponse.EndsWith("\""))
        {
            rawResponse = rawResponse.Substring(1, rawResponse.Length - 2);
            rawResponse = rawResponse.Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        if (rawResponse.StartsWith("```json"))
        {
            rawResponse = rawResponse.Replace("```json", "").Trim();
        }
        if (rawResponse.EndsWith("```"))
        {
            rawResponse = rawResponse.Substring(0, rawResponse.Length - 3).Trim();
        }

        var generated = JSON.Parse(rawResponse);

        if (generated == null)
        {
            outputText.enabled = true;
            outputText.text = "Hiba: JSON feldolgozási hiba";
            StartCoroutine(HideOutputAfterSeconds(5));
            yield break;
        }

        string description = generated["story"];
        JSONArray puzzles = generated["puzzles"].AsArray;

        if (string.IsNullOrEmpty(description))
        {
            outputText.enabled = true;
            outputText.text = "Hiba: Üres történet generálódott";
            StartCoroutine(HideOutputAfterSeconds(5));
            yield break;
        }

        JSONObject storyJson = new JSONObject();
        storyJson["description"] = description;
        storyJson["difficulty"] = difficulty;
        storyJson["type"] = type;

        UnityWebRequest storyPost = new UnityWebRequest(baseUrl + "/story", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(storyJson.ToString());
        storyPost.uploadHandler = new UploadHandlerRaw(bodyRaw);
        storyPost.downloadHandler = new DownloadHandlerBuffer();
        storyPost.SetRequestHeader("Content-Type", "application/json");
        yield return storyPost.SendWebRequest();

        if (storyPost.isNetworkError || storyPost.isHttpError)
        {
            outputText.enabled = true;
            outputText.text = "Hiba a történet feltöltésekor: " + storyPost.error;
            StartCoroutine(HideOutputAfterSeconds(5));
            yield break;
        }

        var storyResponse = JSON.Parse(storyPost.downloadHandler.text);
        if (storyResponse == null || storyResponse["story_id"] == null)
        {
            outputText.enabled = true;
            outputText.text = "Hiba: Érvénytelen válasz a szerver-től";
            StartCoroutine(HideOutputAfterSeconds(5));
            yield break;
        }

        int storyId = storyResponse["story_id"].AsInt;

        bool allPuzzlesUploaded = true;
        int successfulUploads = 0;
        int totalPuzzles = 0;

        if (puzzles != null && puzzles.Count > 0)
        {
            bool hasNestedStructure = false;
            if (puzzles.Count > 0)
            {
                var firstPuzzle = puzzles[0];
                hasNestedStructure = firstPuzzle["puzzle"] != null;
            }

            totalPuzzles = puzzles.Count;

            for (int puzzleIndex = 0; puzzleIndex < puzzles.Count; puzzleIndex++)
            {
                JSONNode puzzleContainer = puzzles[puzzleIndex];

                if (puzzleContainer == null)
                {
                    allPuzzlesUploaded = false;
                    continue;
                }

                JSONNode puzzle = hasNestedStructure ? puzzleContainer["puzzle"] : puzzleContainer;

                if (puzzle == null)
                {
                    allPuzzlesUploaded = false;
                    continue;
                }

                string question = puzzle["question"];
                JSONArray possibleAnswers = puzzle["possible_answers"].AsArray;
                string correctAnswer = puzzle["correct_answer"];

                if (string.IsNullOrEmpty(question) || possibleAnswers == null || string.IsNullOrEmpty(correctAnswer))
                {
                    allPuzzlesUploaded = false;
                    continue;
                }

                JSONObject puzzleJson = new JSONObject();
                puzzleJson["story_id"] = storyId;
                puzzleJson["question"] = question;
                puzzleJson["possible_answers"] = possibleAnswers;
                puzzleJson["correct_answer"] = correctAnswer;

                UnityWebRequest puzzlePost = new UnityWebRequest(baseUrl + "/puzzle", "POST");
                byte[] puzzleRaw = System.Text.Encoding.UTF8.GetBytes(puzzleJson.ToString());
                puzzlePost.uploadHandler = new UploadHandlerRaw(puzzleRaw);
                puzzlePost.downloadHandler = new DownloadHandlerBuffer();
                puzzlePost.SetRequestHeader("Content-Type", "application/json");
                yield return puzzlePost.SendWebRequest();

                if (puzzlePost.isNetworkError || puzzlePost.isHttpError)
                {
                    allPuzzlesUploaded = false;
                    continue;
                }
                else
                {
                    successfulUploads++;
                }
            }
        }

        if (!allPuzzlesUploaded || (totalPuzzles > 0 && successfulUploads != totalPuzzles))
        {
            outputText.enabled = true;
            outputText.text = $"Hiba: Nem sikerült az összes puzzle feltöltése ({successfulUploads}/{totalPuzzles})";
            StartCoroutine(HideOutputAfterSeconds(5));
            yield break;
        }

        if (storyButtonContainer.childCount >= 4)
        {
            Destroy(storyButtonContainer.GetChild(0).gameObject);
        }

        Button btn = Instantiate(storyButtonPrefab, storyButtonContainer);
        string typeHu = typeMapReverse.ContainsKey(type) ? typeMapReverse[type] : type;
        string fullText = $"{typeHu} ({difficulty})\n{description}";
        btn.GetComponentInChildren<Text>().text = fullText;

        storyTexts[storyId] = fullText;
        storyDifficulties[storyId] = difficulty;

        btn.onClick.AddListener(() => SelectStory(storyId));

        outputText.enabled = true;
        outputText.text = "Sikeresen generált és feltöltött történet a következő ID-val: " + storyId;
        StartCoroutine(HideOutputAfterSeconds(5));
    }

    void DisplayStoriesWithClear(string json)
    {
        foreach (Transform child in storyButtonContainer)
        {
            Destroy(child.gameObject);
        }

        var stories = JSON.Parse(json).AsArray;

        if (stories != null)
        {
            foreach (JSONNode story in stories)
            {
                CreateStoryButton(story);
            }
        }
    }

    void CreateStoryButton(JSONNode story)
    {
        string typeEn = story["type"];
        string difficulty = story["difficulty"];
        string description = story["description"];
        int id = story["id"].AsInt;

        string typeHu = typeMapReverse.ContainsKey(typeEn) ? typeMapReverse[typeEn] : typeEn;

        string fullText = $"{typeHu} ({difficulty})\n{description}";
        storyTexts[id] = fullText;
        storyDifficulties[id] = difficulty;

        Button btn = Instantiate(storyButtonPrefab, storyButtonContainer);
        btn.GetComponentInChildren<Text>().text = fullText;
        btn.onClick.AddListener(() => SelectStory(id));
    }

    void SelectStory(int id)
    {
        if (!storyTexts.ContainsKey(id))
        {
            outputText.enabled = true;
            outputText.text = $"Nem található történet az ID-val: {id}";
            return;
        }

        string story = storyTexts[id];
        string difficulty = storyDifficulties.ContainsKey(id) ? storyDifficulties[id] : "közepes";

        PlayerPrefs.SetString("SelectedStory", story);
        PlayerPrefs.SetInt("SelectedStoryId", id);
        PlayerPrefs.SetString("SelectedDifficulty", difficulty);
        PlayerPrefs.Save();

        SceneManager.LoadScene("StoryIntroScene");
    }

    IEnumerator Get(string endpoint, System.Action<string> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get(baseUrl + endpoint);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            outputText.text = "Error: " + www.error;
        }
        else
        {
            callback(www.downloadHandler.text);
        }
    }

    IEnumerator HideOutputAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        outputText.enabled = false;
    }
}