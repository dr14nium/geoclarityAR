using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARSubsystems;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class GeospatialManager : MonoBehaviour
{
    public static GeospatialManager Instance;

    [Header("AR Components")]
    public ARSessionOrigin SessionOrigin;
    public ARSession Session;
    public ARCoreExtensions ARCoreExtensions;
    public AREarthManager EarthManager;
    public ARAnchorManager AnchorManager;
    public ARRaycastManager RaycastManager;
    public ARPlaneManager PlaneManager;

    [Header("Prefab Settings")]
    public List<GameObject> ObjectPrefabs = new List<GameObject>();

    [Header("Origin Settings")]
    public GameObject origin;

    private double Latitude;
    private double Longitude;

    private bool _isInARView = false;
    private bool _isReturning = false;
    private bool _isARInitialized = false;

    private IEnumerator _startLocationService = null;
    private ARGeospatialAnchor _currentAnchor = null;
    private GameObject _currentActiveObject = null; // Track the currently active object

    public delegate void ARStateChanged(string message, bool show);
    public static event ARStateChanged OnARStateChanged;

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

        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.orientation = ScreenOrientation.Portrait;

        Application.targetFrameRate = 60;

        ValidateComponents();

        if (origin != null)
        {
            var anchor = origin.GetComponent<WorldPositionAnchor>();
            if (anchor != null)
            {
                Latitude = anchor.referenceLat;
                Longitude = anchor.referenceLon;
            }
            else
            {
                Debug.LogError("Origin does not contain WorldPositionAnchor component.");
            }
        }
        else
        {
            Debug.LogError("Origin GameObject is not assigned.");
        }
    }

    private void ValidateComponents()
    {
        if (SessionOrigin == null) Debug.LogError("Cannot find ARSessionOrigin.");
        if (Session == null) Debug.LogError("Cannot find ARSession.");
        if (ARCoreExtensions == null) Debug.LogError("Cannot find ARCoreExtensions.");
        if (EarthManager == null) Debug.LogError("Cannot find AREarthManager.");
        if (AnchorManager == null) Debug.LogError("Cannot find ARAnchorManager.");
        if (PlaneManager == null) Debug.LogError("Cannot find ARPlaneManager.");
    }

    private void OnEnable()
    {
        _startLocationService = StartLocationService();
        StartCoroutine(_startLocationService);

        _isReturning = false;
        _isARInitialized = false;
        NotifyARStateChanged("Initializing AR Session...", true);
        SwitchToARView(true);

        TogglePlaneVisualization(1.0f);
    }

    private void OnDisable()
    {
        if (_startLocationService != null)
        {
            StopCoroutine(_startLocationService);
            _startLocationService = null;
        }
        Debug.Log("Stop location services.");
        Input.location.Stop();
    }

    private IEnumerator StartLocationService()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Debug.Log("Requesting the fine location permission.");
            Permission.RequestUserPermission(Permission.FineLocation);
            NotifyARStateChanged("Requesting the fine location permission.", true);
            yield return new WaitForSeconds(3.0f);
        }
