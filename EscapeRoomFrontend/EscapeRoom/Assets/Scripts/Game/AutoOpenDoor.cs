using UnityEngine;

public class AutoOpenDoor : MonoBehaviour
{
    public Animator doorAnimator;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorAnimator.SetBool("character_nearby", true);
            Debug.Log("Player entered: Door opening");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorAnimator.SetBool("character_nearby", false);
            Debug.Log("Player left: Door closing (if allowed)");
        }
    }
}
