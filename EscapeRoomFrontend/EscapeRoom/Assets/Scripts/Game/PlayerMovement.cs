using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float maxDistance;
    [SerializeField] LayerMask groundLayerMask;
    [SerializeField] Transform playerRoot;
    [SerializeField] float smoothTime;
    [SerializeField] Transform feetIcon;
    [SerializeField] float feetIconRotationSpeed;

    [Header("Gaze Auto-Walk Settings")]
    [SerializeField] float gazeWalkDelay = 2.0f;
    [SerializeField] Image gazeWalkProgressImage;

    Vector3 currentVelocity;
    Vector3 targetPosition;
    Transform feetIconRotationPlaceholder;

    private float gazeTimer = 0f;
    private bool isGazingAtGround = false;
    private Vector3 lastGazeHitPoint;

    void Start()
    {
        targetPosition = playerRoot.position;
        feetIconRotationPlaceholder = new GameObject("FeetIconRotationPlaceholder").transform;
    }

    void Update()
    {
        HandleGroundDetection();
        HandleGazeWalking();
        HandleManualInput();

        playerRoot.position = Vector3.SmoothDamp(playerRoot.position,
                                               targetPosition,
                                               ref currentVelocity,
                                               smoothTime);
    }

    void HandleGroundDetection()
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

                lastGazeHitPoint = hit.point;
                isGazingAtGround = true;
            }
            else
            {
                feetIcon.gameObject.GetComponent<Animator>().SetTrigger("Disable");
                isGazingAtGround = false;
            }
        }
        else
        {
            feetIcon.gameObject.GetComponent<Animator>().SetTrigger("Disable");
            isGazingAtGround = false;
        }
    }

    void HandleGazeWalking()
    {

        if (isGazingAtGround)
        {
            gazeTimer += Time.deltaTime;

            if (gazeWalkProgressImage)
            {
                gazeWalkProgressImage.fillAmount = gazeTimer / gazeWalkDelay;
                gazeWalkProgressImage.gameObject.SetActive(true);
            }

            if (gazeTimer >= gazeWalkDelay)
            {
                Debug.Log("Gaze auto-walk triggered!");
                TryMoveToPosition(lastGazeHitPoint);
                ResetGazeTimer();
            }
        }
        else
        {
            ResetGazeTimer();
        }
    }

    void HandleManualInput()
    {
        if (Input.anyKeyDown && isGazingAtGround)
        {
            TryMoveToPosition(lastGazeHitPoint);
        }
    }

    void TryMoveToPosition(Vector3 hitPoint)
    {
        Vector3 destination = new Vector3(hitPoint.x, playerRoot.position.y, hitPoint.z);
        Vector3 direction = destination - playerRoot.position;
        float distance = direction.magnitude;

        float capsuleRadius = 0.5f;
        float capsuleHeight = 3f;
        float halfHeight = (capsuleHeight / 2f) - capsuleRadius;

        Vector3 point1 = playerRoot.position + Vector3.up * capsuleRadius;
        Vector3 point2 = playerRoot.position + Vector3.up * (capsuleHeight - capsuleRadius);

        int obstacleLayers = LayerMask.GetMask("Walls");

        if (!Physics.CapsuleCast(point1, point2, capsuleRadius, direction.normalized, distance, obstacleLayers))
        {
            targetPosition = destination;
        }
        else
        {
            Debug.Log("Blocked by obstacle. Movement canceled.");
        }
    }

    void ResetGazeTimer()
    {
        gazeTimer = 0f;
        if (gazeWalkProgressImage)
        {
            gazeWalkProgressImage.fillAmount = 0f;
            gazeWalkProgressImage.gameObject.SetActive(false);
        }
    }
}