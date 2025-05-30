using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PuzzleFetcher : MonoBehaviour
{
    [System.Serializable]
    public class Puzzle
    {
        public int id;
        public int story_id;
        public string question;
        public string[] possible_answers;
        public string correct_answer;
    }

    public Text questionText;
    public Text[] optionFields;

    IEnumerator Start()
    {
        Debug.Log("PuzzleFetcher.Start() called");

        int storyId = PlayerPrefs.GetInt("SelectedStoryId", -1);
        if (storyId == -1)
        {
            Debug.LogError("No story ID found in PlayerPrefs!");
            yield break;
        }

        Debug.Log($"Fetched storyId = {storyId}");

        yield return StartCoroutine(FetchPuzzles(storyId));
    }


    IEnumerator FetchPuzzles(int storyId)
    {

        string url = $"https://escaperoom-fastapi.azurewebsites.net/puzzles/{storyId}";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Error fetching puzzles: " + www.error);
            yield break;
        }

        // Parse directly the array
        Puzzle[] puzzles = JsonHelper.FromJson<Puzzle>(FixJsonArray(www.downloadHandler.text));

        if (puzzles.Length == 0)
        {
            Debug.LogWarning("No puzzles received.");
            yield break;
        }

        // Show the first puzzle
        Puzzle first = puzzles[0];
        questionText.text = first.question;

        for (int i = 0; i < optionFields.Length; i++)
        {
            if (i < first.possible_answers.Length)
                optionFields[i].text = first.possible_answers[i];
            else
                optionFields[i].text = ""; // Clear any extra UI fields
        }
    }

    // Fix raw array JSON for Unity
    private string FixJsonArray(string json)
    {
        return "{\"Items\":" + json + "}";
    }
}
