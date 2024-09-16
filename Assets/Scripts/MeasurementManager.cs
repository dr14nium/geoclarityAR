using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MeasurementManager : MonoBehaviour
{
    public static MeasurementManager Instance;

    [Header("UI Elements")]
    public Text distanceText;
    public Text areaText;
    public GameObject MeasurePanel;
    public Button MeasureButton;
    public GameObject measurementPointPrefab;

    [Header("Crosshair Settings")]
    public float crosshairThickness = 2.0f;
    public Color crosshairColor = Color.red;
    public float crosshairLength = 20.0f;

    [Header("Measurement Settings")]
    public float measurementPointScale = 0.05f; // Scale of the point prefab
    public float snappingDistance = 0.1f; // Snapping tolerance for polygon closure

    private bool isMeasuring = false;
    private List<GameObject> measurementPoints = new List<GameObject>();
    private LineRenderer lineRenderer;
    private bool isPolygonClosed = false;

    // LayerMask for restricting raycasts only to AR planes
    public LayerMask arPlaneLayerMask; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 0;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.red;
    }

    private void OnEnable()
    {
        MeasureButton.onClick.AddListener(ToggleMeasurementMode);
        ShowMeasurementPanel(false);
    }

    private void OnDisable()
    {
        MeasureButton.onClick.RemoveListener(ToggleMeasurementMode);
    }

    private void Update()
    {
        if (isMeasuring && Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            HandleMeasurementInput();
        }
    }

    public void ShowMeasurementPanel(bool show)
    {
        MeasurePanel.SetActive(show);

        if (show)
        {
            distanceText.text = "0.000 m";
            areaText.text = "0.000 m²";
        }
    }

    private void ToggleMeasurementMode()
    {
        if (LayerManager.Instance != null && LayerManager.Instance.IsLayerMenuActive())
        {
            Debug.Log("[MEASUREMENT_MANAGER] Cannot activate Measurement Mode while Layer Menu is active.");
            return;
        }

        isMeasuring = !isMeasuring;
        ShowMeasurementPanel(isMeasuring);
        lineRenderer.enabled = isMeasuring;

        if (isMeasuring)
        {
            // Measurement mode starts, deactivate colliders on active prefab
            LayerManager.Instance.SetCollidersActive(false);
            ResetMeasurement();
            Debug.Log("[MEASUREMENT_MANAGER] Measurement mode started.");
        }
        else
        {
            // Measurement mode ends, reactivate colliders on active prefab
            LayerManager.Instance.SetCollidersActive(true);
            ResetMeasurement();
            Debug.Log("[MEASUREMENT_MANAGER] Measurement mode ended.");
        }
    }

    public bool IsMeasuring()
    {
        return isMeasuring;
    }

    private void HandleMeasurementInput()
    {
        if (isPolygonClosed)
        {
            UpdateMeasurementValues(0, 0);
            ResetMeasurement();
            Debug.Log("[MEASUREMENT_MANAGER] Polygon closed, resetting measurement.");
            return;
        }

        Vector2 screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        if (ARManager.Instance.RaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            ARRaycastHit hit = hits[0];
            Vector3 hitPosition = hit.pose.position;
            AddMeasurementPoint(hitPosition);
            Debug.Log($"[MEASUREMENT_MANAGER] Plane hit at position: {hitPosition}");
        }
        else
        {
            Ray ray = Camera.main.ScreenPointToRay(screenCenter);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, arPlaneLayerMask))
            {
                AddMeasurementPoint(hitInfo.point);
                Debug.Log($"[MEASUREMENT_MANAGER] AR plane hit at position: {hitInfo.point}");
            }
            else
            {
                Debug.LogError("[MEASUREMENT_MANAGER] No valid hit detected.");
            }
        }
    }

    private void AddMeasurementPoint(Vector3 position)
    {
        if (measurementPoints.Count > 0 && Vector3.Distance(measurementPoints[0].transform.position, position) <= snappingDistance)
        {
            position = measurementPoints[0].transform.position;
            CompleteMeasurement();
            isPolygonClosed = true;
        }
        else
        {
            GameObject point = Instantiate(measurementPointPrefab, position, Quaternion.identity);
            point.transform.localScale = Vector3.one * measurementPointScale;
            measurementPoints.Add(point);

            lineRenderer.positionCount = measurementPoints.Count;
            lineRenderer.SetPosition(measurementPoints.Count - 1, position);

            if (!isPolygonClosed)
            {
                UpdateDistanceText();
            }
        }
    }

    private void UpdateDistanceText()
    {
        if (measurementPoints.Count < 2) return;

        float totalDistance = 0f;
        for (int i = 1; i < measurementPoints.Count; i++)
        {
            totalDistance += Vector3.Distance(measurementPoints[i - 1].transform.position, measurementPoints[i].transform.position);
        }

        UpdateMeasurementValues(totalDistance, 0);
    }

    private void CompleteMeasurement()
    {
        if (measurementPoints.Count > 2)
        {
            float perimeter = CalculatePerimeter(measurementPoints);
            float area = CalculatePolygonArea(measurementPoints);

            Debug.Log($"[MEASUREMENT_MANAGER] Measurement completed. Perimeter: {perimeter} m, Area: {area} m²");

            lineRenderer.positionCount = measurementPoints.Count + 1;
            lineRenderer.SetPosition(measurementPoints.Count, measurementPoints[0].transform.position);

            UpdateMeasurementValues(perimeter, area);
        }
    }

    private float CalculatePolygonArea(List<GameObject> points)
    {
        float area = 0f;
        int numPoints = points.Count;

        if (numPoints < 3) return 0f;

        for (int i = 0; i < numPoints; i++)
        {
            Vector3 p1 = points[i].transform.position;
            Vector3 p2 = points[(i + 1) % numPoints].transform.position;

            area += (p1.x * p2.z) - (p2.x * p1.z);
        }

        area = Mathf.Abs(area) / 2.0f;

        Debug.Log($"[MEASUREMENT_MANAGER] Calculated area: {area} m²");
        return area;
    }

    private float CalculatePerimeter(List<GameObject> points)
    {
        float perimeter = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            perimeter += Vector3.Distance(points[i - 1].transform.position, points[i].transform.position);
        }

        perimeter += Vector3.Distance(points[points.Count - 1].transform.position, points[0].transform.position);
        return perimeter;
    }

    private void ResetMeasurement()
    {
        foreach (var point in measurementPoints)
        {
            Destroy(point);
        }

        measurementPoints.Clear();
        lineRenderer.positionCount = 0;
        isPolygonClosed = false;

        Debug.Log("[MEASUREMENT_MANAGER] Measurement reset.");
    }

    private void UpdateMeasurementValues(float perimeter, float area)
    {
        distanceText.text = perimeter > 0 ? $"{perimeter:F3} m" : "0.000 m";
        areaText.text = area > 0 ? $"{area:F3} m²" : "0.000 m²";

        Debug.Log($"[MEASUREMENT_MANAGER] UI updated: Perimeter: {perimeter:F3}, Area: {area:F3}");
    }

    private void OnGUI()
    {
        if (!isMeasuring) return;

        Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
        var oldColor = GUI.color;
        GUI.color = crosshairColor;

        GUI.DrawTexture(new Rect(center.x - crosshairLength / 2, center.y - crosshairThickness / 2, crosshairLength, crosshairThickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(center.x - crosshairThickness / 2, center.y - crosshairLength / 2, crosshairThickness, crosshairLength), Texture2D.whiteTexture);

        GUI.color = oldColor;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
