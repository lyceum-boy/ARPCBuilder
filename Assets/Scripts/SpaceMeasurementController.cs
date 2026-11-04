
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(ARRaycastManager))]
public class SpaceMeasurementController : MonoBehaviour
{
    [Header("AR")]
    [SerializeField]
    private ARRaycastManager raycastManager;

    [Header("UI")]
    [SerializeField]
    private TMP_Text measurementStatusText;

    [SerializeField]
    private TMP_Text heightValueText;

    [Header("Measurement")]
    [SerializeField, Min(0.1f)]
    private float availableHeightMeters = 0.6f;

    [Header("Visualization")]
    [SerializeField]
    private Material measurementMaterial;
    [SerializeField]
    private Material markerMaterial;

    [SerializeField, Min(0.001f)]
    private float lineWidth = 0.008f;

    private readonly List<ARRaycastHit> hits = new();
    private readonly List<RaycastResult> uiHits = new();

    private readonly Vector3[] points = new Vector3[3];
    private readonly List<GameObject> markers = new();
    private readonly List<LineRenderer> edges = new();

    private GameObject outlineRoot;
    private int pointCount;
    private float signedDepth;

    public float Width { get; private set; }
    public float Depth { get; private set; }
    public float Height => availableHeightMeters;

    public bool IsComplete => pointCount == 3;

