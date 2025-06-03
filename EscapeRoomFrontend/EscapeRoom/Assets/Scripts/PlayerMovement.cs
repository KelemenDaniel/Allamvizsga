using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float maxDistance;
    [SerializeField] LayerMask groundLayerMask;
    [SerializeField] Transform playerRoot;
    [SerializeField] float smoothTime;
    [SerializeField] Transform feetIcon;
    [SerializeField] float feetIconRotationSpeed;
    Vector3 currentVelocity;
    Vector3 targetPosition;
    Transform feetIconRotationPlaceholder;

    void Start()
    {
        StartCoroutine(LoadVR());
        targetPosition = playerRoot.position;
        feetIconRotationPlaceholder = new GameObject("FeetIconRotationPlaceholder").transform;
    }


    IEnumerator LoadVR()
    {
        XRSettings.LoadDeviceByName("OpenVR");
        yield return null;
        XRSettings.enabled = true;
    }

    void Update()
    {
        if (Physics.Raycast(transform.position, transform.forward, maxDistance, groundLayerMask))
        {
            RaycastHit hit;
            Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, groundLayerMask);

            if (hit.collider.gameObject.layer == 8)
            {
                feetIcon.gameObject.GetComponent<Animator>().SetTrigger("Enable");
                feetIcon.position = hit.point;
                feetIconRotationPlaceholder.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                feetIcon.rotation = Quaternion.Slerp(feetIcon.rotation, feetIconRotationPlaceholder.rotation, feetIconRotationSpeed * Time.deltaTime);

                if (Input.anyKeyDown)
                {
                    Vector3 destination = new Vector3(hit.point.x, playerRoot.position.y, hit.point.z);
                    Vector3 direction = destination - playerRoot.position;
                    float distance = direction.magnitude;

                    // Capsule collider dimensions
                    float capsuleRadius = 0.5f;
                    float capsuleHeight = 3f;
                    float halfHeight = (capsuleHeight / 2f) - capsuleRadius;

                    // Define the capsule’s two endpoints
                    Vector3 point1 = playerRoot.position + Vector3.up * capsuleRadius;
                    Vector3 point2 = playerRoot.position + Vector3.up * (capsuleHeight - capsuleRadius);

                    // Do the cast against wall layers (not ground only)
                    int obstacleLayers = LayerMask.GetMask("Walls"); // include walls

                    if (!Physics.CapsuleCast(point1, point2, capsuleRadius, direction.normalized, distance, obstacleLayers))
                    {
                        targetPosition = destination;
                    }
                    else
                    {
                        Debug.Log("Blocked by obstacle. Movement canceled.");
                    }
                }


            }
            else
            {
                feetIcon.gameObject.GetComponent<Animator>().SetTrigger("Disable");
            }

        }
        else
        {
            feetIcon.gameObject.GetComponent<Animator>().SetTrigger("Disable");
        }

        playerRoot.position = Vector3.SmoothDamp(playerRoot.position,
                                               targetPosition,
                                               ref currentVelocity,
                                               smoothTime);
    }
}
