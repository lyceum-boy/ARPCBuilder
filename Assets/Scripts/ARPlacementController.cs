
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(ARRaycastManager))]
public class ARPlacementController : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject casePrefab;

    private ARRaycastManager raycastManager;
    private GameObject placedCase;

    private float initialYawDegrees;

    private readonly List<ARRaycastHit> hits = new();
    private readonly List<RaycastResult> uiHits = new();

    public GameObject PlacedCase => placedCase;
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

    private void Update()
    {
        // Первоначальное размещение выполняется один раз.
        // Повторное перемещение обрабатывает CasePlacementControls.
        if (casePrefab == null ||
            placedCase != null ||
            raycastManager == null)
        {
            return;
        }

        if (!TryGetPointerPosition(out Vector2 screenPosition))
            return;

        // Нельзя размещать корпус нажатием на UI.
        if (IsPointerOverUI(screenPosition))
            return;

        if (!raycastManager.Raycast(
                screenPosition,
                hits,
                TrackableType.PlaneWithinPolygon))
        {
            return;
        }

        Pose hitPose = hits[0].pose;

        placedCase = Instantiate(
            casePrefab,
            hitPose.position,
            Quaternion.Euler(0f, initialYawDegrees, 0f)
        );

        Debug.Log("PC case placed.");
    }

    private static bool TryGetPointerPosition(out Vector2 position)
    {
        position = default;

#if UNITY_EDITOR
        // XR Simulation: ввод обычной мышью.
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return false;
        }

        position = Mouse.current.position.ReadValue();

#else
        // iOS: сенсорный ввод.
        if (Touch.activeTouches.Count != 1)
            return false;

        Touch touch = Touch.activeTouches[0];

        if (!touch.began)
            return false;

        position = touch.screenPosition;
#endif

        return true;
    }

    private bool IsPointerOverUI(Vector2 position)
    {
        if (EventSystem.current == null)
            return false;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = position
        };

        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);

        return uiHits.Count > 0;
    }

    public void SetInitialYaw(float degrees)
    {
        initialYawDegrees = degrees;

        if (placedCase != null)
        {
            placedCase.transform.rotation =
                Quaternion.Euler(
                    0f,
                    initialYawDegrees,
                    0f
                );
        }
    }

    public void ClearPlacedCase()
    {
        if (placedCase == null)
            return;

        Destroy(placedCase);
        placedCase = null;

        Debug.Log("PC case cleared.");
    }
}
