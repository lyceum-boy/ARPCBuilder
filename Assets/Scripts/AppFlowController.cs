using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AppFlowController : MonoBehaviour
{
    public enum TrainingStage
    {
        MeasureSpace,
        SelectCase,
        PlaceCase,
        AssemblePC,
        Completed
    }

    [Header("Stage Panels")]
    [SerializeField] private GameObject measureSpacePanel;
    [SerializeField] private GameObject caseSelectionPanel;
    [SerializeField] private GameObject placementPanel;
    [SerializeField] private GameObject assemblyPanel;
    [SerializeField] private GameObject completionPanel;

    [Header("AR Controllers")]
    [SerializeField]
    private ARPlacementController placementController;
    [SerializeField]
    private SpaceMeasurementController measurementController;

    [Header("Measurement Navigation")]
    [SerializeField] private Button continueMeasurementButton;

    [Header("Case Selection UI")]
    [SerializeField] private TMP_Text caseSelectionSpaceText;

    // X = ширина, Y = высота, Z = глубина.
    public Vector3 MeasuredSpaceDimensions { get; private set; }

    public TrainingStage CurrentStage { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        ShowStage(TrainingStage.MeasureSpace);
    }

    private void Update()
    {
        if (continueMeasurementButton == null ||
            measurementController == null)
            return;

        bool canContinue =
            CurrentStage == TrainingStage.MeasureSpace &&
            measurementController.IsComplete;

        continueMeasurementButton.interactable = canContinue;
    }


    public void ContinueToCaseSelection()
    {
        if (CurrentStage != TrainingStage.MeasureSpace ||
            measurementController == null ||
            !measurementController.IsComplete)
        {
            Debug.LogWarning(
                "Для продолжения необходимо указать три точки."
            );
            return;
        }

        // Сохраняем размеры до переключения этапа.
        MeasuredSpaceDimensions = new Vector3(
            measurementController.Width,
            measurementController.Height,
            measurementController.Depth
        );

        if (caseSelectionSpaceText != null)
        {
            caseSelectionSpaceText.text =
                "Доступное пространство\n" +
                $"Ширина: {DimensionFormatter.Centimeters(MeasuredSpaceDimensions.x)}\n" +
                $"Глубина: {DimensionFormatter.Centimeters(MeasuredSpaceDimensions.z)}\n" +
                $"Высота: {DimensionFormatter.Centimeters(MeasuredSpaceDimensions.y)}";
        }

        Debug.Log(
            $"Available space: {MeasuredSpaceDimensions}"
        );

        ShowStage(TrainingStage.SelectCase);
    }

    public void BackToMeasurement()
    {
        ShowStage(TrainingStage.MeasureSpace);
    }

    public void ShowStage(TrainingStage stage)
    {
        CurrentStage = stage;

        measureSpacePanel.SetActive(
            stage == TrainingStage.MeasureSpace);

        caseSelectionPanel.SetActive(
            stage == TrainingStage.SelectCase);

        placementPanel.SetActive(
            stage == TrainingStage.PlaceCase);

        assemblyPanel.SetActive(
            stage == TrainingStage.AssemblePC);

        completionPanel.SetActive(
            stage == TrainingStage.Completed);

        if (placementController != null)
        {
            placementController.enabled =
                stage == TrainingStage.PlaceCase;
        }

        if (measurementController != null)
        {
            measurementController.enabled =
                stage == TrainingStage.MeasureSpace;
        }

        Debug.Log($"Training stage: {stage}");
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
