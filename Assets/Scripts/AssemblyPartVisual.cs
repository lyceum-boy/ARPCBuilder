using UnityEngine;

/// <summary>
/// Centers imported model geometry around the root pivot without rescaling it.
/// Put this component on MotherboardPart and assign its direct child ModelVisual.
/// Keep the original GLB nested under ModelVisual at its native scale.
/// </summary>
[DisallowMultipleComponent]
public class AssemblyPartVisual : MonoBehaviour
{
    [SerializeField] private Transform modelVisual;

    private void Awake()
    {
        if (modelVisual == null || modelVisual.parent != transform)
        {
            Debug.LogWarning($"{name}: assign direct child ModelVisual to AssemblyPartVisual.", this);
            return;
        }

        MeshFilter[] meshes = modelVisual.GetComponentsInChildren<MeshFilter>(true);
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        bool found = false;

        foreach (MeshFilter meshFilter in meshes)
        {
            if (meshFilter.sharedMesh == null) continue;

            Bounds bounds = meshFilter.sharedMesh.bounds;
            Matrix4x4 meshToPart = transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                Vector3 localPoint = meshToPart.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, localPoint);
                max = Vector3.Max(max, localPoint);
                found = true;
            }
        }

        if (found)
            modelVisual.localPosition -= (min + max) * 0.5f;
    }
}
