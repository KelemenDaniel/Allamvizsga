using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public Animator animator;
    bool opened = false;

    public void Open()
    {
        if (opened) return;
        opened = true;
        Debug.Log("Opening door");
        animator.SetTrigger("Open");
    }


}
