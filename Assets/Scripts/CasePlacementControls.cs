
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class CasePlacementControls : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private AppFlowController flowController;
    [SerializeField] private ARPlacementController placementController;
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Interface")]
    [SerializeField] private TMP_Text placementStatusText;
    [SerializeField] private Button rotateLeftButton;
    [SerializeField] private Button rotateRightButton;
    [SerializeField] private Button repositionCaseButton;
    [SerializeField] private Button confirmPlacementButton;

    private readonly List<ARRaycastHit> hits = new();
    private readonly List<RaycastResult> uiHits = new();

    private bool isRepositioning;

    private bool IsPlacementStage =>
        flowController != null &&
        flowController.CurrentStage ==
            AppFlowController.TrainingStage.PlaceCase;

    private GameObject CurrentCase =>
        placementController != null
            ? placementController.PlacedCase
            : null;

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
        if (!IsPlacementStage)
        {
            isRepositioning = false;
            return;
        }

        GameObject pcCase = CurrentCase;
        bool hasCase = pcCase != null;

        if (!hasCase)
            isRepositioning = false;

        SetButtonState(rotateLeftButton, hasCase && !isRepositioning);
        SetButtonState(rotateRightButton, hasCase && !isRepositioning);
        SetButtonState(repositionCaseButton, hasCase && !isRepositioning);
        SetButtonState(confirmPlacementButton, hasCase && !isRepositioning);

        if (!hasCase)
        {
            SetStatus(
                "Коснись обнаруженной поверхности,\n" +
                "чтобы разместить корпус."
            );
            return;
        }

        if (!isRepositioning)
        {
            SetStatus(
                "Корпус размещён.\n" +
                "Можешь повернуть или переместить его."
            );
            return;
        }

        SetStatus(
            "Коснись новой позиции на поверхности."
        );

        if (!TryGetPointerPosition(out Vector2 screenPosition))
            return;

        if (IsPointerOverUI(screenPosition))
            return;

        if (raycastManager == null ||
            !raycastManager.Raycast(
                screenPosition,
                hits,
                TrackableType.PlaneWithinPolygon
            ))
        {
            SetStatus("Плоскость не обнаружена. Попробуй ещё раз.");
            return;
        }

        // Меняем только позицию, сохраняя ориентацию.
        pcCase.transform.position = hits[0].pose.position;

        isRepositioning = false;

        Debug.Log("PC case repositioned.");
    }

    private static void SetButtonState(Button button, bool active)
    {
        if (button != null && button.interactable != active)
            button.interactable = active;
    }

    private void SetStatus(string message)
    {
        if (placementStatusText != null &&
            placementStatusText.text != message)
        {
            placementStatusText.text = message;
        }
    }

    private static bool TryGetPointerPosition(out Vector2 position)
    {
        position = default;

#if UNITY_EDITOR
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return false;
        }

        position = Mouse.current.position.ReadValue();
#else
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

        foreach (RaycastResult result in uiHits)
        {
            if (result.gameObject != null &&
                result.gameObject.GetComponentInParent<Canvas>() != null)
            {
                return true;
            }
        }

        return false;
    }

    public void RotateLeft()
    {
        if (!IsPlacementStage ||
            isRepositioning ||
            CurrentCase == null)
            return;

        CurrentCase.transform.Rotate(
            0f, -90f, 0f, Space.World
        );
    }

    public void RotateRight()
    {
        if (!IsPlacementStage ||
            isRepositioning ||
            CurrentCase == null)
            return;

        CurrentCase.transform.Rotate(
            0f, 90f, 0f, Space.World
        );
    }

    public void BeginReposition()
    {
        if (!IsPlacementStage || CurrentCase == null)
            return;

        isRepositioning = true;
    }

    public void ConfirmPlacement()
    {
        if (!IsPlacementStage ||
            isRepositioning ||
            CurrentCase == null ||
            flowController == null)
        {
            return;
        }

        flowController.ShowStage(
            AppFlowController.TrainingStage.AssemblePC
        );
    }
}
