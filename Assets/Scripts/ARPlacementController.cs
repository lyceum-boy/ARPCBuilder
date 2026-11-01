using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(ARRaycastManager))]
public class ARPlacementController : MonoBehaviour
{
    [SerializeField] private GameObject casePrefab;

    private ARRaycastManager raycastManager;
    private GameObject placedCase;

    private readonly List<ARRaycastHit> hits = new();

    public bool IsPlaced => placedCase != null;

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    // Update is called once per frame
    private void Update()
    {
        if (casePrefab == null || Touch.activeTouches.Count != 1)
            return;

        Touch touch = Touch.activeTouches[0];

        if (!touch.began)
            return;

        if (!raycastManager.Raycast(
            touch.screenPosition,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            return;
        }

        Pose hitPose = hits[0].pose;

        if (placedCase == null)
        {
            placedCase = Instantiate(
                casePrefab,
                hitPose.position,
                Quaternion.identity
            );
        }
        else
        {
            placedCase.transform.position = hitPose.position;
        }
    }
}
