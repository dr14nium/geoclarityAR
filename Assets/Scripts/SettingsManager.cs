using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("UI Elements")]
    public Slider PlaneVisualizationSlider;
    public GameObject SettingsMenu;
    public Button SettingsToggleButton;

    private bool isSettingsMenuActive = false;

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
    }

    private void OnEnable()
    {
        SettingsToggleButton.onClick.AddListener(ToggleSettingsMenu);
        PlaneVisualizationSlider.onValueChanged.AddListener(UpdatePlaneVisualization);

        StartCoroutine(SyncPlaneVisualizationSliderWithPlaneManager());
    }

    private void OnDisable()
    {
        SettingsToggleButton.onClick.RemoveListener(ToggleSettingsMenu);
        PlaneVisualizationSlider.onValueChanged.RemoveListener(UpdatePlaneVisualization);
    }

    private IEnumerator SyncPlaneVisualizationSliderWithPlaneManager()
    {
        yield return new WaitForSeconds(0.1f); 

        PlaneVisualizationSlider.value = ARManager.Instance.PlaneManager.enabled ? 1.0f : 0.0f;
    }

    private void ToggleSettingsMenu()
    {
        isSettingsMenuActive = !isSettingsMenuActive;
        SettingsMenu.SetActive(isSettingsMenuActive);
    }

    public void UpdatePlaneVisualization(float value)
    {
        bool planesVisible = value > 0.5f;
        ARManager.Instance.PlaneManager.enabled = planesVisible;

        SetAllPlanesActive(planesVisible);

        string message = planesVisible ? "Plane visualization enabled." : "Plane visualization disabled.";
        SnackBarManager.Instance.ShowSnackBar(message, true);
    }

    private void SetAllPlanesActive(bool isActive)
    {
        foreach (var plane in ARManager.Instance.PlaneManager.trackables)
        {
            plane.gameObject.SetActive(isActive);
        }
    }

    private void Update()
    {
        // Cek apakah SettingsMenu aktif dan pengguna mengklik di luar area UI
        if (isSettingsMenuActive && Input.GetMouseButtonDown(0))
        {
            // Jika klik terjadi di luar elemen UI (tidak mengenai UI), maka sembunyikan SettingsMenu
            if (!IsPointerOverUIElement())
            {
                ToggleSettingsMenu();
            }
        }
    }

    // Fungsi untuk mengecek apakah pointer berada di atas elemen UI
    private bool IsPointerOverUIElement()
    {
        // Cek apakah pointer mengenai elemen UI saja (tidak objek di dalam scene)
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Loop over raycast results to check if any of the results are UI elements
        foreach (RaycastResult result in results)
        {
            // Jika raycast mengenai UI element
            if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                return true;
            }
        }
        return false;
    }
}
