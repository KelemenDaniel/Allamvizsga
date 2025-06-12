using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GazeAutoClick : MonoBehaviour
{
    public float gazeClickDelay = 2.0f;
    public Image gazeProgressImage;
    private float gazeTimer = 0f;
    private GameObject lastGazedObject = null;

    void Update()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Screen.width / 2f, Screen.height / 2f)
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        GameObject currentGazed = results.Count > 0 ? results[0].gameObject : null;

        Button button = currentGazed?.GetComponentInParent<Button>();
        Dropdown dropdown = currentGazed?.GetComponentInParent<Dropdown>();
        Toggle toggle = currentGazed?.GetComponentInParent<Toggle>();
        GazeActivatePostit postit = currentGazed?.GetComponentInParent<GazeActivatePostit>();
        GeminiCharacterHintProvider character = currentGazed?.GetComponentInParent<GeminiCharacterHintProvider>();

        bool validGazeTarget = button || dropdown || toggle || postit || character;

        if (results.Count > 0)
            Debug.Log("Gaze hit: " + results[0].gameObject.name);
        else
            Debug.Log("Gaze hit: nothing");

        if (validGazeTarget)
        {
            if (currentGazed != lastGazedObject)
            {
                if (lastGazedObject != null)
                    ExecuteEvents.Execute(lastGazedObject, pointerData, ExecuteEvents.pointerExitHandler);
                ExecuteEvents.Execute(currentGazed, pointerData, ExecuteEvents.pointerEnterHandler);
                lastGazedObject = currentGazed;
                gazeTimer = 0f;
                if (gazeProgressImage)
                {
                    gazeProgressImage.fillAmount = 0f;
                    gazeProgressImage.gameObject.SetActive(true);
                }
            }

            gazeTimer += Time.deltaTime;
            if (gazeProgressImage)
                gazeProgressImage.fillAmount = gazeTimer / gazeClickDelay;

            if (gazeTimer >= gazeClickDelay)
            {
                if (toggle)
                {
                    Debug.Log("Gaze click Toggle: " + toggle.name);
                    ExecuteEvents.Execute(toggle.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                }
                else if (button)
                {
                    Debug.Log("Gaze click Button: " + button.name);
                    button.onClick.Invoke();
                }
                else if (dropdown)
                {
                    Debug.Log("Gaze click Dropdown: " + dropdown.name);
                    ExecuteEvents.Execute(dropdown.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                }
                else if (postit)
                {
                    Debug.Log("Gaze click Postit: " + postit.name);
                    ExecuteEvents.Execute(postit.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                }
                else if (character)
                {
                    Debug.Log("Gaze click Character: " + character.name);
                    ExecuteEvents.Execute(character.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                }
                ResetGaze();
            }
        }
        else
        {
            if (lastGazedObject != null)
                ExecuteEvents.Execute(lastGazedObject, pointerData, ExecuteEvents.pointerExitHandler);
            ResetGaze();
        }
    }

    private void ResetGaze()
    {
        lastGazedObject = null;
        gazeTimer = 0f;
        if (gazeProgressImage)
        {
            gazeProgressImage.fillAmount = 0f;
            gazeProgressImage.gameObject.SetActive(false);
        }
    }
}