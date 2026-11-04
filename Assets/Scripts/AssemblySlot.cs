using UnityEngine;

public class AssemblySlot : MonoBehaviour
{
    [Header("Component")]
    [SerializeField, Min(0)] private int stepOrder;
    [SerializeField] private string componentName;
    [TextArea(2, 4)]
    [SerializeField]
    private string description;

    public string Description => description;

    [TextArea(2, 4)]
    [SerializeField] private string instruction;

    [Header("Installation")]
    [SerializeField] private GameObject componentPrefab;
    [SerializeField] private GameObject targetMarker;

    [SerializeField, Min(0.02f)]
    private float snapDistanceMeters = 0.08f;

    public int StepOrder => stepOrder;
    public string ComponentName => componentName;
    public string Instruction => instruction;
    public GameObject ComponentPrefab => componentPrefab;
    public float SnapDistance => snapDistanceMeters;

    public void ShowTarget(bool visible)
    {
        if (targetMarker != null)
            targetMarker.SetActive(visible);
    }
}
