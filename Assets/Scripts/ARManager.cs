using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class ARManager : MonoBehaviour
{
    public static ARManager Instance;

    [Header("AR Components")]
    public ARSessionOrigin SessionOrigin;
    public ARSession Session;
    public ARCoreExtensions ARCoreExtensions;
    public AREarthManager EarthManager;
    public ARAnchorManager AnchorManager;
    public ARRaycastManager RaycastManager;
    public ARPlaneManager PlaneManager;

    [Header("UI Elements")]
    public Text PositionText;

    [Header("Popups")]
    public GameObject GPSDisabledPopup;
    public Button ActivateGPSButton;
    public Button ExitAppButton;

    public GameObject VPSNotAvailablePopup;
    public Button ContinueWithoutVPSButton;

    public ARGeospatialAnchor _currentAnchor = null;
    private GameObject _currentActiveObject = null;
    private bool _isARInitialized = false;
    private bool _isPositionUpdatedShown = false;
    private bool _isPlaneDetectedShown = false;
    private bool _gpsActive = false;
    private bool _vpsChecked = false;

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

        ValidateComponents();
        ActivateGPSButton.onClick.AddListener(ActivateGPS);
        ExitAppButton.onClick.AddListener(ExitApplication);
        ContinueWithoutVPSButton.onClick.AddListener(ContinueWithoutVPS);
        PositionText.gameObject.SetActive(false); // Hide PositionText at start
    }

    private void ValidateComponents()
    {
        if (SessionOrigin == null)
        {
            SnackBarManager.Instance.ShowSnackBar("ARSessionOrigin not found.", true);
        }
        if (Session == null)
        {
            SnackBarManager.Instance.ShowSnackBar("ARSession not found.", true);
        }
        if (ARCoreExtensions == null)
        {
            SnackBarManager.Instance.ShowSnackBar("ARCoreExtensions not found.", true);
        }
        if (EarthManager == null)
        {
            SnackBarManager.Instance.ShowSnackBar("AREarthManager not found.", true);
        }
        if (AnchorManager == null)
        {
            SnackBarManager.Instance.ShowSnackBar("ARAnchorManager not found.", true);
        }
        if (PlaneManager == null)
        {
            SnackBarManager.Instance.ShowSnackBar("ARPlaneManager not found.", true);
        }
        if (PositionText == null)
        {
            SnackBarManager.Instance.ShowSnackBar("PositionText UI element not found.", true);
        }
    }

    private void OnEnable()
    {
        PlaneManager.planesChanged += OnPlanesChanged;
        StartCoroutine(CheckAndStartLocationService());
    }

    private void OnDisable()
    {
        Input.location.Stop();
        PlaneManager.planesChanged -= OnPlanesChanged;
    }

    private IEnumerator CheckAndStartLocationService()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1);
        }

        if (!Input.location.isEnabledByUser)
        {
            GPSDisabledPopup.SetActive(true);
            DisableAR();
            yield break;
        }

        Input.location.Start();
        while (Input.location.status == LocationServiceStatus.Initializing)
        {
            yield return null;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            _gpsActive = true;
            GPSDisabledPopup.SetActive(false);
            SnackBarManager.Instance.ShowSnackBar("Location Service is running.", true);
            InitializeAR();
        }
        else
        {
            SnackBarManager.Instance.ShowSnackBar("Failed to start Location Service.", true);
        }
    }

    private void InitializeAR()
    {
        if (_gpsActive && !_isARInitialized)
        {
            _isARInitialized = true;
            Session.enabled = true;
            PlaneManager.enabled = true;
            RaycastManager.enabled = true;
            SnackBarManager.Instance.ShowSnackBar("AR Session initialized.", true);
            StartCoroutine(CheckVPSAvailabilityWithDelay());
        }
    }

    private IEnumerator CheckVPSAvailabilityWithDelay()
    {
        yield return new WaitForSeconds(Random.Range(15f, 30f));

        if (EarthManager.EarthTrackingState != TrackingState.Tracking)
        {
            VPSNotAvailablePopup.SetActive(true);
            _vpsChecked = true;
        }
    }

    private void DisableAR()
    {
        Session.enabled = false;
        PlaneManager.enabled = false;
        _isARInitialized = false;
    }

    private void Update()
    {
        if (_gpsActive && _isARInitialized)
        {
            if (_vpsChecked && EarthManager.EarthTrackingState != TrackingState.Tracking)
            {
                VPSNotAvailablePopup.SetActive(true);
            }

            UpdatePositionText();

            // Panggil pembuatan geospatial anchor otomatis
            if (EarthManager.EarthTrackingState == TrackingState.Tracking && _currentAnchor == null)
            {
                double latitude = EarthManager.CameraGeospatialPose.Latitude;
                double longitude = EarthManager.CameraGeospatialPose.Longitude;

                CreateGeospatialAnchor(latitude, longitude);
            }
        }
    }

    private void UpdatePositionText()
    {
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            PositionText.text = "AR session is not tracking.";
            return;
        }

        if (EarthManager.EarthTrackingState != TrackingState.Tracking)
        {
            PositionText.text = "Earth tracking is not available.";
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

        PositionText.text = positionText;

        if (!_isPositionUpdatedShown)
        {
            PositionText.gameObject.SetActive(true);
            SnackBarManager.Instance.ShowSnackBar("Position updated.", true);
            _isPositionUpdatedShown = true;
        }
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (!_isPlaneDetectedShown && args.added.Count > 0)
        {
            SnackBarManager.Instance.ShowSnackBar("Plane detected.", true);
            _isPlaneDetectedShown = true;
        }
    }

    private void CreateGeospatialAnchor(double latitude, double longitude)
    {
        var earthState = EarthManager.EarthState;
        if (earthState != EarthState.Enabled)
        {
            SnackBarManager.Instance.ShowSnackBar("Earth state is not enabled.", true);
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
                    // Panggil object default dari LayerManager
                    LayerManager.Instance.InstantiateDefaultObject();
                    SnackBarManager.Instance.ShowSnackBar("Anchor created and default object instantiated.", true);
                }
            }
            else
            {
                SnackBarManager.Instance.ShowSnackBar("Failed to create terrain anchor.", true);
            }
        }
    }

    // Fungsi untuk tombol Activate GPS
    private void ActivateGPS()
    {
#if UNITY_ANDROID
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.LOCATION_SOURCE_SETTINGS");
            currentActivity.Call("startActivity", intent);
        }
#endif
        GPSDisabledPopup.SetActive(false);
        StartCoroutine(CheckAndStartLocationService());
    }

    private void ExitApplication()
    {
        Application.Quit();
    }

    private void ContinueWithoutVPS()
    {
        VPSNotAvailablePopup.SetActive(false);
    }

    // Fungsi untuk mengaktifkan prefab di LayerManager
    public void ActivatePrefabInLayer(string prefabName)
    {
        LayerManager.Instance.ActivatePrefab(prefabName);
    }

    public GameObject CurrentActiveObject
    {
        get { return _currentActiveObject; }
        set
        {
            // Sembunyikan objek sebelumnya jika ada
            if (_currentActiveObject != null)
            {
                _currentActiveObject.SetActive(false);  // Sembunyikan object sebelumnya
            }

            // Set object baru sebagai object aktif
            _currentActiveObject = value;

            // Tampilkan object baru
            if (_currentActiveObject != null)
            {
                _currentActiveObject.SetActive(true);  // Tampilkan object baru
            }

            // Inform LayerManager about the active object change if needed
            LayerManager.Instance.CurrentActiveObject = _currentActiveObject;  // Beri tahu LayerManager object mana yang aktif
        }
    }
}