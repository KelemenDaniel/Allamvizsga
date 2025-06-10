using UnityEngine;
using UnityEngine.EventSystems;

public class GazeActivatePostit : MonoBehaviour, IPointerClickHandler
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        animator.SetTrigger("PlayPostit");
        Debug.Log("Postit animation triggered on: " + gameObject.name);
    }
}