#endif

        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Location service is disabled by the user.");
            NotifyARStateChanged("Location service is disabled by the user.", true);
            yield break;
        }

        Debug.Log("Starting location service.");
        NotifyARStateChanged("Starting location service.", true);
        Input.location.Start();

        while (Input.location.status == LocationServiceStatus.Initializing)
        {
            yield return null;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            string statusMessage = $"Location service ended with {Input.location.status} status.";
            Debug.LogWarning(statusMessage);
            NotifyARStateChanged(statusMessage, true);
            Input.location.Stop();
        }
    }

    private void SwitchToARView(bool enable)
    {
        _isInARView = enable;
        SessionOrigin.gameObject.SetActive(enable);
        Session.gameObject.SetActive(enable);
        ARCoreExtensions.gameObject.SetActive(enable);
        Debug.Log($"Switched to AR view: {enable}");
    }

    private void Update()
    {
        if (_isReturning)
        {
            return;
        }

        if (!_isARInitialized && ARSession.state == ARSessionState.SessionTracking)
        {
            if (EarthManager.EarthTrackingState == TrackingState.Tracking)
            {
                _isARInitialized = true;
                NotifyARStateChanged("Initializing AR Session complete", true);
                CreateTerrainAnchor(Latitude, Longitude);
            }
            else
            {
                NotifyARStateChanged("Earth tracking not available. Ensure you are in a clear, open area with good GPS signal.", true);
                Debug.LogWarning("Earth tracking not available.");
            }
        }

        UpdatePositionText();
    }

    private void CreateTerrainAnchor(double latitude, double longitude)
    {
        var earthState = EarthManager.EarthState;
        if (earthState != EarthState.Enabled)
        {
            NotifyARStateChanged("Earth state is not enabled.", true);
            Debug.LogError("Earth state is not enabled.");
            return;
        }

        ResolveAnchorOnTerrainPromise promise = AnchorManager.ResolveAnchorOnTerrainAsync(latitude, longitude, 0, Quaternion.identity);
        StartCoroutine(CheckTerrainPromise(promise));
    }

    private IEnumerator CheckTerrainPromise(ResolveAnchorOnTerrainPromise promise)
    {
        yield return new WaitUntil(() => promise.State != PromiseState.Pending);

        if (promise.State == PromiseState.Done)
        {
            var result = promise.Result;
            if (result.TerrainAnchorState == TerrainAnchorState.Success)
            {
                _currentAnchor = result.Anchor;
                if (_currentAnchor != null)
                {
                    ActivatePrefab(0); // Activate the first prefab by default
                    NotifyARStateChanged("Anchor created and first object instantiated.", true);
                    Debug.Log("Anchor created and first object instantiated.");
                }
            }
            else
            {
                NotifyARStateChanged("Failed to create terrain anchor.", true);
                Debug.LogError("Failed to create terrain anchor.");
            }
        }
    }

    public void ActivatePrefab(int prefabIndex)
    {
        if (prefabIndex >= 0 && prefabIndex < ObjectPrefabs.Count && _currentAnchor != null)
        {
            // Remove the current active object if it exists
            if (_currentActiveObject != null)
            {
                Destroy(_currentActiveObject);
            }

            // Instantiate and activate the new prefab
            _currentActiveObject = Instantiate(ObjectPrefabs[prefabIndex], _currentAnchor.transform);
            _currentActiveObject.transform.position = _currentAnchor.transform.position;
            _currentActiveObject.transform.rotation = _currentAnchor.transform.rotation;
            _currentActiveObject.transform.localScale = Vector3.one;

            Debug.Log($"Prefab {prefabIndex} instantiated and activated.");
        }
        else
        {
            Debug.LogWarning("Prefab index out of range or anchor is not created yet.");
        }
    }

    private void NotifyARStateChanged(string message, bool show)
    {
        OnARStateChanged?.Invoke(message, show);
    }

    private void UpdatePositionText()
    {
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            UIManager.Instance.UpdatePositionText("AR session is not tracking.");
            return;
        }

        if (EarthManager.EarthTrackingState != TrackingState.Tracking)
        {
            UIManager.Instance.UpdatePositionText("Earth tracking is not available.");
            return;
        }

        var pose = EarthManager.CameraGeospatialPose;

        string positionText = string.Format(
            "Latitude/Longitude: {0}, {1}\n" +
            "Horizontal Accuracy: {2}m\n" +
            "Altitude: {3}m\n" +
            "Vertical Accuracy: {4}m\n" +
            "Orientation Yaw Accuracy: {5}°",
            pose.Latitude.ToString("F6"),
            pose.Longitude.ToString("F6"),
            pose.HorizontalAccuracy.ToString("F6"),
            pose.Altitude.ToString("F2"),
            pose.VerticalAccuracy.ToString("F2"),
            pose.OrientationYawAccuracy.ToString("F1"));

        UIManager.Instance.UpdatePositionText(positionText);
    }

    public void TogglePlaneVisualization(float value)
    {
        bool isOn = value > 0.5f;
        PlaneManager.enabled = isOn;
        foreach (var plane in PlaneManager.trackables)
        {
            plane.gameObject.SetActive(isOn);
        }
    }
}
