
using UnityEngine;

public class CaseBoundsDebugger : MonoBehaviour
{
    [ContextMenu("Print Case Bounds")]
    public void PrintCaseBounds()
    {
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            Debug.LogWarning("No active renderers found.");
            return;
        }

        Vector3 size = combinedBounds.size;
        Vector3 localMin = transform.InverseTransformPoint(
            combinedBounds.min
        );
        Vector3 localMax = transform.InverseTransformPoint(
            combinedBounds.max
        );

        Debug.Log(
            $"Case dimensions:\n" +
            $"Width (X): {size.x * 1000f:F1} mm\n" +
            $"Height (Y): {size.y * 1000f:F1} mm\n" +
            $"Depth (Z): {size.z * 1000f:F1} mm\n" +
            $"Local minimum: {localMin}\n" +
            $"Local maximum: {localMax}"
        );
    }
}