    private void Awake()
    {
        if (raycastManager == null)
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

    private void Start()
    {
        ResetMeasurement();
    }

    private void Update()
    {
        if (IsComplete || raycastManager == null)
            return;

        Vector2 screenPosition;

#if UNITY_EDITOR
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        screenPosition = Mouse.current.position.ReadValue();
#else
        if (Touch.activeTouches.Count != 1)
            return;

        Touch touch = Touch.activeTouches[0];

        if (!touch.began)
            return;

        screenPosition = touch.screenPosition;
#endif

        if (IsPointerOverUI(screenPosition))
            return;

        if (!raycastManager.Raycast(
            screenPosition,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            SetStatus("Плоскость не обнаружена.");
            return;
        }

        Vector3 position = hits[0].pose.position;

        // Все точки располагаются на одной поверхности.
        if (pointCount > 0 &&
            Mathf.Abs(position.y - points[0].y) > 0.08f)
        {
            SetStatus("Выбери точки на одной поверхности.");
            return;
        }

        // Проверяем расстояние между первыми точками.
        if (pointCount == 1 &&
            Vector3.Distance(position, points[0]) < 0.05f)
        {
            SetStatus("Укажи вторую точку дальше от первой.");
            return;
        }

        // Третья точка должна задавать ненулевую глубину.
        if (pointCount == 2)
        {
            Vector3 widthDirection = points[1] - points[0];
            widthDirection.y = 0f;
            widthDirection.Normalize();

            Vector3 depthDirection =
                Vector3.Cross(Vector3.up, widthDirection);

            float proposedDepth = Mathf.Abs(Vector3.Dot(
                position - points[0],
                depthDirection
            ));

            if (proposedDepth < 0.05f)
            {
                SetStatus("Укажи третью точку в стороне от линии ширины.");
                return;
            }
        }

        points[pointCount] = position;
        CreateMarker(position, pointCount + 1);
        pointCount++;

        if (IsComplete)
            CalculateDimensions();

        UpdateStatus();
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

    private void CalculateDimensions()
    {
        Vector3 widthVector = points[1] - points[0];
        widthVector.y = 0f;

        Width = widthVector.magnitude;

        Vector3 depthDirection = Vector3.Cross(
            Vector3.up,
            widthVector.normalized
        );

        signedDepth = Vector3.Dot(
            points[2] - points[0],
            depthDirection
        );

        Depth = Mathf.Abs(signedDepth);

        CreateOutline();
    }

    private void CreateMarker(Vector3 position, int number)
    {
        GameObject marker =
            GameObject.CreatePrimitive(PrimitiveType.Sphere);

        marker.name = $"MeasurementPoint_{number}";

        marker.transform.position =
            position + Vector3.up * 0.025f;

        marker.transform.localScale =
            Vector3.one * 0.05f;

        if (measurementMaterial != null)
        {
            marker.GetComponent<Renderer>().sharedMaterial =
                measurementMaterial;
        }

        if (markerMaterial != null)
        {
            marker.GetComponent<Renderer>().sharedMaterial =
                markerMaterial;
        }

        Destroy(marker.GetComponent<Collider>());
        markers.Add(marker);
    }

    private void CreateOutline()
    {
        if (measurementMaterial == null)
        {
            Debug.LogWarning(
                "Не назначен Measurement Material."
            );
            return;
        }

        outlineRoot = new GameObject("MeasuredVolume");

        // 4 нижних, 4 верхних и 4 вертикальных ребра.
        for (int i = 0; i < 12; i++)
        {
            GameObject edgeObject =
                new GameObject($"Edge_{i + 1:00}");

            edgeObject.transform.SetParent(
                outlineRoot.transform
            );

            LineRenderer line =
                edgeObject.AddComponent<LineRenderer>();

            line.sharedMaterial = measurementMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.numCapVertices = 4;

            line.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            line.receiveShadows = false;

            edges.Add(line);
        }

        UpdateOutline();
    }

    private void UpdateOutline()
    {
        if (!IsComplete || edges.Count != 12)
            return;

        Vector3 widthVector = points[1] - points[0];
        widthVector.y = 0f;

        Vector3 depthDirection = Vector3.Cross(
            Vector3.up,
            widthVector.normalized
        );

        Vector3 depthVector = depthDirection * signedDepth;

        // Небольшое смещение предотвращает наложение
        // линий на поверхность стола или пола.
        Vector3 a = points[0] + Vector3.up * 0.012f;
        Vector3 b = a + widthVector;
        Vector3 c = b + depthVector;
        Vector3 d = a + depthVector;

        Vector3 heightVector =
            Vector3.up * availableHeightMeters;

        Vector3[] bottom = { a, b, c, d };
        Vector3[] top =
        {
            a + heightVector,
            b + heightVector,
            c + heightVector,
            d + heightVector
        };

        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;

            SetEdge(i, bottom[i], bottom[next]);
            SetEdge(i + 4, top[i], top[next]);
            SetEdge(i + 8, bottom[i], top[i]);
        }
    }

    private void SetEdge(int index, Vector3 start, Vector3 end)
    {
        edges[index].SetPosition(0, start);
        edges[index].SetPosition(1, end);
    }


    public bool TryGetCasePlacementPose(out Pose pose)
    {
        pose = default;

        if (!IsComplete || Width < 0.05f || Depth < 0.05f)
            return false;

        // Первая сторона измеренного прямоугольника.
        Vector3 widthVector = points[1] - points[0];
        widthVector.y = 0f;

        Vector3 widthDirection = widthVector.normalized;

        // Вторая сторона с учётом того,
        // по какую сторону выбрана третья точка.
        Vector3 depthDirection =
            Vector3.Cross(Vector3.up, widthDirection);

        Vector3 depthVector = depthDirection * signedDepth;

        // Геометрический центр измеренной области.
        Vector3 center =
            points[0] +
            widthVector * 0.5f +
            depthVector * 0.5f;

        center.y = points[0].y;

        // Локальная ось Z корпуса — его глубина.
        // Совмещаем её с длинной стороной области.
        Vector3 longDirection =
            Width >= Depth
                ? widthDirection
                : depthVector.normalized;

        Quaternion rotation =
            Quaternion.LookRotation(
                longDirection,
                Vector3.up
            );

        pose = new Pose(center, rotation);
        return true;
    }

    public void SetAvailableHeight(float heightMeters)
    {
        availableHeightMeters = Mathf.Max(0.1f, heightMeters);

        UpdateHeightText();
        UpdateOutline();
        UpdateStatus();
    }

    public void ResetMeasurement()
    {
        foreach (GameObject marker in markers)
        {
            if (marker != null)
                Destroy(marker);
        }

        markers.Clear();

        if (outlineRoot != null)
            Destroy(outlineRoot);

        outlineRoot = null;
        edges.Clear();

        pointCount = 0;
        signedDepth = 0f;

        Width = 0f;
        Depth = 0f;

        UpdateHeightText();
        UpdateStatus();
    }

    private void UpdateHeightText()
    {
        if (heightValueText != null)
        {
            heightValueText.text =
                "Доступная высота: " +
                DimensionFormatter.Centimeters(availableHeightMeters);
        }
    }

    private void UpdateStatus()
    {
        if (IsComplete)
        {
            SetStatus(
                $"Ширина: {DimensionFormatter.Centimeters(Width)}\n" +
                $"Глубина: {DimensionFormatter.Centimeters(Depth)}\n" +
                $"Высота (задана вручную): " +
                DimensionFormatter.Centimeters(Height)
            );
        }
        else
        {
            SetStatus(
                $"Измерение пространства\n" +
                $"Укажи точку {pointCount + 1} из 3"
            );
        }
    }

    private void SetStatus(string message)
    {
        if (measurementStatusText != null)
            measurementStatusText.text = message;

        Debug.Log(message);
    }

    public void SetVisualizationVisible(bool visible)
    {
        foreach (GameObject marker in markers)
        {
            if (marker != null)
                marker.SetActive(visible);
        }

        if (outlineRoot != null)
            outlineRoot.SetActive(visible);
    }
}
