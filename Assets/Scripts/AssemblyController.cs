using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssemblyController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private AppFlowController flowController;
    [SerializeField] private ARPlacementController placementController;
    [SerializeField] private Camera arCamera;

    [Header("Interface")]
    [SerializeField] private TMP_Text assemblyHintText;
    [SerializeField] private TMP_Text assemblyStepText;
    [SerializeField] private TMP_Text assemblyInfoText;
    [SerializeField] private TMP_Text assemblyProgressText;
    [SerializeField] private TMP_Text dragBoxLabelText;
    [SerializeField] private Button confirmComponentButton;

    [Header("3D Preview")]
    [SerializeField] private RectTransform componentDragBox;
    [SerializeField, Range(0.3f, 1.5f)] private float previewDistanceMeters = 0.75f;
    [SerializeField, Range(0.4f, 0.95f)] private float previewBoxFill = 0.72f;
    [SerializeField] private Vector3 previewEulerAngles = new Vector3(0f, 90f, 0f);

    [Header("Installation Hint")]
    [SerializeField] private Material targetGhostMaterial;

    private AssemblySlot[] slots = Array.Empty<AssemblySlot>();
    private readonly List<GameObject> installedParts = new();
    private int currentIndex;
    private GameObject previewPart;
    private GameObject draggingPart;
    private GameObject pendingPart;
    private GameObject targetGhost;
    private bool nearTarget;
    // Offset between the component root and the center of its visible geometry.
    // Imported GLB pivots are often not at the center of a motherboard.
    private Vector3 dragVisualOffset;
    // Remember the target center while its translucent ghost is temporarily hidden.
    private Vector3 dragTargetCenter;

    private bool IsAssemblyStage =>
        flowController != null &&
        flowController.CurrentStage == AppFlowController.TrainingStage.AssemblePC;

    private bool CanDrag =>
        IsAssemblyStage &&
        placementController != null && placementController.PlacedCase != null &&
        arCamera != null && currentIndex < slots.Length &&
        slots[currentIndex].ComponentPrefab != null;

    private AssemblySlot CurrentSlot => slots[currentIndex];

    private void OnEnable() => InitializeAssembly();

    private void OnDisable()
    {
        DestroyObject(ref previewPart);
        DestroyObject(ref draggingPart);
        DestroyObject(ref pendingPart);
        DestroyObject(ref targetGhost);
        nearTarget = false;
    }

    private static void DestroyObject(ref GameObject obj)
    {
        if (obj != null) Destroy(obj);
        obj = null;
    }

    private void InitializeAssembly()
    {
        slots = Array.Empty<AssemblySlot>();
        currentIndex = 0;
        nearTarget = false;
        installedParts.Clear();
        DestroyObject(ref previewPart);
        DestroyObject(ref draggingPart);
        DestroyObject(ref pendingPart);
        DestroyObject(ref targetGhost);

        if (placementController == null || placementController.PlacedCase == null)
        {
            ShowStatus("Сначала размести корпус.");
            SetConfirmEnabled(false);
            return;
        }

        slots = placementController.PlacedCase.GetComponentsInChildren<AssemblySlot>(true);
        Array.Sort(slots, (a, b) => a.StepOrder.CompareTo(b.StepOrder));

        // Legacy cube markers must never appear: target hint is now a ghost of the actual prefab.
        foreach (AssemblySlot slot in slots) slot.ShowTarget(false);

        if (slots.Length == 0)
        {
            ShowStatus("Точки установки не найдены.");
            SetConfirmEnabled(false);
            return;
        }

        CreateTargetGhost();
        CreatePreview();
        RefreshUI();
    }

    private GameObject CreatePartAtSlot()
    {
        if (!CanDrag) return null;
        GameObject part = Instantiate(CurrentSlot.ComponentPrefab, CurrentSlot.transform);
        part.transform.localPosition = Vector3.zero;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = Vector3.one;
        return part;
    }

    private void CreateTargetGhost()
    {
        DestroyObject(ref targetGhost);
        if (!CanDrag) return;

        targetGhost = CreatePartAtSlot();
        targetGhost.name = CurrentSlot.ComponentName + "_TargetGhost";
        if (targetGhostMaterial != null)
            OverrideAllMaterials(targetGhost, targetGhostMaterial);
    }

    private static void OverrideAllMaterials(GameObject part, Material material)
    {
        foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private void CreatePreview()
    {
        DestroyObject(ref previewPart);
        if (!CanDrag || componentDragBox == null || pendingPart != null) return;

        previewPart = Instantiate(CurrentSlot.ComponentPrefab, arCamera.transform);
        previewPart.name = CurrentSlot.ComponentName + "_Preview";
        previewPart.transform.localScale = Vector3.one;
        previewPart.transform.localRotation = Quaternion.Euler(previewEulerAngles);

        Vector2 cardCenter = RectTransformUtility.WorldToScreenPoint(null, componentDragBox.position);
        previewPart.transform.position = arCamera.ScreenToWorldPoint(
            new Vector3(cardCenter.x, cardCenter.y, previewDistanceMeters));

        FitPreviewToBox();

        // Center the rendered mesh over the card. The imported model pivot
        // may not coincide with the middle of the visible motherboard.
        if (TryGetVisibleBounds(previewPart, out Bounds previewBounds))
        {
            Vector3 desiredCenter = arCamera.ScreenToWorldPoint(
                new Vector3(cardCenter.x, cardCenter.y, previewDistanceMeters));
            previewPart.transform.position += desiredCenter - previewBounds.center;
        }
    }

    private void FitPreviewToBox()
    {
        if (previewPart == null || componentDragBox == null || arCamera == null) return;

        Vector3[] cardCorners = new Vector3[4];
        componentDragBox.GetWorldCorners(cardCorners);
        Vector2 cardMin = RectTransformUtility.WorldToScreenPoint(null, cardCorners[0]);
        Vector2 cardMax = RectTransformUtility.WorldToScreenPoint(null, cardCorners[2]);
        float cardWidth = Mathf.Abs(cardMax.x - cardMin.x);
        float cardHeight = Mathf.Abs(cardMax.y - cardMin.y);
        if (cardWidth < 1f || cardHeight < 1f) return;

        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        bool found = false;

        foreach (Renderer renderer in previewPart.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            Bounds bounds = renderer.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                Vector3 screen = arCamera.WorldToScreenPoint(corner);
                if (screen.z <= 0f) continue;
                minX = Mathf.Min(minX, screen.x);
                maxX = Mathf.Max(maxX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxY = Mathf.Max(maxY, screen.y);
                found = true;
            }
        }

        if (!found) return;
        float currentWidth = Mathf.Max(1f, maxX - minX);
        float currentHeight = Mathf.Max(1f, maxY - minY);
        float ratio = Mathf.Min(cardWidth / currentWidth, cardHeight / currentHeight);
        previewPart.transform.localScale = Vector3.one * Mathf.Clamp(ratio * previewBoxFill, 0.01f, 3f);
    }

    // The slot pivot can be elsewhere than the visual center of an imported GLB.
    // Calculate the visible bounds in world coordinates instead of assuming the
    // pivot is inside the middle of the visible mesh.
    private static bool TryGetVisibleBounds(GameObject part, out Bounds result)
    {
        result = default;
        if (part == null) return false;

        bool found = false;
        foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!found)
            {
                result = renderer.bounds;
                found = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }

    private Vector3 GetTargetVisualCenter()
    {
        return TryGetVisibleBounds(targetGhost, out Bounds bounds)
            ? bounds.center
            : CurrentSlot.transform.position;
    }

    public void BeginComponentDrag(Vector2 screenPosition)
    {
        if (!CanDrag) return;

        if (pendingPart != null)
        {
            DestroyObject(ref pendingPart);
            CreateTargetGhost();
        }

        DestroyObject(ref draggingPart);
        if (previewPart != null)
        {
            draggingPart = previewPart;
            previewPart = null;
            draggingPart.transform.SetParent(placementController.PlacedCase.transform, true);
        }
        else
        {
            draggingPart = Instantiate(CurrentSlot.ComponentPrefab, placementController.PlacedCase.transform);
        }

        draggingPart.name = CurrentSlot.ComponentName + "_Dragging";
        draggingPart.transform.localScale = Vector3.one;
        draggingPart.transform.position = CurrentSlot.transform.position;
        draggingPart.transform.rotation = CurrentSlot.transform.rotation;

        // Preserve the alignment between the visual mesh and the slot while
        // dragging, even if the original GLB pivot is off-center.
        dragVisualOffset = TryGetVisibleBounds(draggingPart, out Bounds dragBounds)
            ? dragBounds.center - draggingPart.transform.position
            : Vector3.zero;

        dragTargetCenter = GetTargetVisualCenter();
        if (targetGhost != null) targetGhost.SetActive(true);

        nearTarget = false;
        UpdateComponentDrag(screenPosition);
        RefreshUI("Перемести деталь в подсвеченную область.");
    }

    public void UpdateComponentDrag(Vector2 screenPosition)
    {
        if (!CanDrag || draggingPart == null) return;

        Vector3 targetCenter = dragTargetCenter;
        Plane dragPlane = new Plane(arCamera.transform.forward, targetCenter);
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (!dragPlane.Raycast(ray, out float distance))
        {
            nearTarget = false;
            if (targetGhost != null) targetGhost.SetActive(true);
            return;
        }

        // Make the visible board follow the pointer, not its imported pivot.
        draggingPart.transform.position = ray.GetPoint(distance) - dragVisualOffset;

        // Visual hit test: compare the finger with the projected CENTER of the
        // target mesh. This is easier to use on a phone than an 8 cm 3D sphere
        // around a pivot that may be outside the geometry.
        Vector3 targetScreen = arCamera.WorldToScreenPoint(targetCenter);
        // Narrower radius: ~28 px at 932 px height, ~84 px on a 3x phone screen.
        float screenTolerance = Mathf.Max(20f, Screen.height * 0.03f);
        bool nearOnScreen = targetScreen.z > 0f &&
            Vector2.Distance(screenPosition,
                new Vector2(targetScreen.x, targetScreen.y)) <= screenTolerance;

        // Keep the original configurable world-space tolerance as a fallback.
        bool nearInWorld = Vector3.Distance(
            draggingPart.transform.position,
            CurrentSlot.transform.position) <= CurrentSlot.SnapDistance;

        bool nowNear = nearOnScreen || nearInWorld;

        // The blue ghost used to overlap the real board and tint it blue.
        // Hide the ghost near the target; keep the board's original materials.
        if (targetGhost != null) targetGhost.SetActive(!nowNear);

        // Show live snapping feedback while the pointer remains near the slot.
        if (nowNear)
            draggingPart.transform.position = CurrentSlot.transform.position;
        if (nowNear != nearTarget)
        {
            nearTarget = nowNear;
            RefreshUI(nearTarget
                ? "Правильное положение! Отпусти деталь."
                : "Перемести деталь в подсвеченную область.");
        }
    }

    public void EndComponentDrag(Vector2 screenPosition)
    {
        if (draggingPart == null) return;
        UpdateComponentDrag(screenPosition);

        if (nearTarget)
        {
            pendingPart = draggingPart;
            pendingPart.transform.SetParent(CurrentSlot.transform, false);
            pendingPart.transform.localPosition = Vector3.zero;
            pendingPart.transform.localRotation = Quaternion.identity;
            pendingPart.transform.localScale = Vector3.one;
            DestroyObject(ref targetGhost);
            Debug.Log("Component snapped to slot.");
        }
        else
        {
            DestroyObject(ref draggingPart);
            Debug.Log("Component missed slot.");
        }

        draggingPart = null;
        nearTarget = false;
        if (pendingPart == null) CreatePreview();
        RefreshUI(pendingPart != null
            ? "Деталь размещена. Подтверди установку."
            : "Не попал в место установки. Попробуй ещё раз.");
    }

    public void ConfirmComponent()
    {
        if (!IsAssemblyStage || pendingPart == null) return;

        installedParts.Add(pendingPart);
        pendingPart = null;
        currentIndex++;

        if (currentIndex >= slots.Length)
        {
            Debug.Log("PC assembly completed.");
            flowController.ShowStage(AppFlowController.TrainingStage.Completed);
            return;
        }

        CreateTargetGhost();
        CreatePreview();
        RefreshUI();
    }

    public void BackToPlacement()
    {
        if (!IsAssemblyStage) return;

        DestroyObject(ref previewPart);
        DestroyObject(ref draggingPart);
        DestroyObject(ref pendingPart);
        DestroyObject(ref targetGhost);
        foreach (GameObject part in installedParts)
            if (part != null) Destroy(part);
        installedParts.Clear();
        foreach (AssemblySlot slot in slots)
            if (slot != null) slot.ShowTarget(false);

        flowController.ShowStage(AppFlowController.TrainingStage.PlaceCase);
    }

    private void RefreshUI(string overrideHint = null)
    {
        if (slots.Length == 0 || currentIndex >= slots.Length) return;
        AssemblySlot slot = CurrentSlot;

        ShowStatus(overrideHint ?? (pendingPart != null
            ? "Подтверди установку компонента."
            : $"Перетащи компонент: {slot.ComponentName}"));

        if (assemblyStepText != null)
            assemblyStepText.text = $"Этап {currentIndex + 1} из {slots.Length}";
        if (assemblyInfoText != null)
            assemblyInfoText.text = $"<b>{slot.ComponentName}</b>\n{slot.Description}";
        if (assemblyProgressText != null)
            assemblyProgressText.text = $"Установлено компонентов: {currentIndex} из {slots.Length}";
        if (dragBoxLabelText != null) dragBoxLabelText.text = "";
        SetConfirmEnabled(pendingPart != null);
    }

    private void ShowStatus(string message)
    {
        if (assemblyHintText != null) assemblyHintText.text = message;
    }

    private void SetConfirmEnabled(bool enabled)
    {
        if (confirmComponentButton != null)
            confirmComponentButton.interactable = enabled;
    }
}
