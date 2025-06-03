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
    public Button[] answerButtons;
    private Puzzle currentPuzzle;


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

        Puzzle[] puzzles = JsonHelper.FromJson<Puzzle>(FixJsonArray(www.downloadHandler.text));
        if (puzzles.Length == 0)
        {
            Debug.LogWarning("No puzzles received.");
            yield break;
        }

        currentPuzzle = puzzles[0];

        questionText.text = currentPuzzle.question;

        for (int i = 0; i < optionFields.Length; i++)
        {
            if (i < currentPuzzle.possible_answers.Length)
            {
                optionFields[i].text = currentPuzzle.possible_answers[i];
                int index = i;

                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
            }
            else
            {
                optionFields[i].text = "";
                answerButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public GameObject door;
    public Animator doorAnimator;
    public GameObject littleDoor;

    public void OnAnswerSelected(int index)
    {
        string selectedAnswer = optionFields[index].text;
        Debug.Log("Selected Answer: " + selectedAnswer);

        if (selectedAnswer == currentPuzzle.correct_answer)
        {
            Debug.Log("Correct Answer!");

            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger("Open");
                doorAnimator.SetTrigger("StayOpen");
            }

            if (door != null)
            {
                door.layer = LayerMask.NameToLayer("Default");
            }
        }
        else
        {
            Debug.Log("Wrong Answer!");
        }
    }



    private string FixJsonArray(string json)
    {
        return "{\"Items\":" + json + "}";
    }
}
