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
    public int puzzleIndex = 0;
    private Puzzle[] allPuzzles;
    private ColorBlock[] originalColors;




    IEnumerator Start()
    {
        Debug.Log("PuzzleFetcher.Start() called");

        originalColors = new ColorBlock[answerButtons.Length];
        for (int i = 0; i < answerButtons.Length; i++)
        {
            originalColors[i] = answerButtons[i].colors;
        }

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
            FetchPuzzles(storyId);
            yield break;
        }

        allPuzzles = JsonHelper.FromJson<Puzzle>(FixJsonArray(www.downloadHandler.text));

        if (puzzleIndex < 0 || puzzleIndex >= allPuzzles.Length)
        {
            Debug.LogWarning("Invalid puzzle index!");
            yield break;
        }

        ShowPuzzle(allPuzzles[puzzleIndex]);
    }

    void ShowPuzzle(Puzzle puzzle)
    {
        currentPuzzle = puzzle;
        questionText.text = puzzle.question;

        for (int i = 0; i < optionFields.Length; i++)
        {
            if (i < puzzle.possible_answers.Length)
            {
                optionFields[i].text = puzzle.possible_answers[i];
                int index = i;

                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
                answerButtons[i].gameObject.SetActive(true);
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

        bool isCorrect = selectedAnswer == currentPuzzle.correct_answer;

        if (isCorrect)
        {
            Debug.Log("Correct Answer!");
            SetButtonColor(answerButtons[index], Color.green);

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
            SetButtonColor(answerButtons[index], Color.red);
        }

        StartCoroutine(ResetButtonColorAfterDelay(answerButtons[index], index, 3f));
    }


    private void SetButtonColor(Button button, Color color)
    {
        ColorBlock cb = button.colors;
        cb.normalColor = color;
        cb.highlightedColor = color;
        cb.pressedColor = color;
        cb.selectedColor = color;
        button.colors = cb;
    }

    private IEnumerator ResetButtonColorAfterDelay(Button button, int index, float delay)
    {
        yield return new WaitForSeconds(delay);

        button.colors = originalColors[index];
    }


    private string FixJsonArray(string json)
    {
        return "{\"Items\":" + json + "}";
    }
}
