
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CaseSelectionController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private AppFlowController flowController;
    [SerializeField] private ARPlacementController placementController;

    [Header("Standard Case")]
    [SerializeField] private Button standardCaseButton;
    [SerializeField] private TMP_Text standardDetailsText;
    [SerializeField] private TMP_Text standardFitStatusText;

    [Header("Mini-Tower")]
    [SerializeField] private Button miniTowerCaseButton;
    [SerializeField] private TMP_Text miniTowerDetailsText;
    [SerializeField] private TMP_Text miniTowerFitStatusText;

    [Header("Navigation")]
    [SerializeField] private TMP_Text overallFitStatusText;
    [SerializeField] private Button placeCaseButton;

    [Header("Standard Dimensions (mm)")]
    [SerializeField, Min(1)] private int widthMm = 242;
    [SerializeField, Min(1)] private int heightMm = 492;
    [SerializeField, Min(1)] private int depthMm = 476;

    [Header("Mini-Tower Dimensions (mm)")]
    [SerializeField, Min(1)] private int miniWidthMm = 165;
    [SerializeField, Min(1)] private int miniHeightMm = 350;
    [SerializeField, Min(1)] private int miniDepthMm = 350;

    private bool standardFits;
    private bool miniFits;
    private bool standardSelected;
    private float initialYawDegrees;

    private static bool CheckFit(
        Vector3 space,
        int widthMm,
        int heightMm,
        int depthMm,
        out bool rotated)
    {
        float width = widthMm / 1000f;
        float height = heightMm / 1000f;
        float depth = depthMm / 1000f;

        bool heightFits = space.y >= height;

        bool normal =
            heightFits &&
            space.x >= width &&
            space.z >= depth;

        bool sideways =
            heightFits &&
            space.x >= depth &&
            space.z >= width;

        rotated = !normal && sideways;

        return normal || sideways;
    }

    private static string FitMessage(bool fits, bool rotated)
    {
        if (!fits)
            return "Не помещается";

        return rotated
            ? "Подходит по габаритам"
            : "Подходит по габаритам";
    }

    public void Evaluate(Vector3 space)
    {
        standardSelected = false;

        standardFits = CheckFit(
            space,
            widthMm,
            heightMm,
            depthMm,
            out bool standardRotated
        );

        miniFits = CheckFit(
            space,
            miniWidthMm,
            miniHeightMm,
            miniDepthMm,
            out bool miniRotated
        );

        initialYawDegrees = standardRotated ? 90f : 0f;

        if (standardDetailsText != null)
        {
            standardDetailsText.text =
                "PCCaseStandard\n" +
                $"{widthMm} × {heightMm} × {depthMm} мм";
        }

        if (miniTowerDetailsText != null)
        {
            miniTowerDetailsText.text =
                "Mini-Tower (в разработке)\n" +
                $"{miniWidthMm} × {miniHeightMm} × " +
                $"{miniDepthMm} мм";
        }

        if (standardFitStatusText != null)
        {
            standardFitStatusText.text =
                FitMessage(standardFits, standardRotated);

            standardFitStatusText.color = standardFits
                ? Color.green
                : new Color(1f, 0.5f, 0.5f);
        }

        if (miniTowerFitStatusText != null)
        {
            miniTowerFitStatusText.text =
                FitMessage(miniFits, miniRotated) +
                "\nВыбор пока недоступен";

            miniTowerFitStatusText.color = miniFits
                ? new Color(0.6f, 0.85f, 0.65f)
                : new Color(1f, 0.5f, 0.5f);
        }

        if (standardCaseButton != null)
            standardCaseButton.interactable = standardFits;

        // Временно отключён независимо от размеров.
        if (miniTowerCaseButton != null)
            miniTowerCaseButton.interactable = false;

        if (placeCaseButton != null)
            placeCaseButton.interactable = false;

        if (overallFitStatusText != null)
        {
            if (!standardFits && !miniFits)
            {
                overallFitStatusText.text =
                    "Ни один корпус не помещается.\n" +
                    "Измерь другую область.";
            }
            else if (!standardFits && miniFits)
            {
                overallFitStatusText.text =
                    "Подходит только Mini-Tower,\n" +
                    "но он пока недоступен.";
            }
            else
            {
                overallFitStatusText.text =
                    "Выбери стандартный корпус.";
            }
        }
    }

    public void SelectStandardCase()
    {
        if (!standardFits)
            return;

        standardSelected = true;

        if (placeCaseButton != null)
            placeCaseButton.interactable = true;

        if (overallFitStatusText != null)
            overallFitStatusText.text =
                "PCCaseStandard выбран.";
    }

    public void ConfirmSelection()
    {
        if (!standardSelected ||
            !standardFits ||
            flowController == null ||
            placementController == null)
        {
            return;
        }

        placementController.SetInitialYaw(
            initialYawDegrees
        );

        flowController.ShowStage(
            AppFlowController.TrainingStage.PlaceCase
        );
    }
}
