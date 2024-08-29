using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class PrefabKeyInfo
{
    public List<KeyInfo> informationKeys = new List<KeyInfo>();
}

[System.Serializable]
public class KeyInfo
{
    public string displayText;  // Text to display before the key's value
    public string key;          // Key from the feature info to display
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public GameObject SnackBarPanel;
    public Text SnackBarText;
    public Text PositionText;

    public GameObject ObjectInformationPanel;  // Popup panel
    public Button CloseButton;

    [Header("Content Settings")]
    public Transform ContentParent;  // Assign the ContentParent
    public GameObject RowPrefab;     // Assign the RowPrefab

    [Header("Layer and Settings")]
    public Button LayerButton; // Button to toggle Layer Menu
    public GameObject LayerMenu;
    public Button LayerCloseButton;
    public Button SettingsButton;
    public GameObject SettingsMenu;

    public List<Button> LayerButtons; // Buttons to activate each prefab

    [Header("Configurable Object Information")]
    public List<PrefabKeyInfo> informationKeys = new List<PrefabKeyInfo>(); // List of PrefabKeyInfo

    [Header("Visualization Sliders")]
    public Slider PlaneVisualizationSlider;

    private int currentPrefabIndex = 0; // Default to first prefab

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
        GeospatialManager.OnARStateChanged += ShowSnackBar;
        CloseButton.onClick.AddListener(HideObjectInformation);

        LayerButton.onClick.AddListener(ToggleLayerMenu);
        LayerCloseButton.onClick.AddListener(HideLayerMenu);
        SettingsButton.onClick.AddListener(ToggleSettingsMenu);

        PlaneVisualizationSlider.value = 1.0f; // Assuming 1.0f means "ON" for visualization
        PlaneVisualizationSlider.onValueChanged.AddListener((value) =>
        {
            Debug.Log($"Plane Visualization Slider Value: {value}");
            GeospatialManager.Instance.TogglePlaneVisualization(value);
        });

        InitializeInformationKeys(); // Initialize the informationKeys list based on the number of prefabs

        // Set default object to be active
        GeospatialManager.Instance.ActivatePrefab(0);

        for (int i = 0; i < LayerButtons.Count; i++)
        {
            int index = i;
            LayerButtons[i].onClick.AddListener(() =>
            {
                GeospatialManager.Instance.ActivatePrefab(index);
                currentPrefabIndex = index;
            });
        }
    }

    private void OnDisable()
    {
        GeospatialManager.OnARStateChanged -= ShowSnackBar;

        LayerButton.onClick.RemoveListener(ToggleLayerMenu);
        LayerCloseButton.onClick.RemoveListener(HideLayerMenu);
        SettingsButton.onClick.RemoveListener(ToggleSettingsMenu);

        PlaneVisualizationSlider.onValueChanged.RemoveAllListeners();

        foreach (var button in LayerButtons)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    private void InitializeInformationKeys()
    {
        // Clear any existing keys
        informationKeys.Clear();

        // Create default KeyInfo for each prefab in GeospatialManager
        for (int i = 0; i < GeospatialManager.Instance.ObjectPrefabs.Count; i++)
        {
            var prefabKeyInfo = new PrefabKeyInfo
            {
                informationKeys = new List<KeyInfo>
                {
                    new KeyInfo { displayText = "Informasi", key = "info" }
                }
            };
            informationKeys.Add(prefabKeyInfo);
        }
    }

    private void Update()
    {
        // Check if the popup is active and there is a click outside of it
        if (ObjectInformationPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            // Hide popup if click is outside
            if (!IsPointerOverGameObject(ObjectInformationPanel) && !EventSystem.current.IsPointerOverGameObject())
            {
                HideObjectInformation();
            }
        }

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            if (LayerMenu.activeSelf)
            {
                HideLayerMenu();
            }
            if (SettingsMenu.activeSelf)
            {
                HideSettingsMenu();
            }
        }
    }

    private bool IsPointerOverGameObject(GameObject target)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject == target || result.gameObject.transform.IsChildOf(target.transform))
            {
                return true;
            }
        }

        return false;
    }

    private void ShowSnackBar(string message, bool show)
    {
        SnackBarText.text = message;
        SnackBarPanel.SetActive(show);

        if (show)
        {
            StartCoroutine(HideSnackBarAfterDelay(3.0f));
        }
    }

    private IEnumerator HideSnackBarAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SnackBarPanel.SetActive(false);
    }

    public void UpdatePositionText(string message)
    {
        PositionText.text = message;
    }

    public void ShowObjectInformation(FeatureAttributes featureAttributes)
    {
        if (featureAttributes == null)
        {
            Debug.LogWarning("FeatureAttributes is null, no information available.");
            return;
        }

        List<KeyInfo> currentInfoKeys = GetCurrentInfoKeys();

        if (currentInfoKeys == null || currentInfoKeys.Count == 0)
        {
            Debug.LogWarning("No keys available for display in UI.");
            return;
        }

        foreach (Transform child in ContentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var keyInfo in currentInfoKeys)
        {
            var rowInstance = Instantiate(RowPrefab, ContentParent);
            var texts = rowInstance.GetComponentsInChildren<Text>();

            texts[0].text = keyInfo.displayText;   // LabelText
            texts[1].text = ":";                   // ColonText
            texts[2].text = featureAttributes.GetAttributeValue(keyInfo.key); // ValueText
        }

        ObjectInformationPanel.SetActive(true);
    }

    public void HideObjectInformation()
    {
        ObjectInformationPanel.SetActive(false);
    }

    private List<KeyInfo> GetCurrentInfoKeys()
    {
        if (currentPrefabIndex >= 0 && currentPrefabIndex < informationKeys.Count)
        {
            return informationKeys[currentPrefabIndex].informationKeys;
        }

        return null;
    }

    private void ToggleLayerMenu()
    {
        if (LayerMenu != null)
        {
            LayerMenu.SetActive(!LayerMenu.activeSelf);
        }
    }

    private void HideLayerMenu()
    {
        if (LayerMenu != null)
        {
            LayerMenu.SetActive(false);
        }
    }

    private void ToggleSettingsMenu()
    {
        if (SettingsMenu != null)
        {
            SettingsMenu.SetActive(!SettingsMenu.activeSelf);
        }
    }

    private void HideSettingsMenu()
    {
        if (SettingsMenu != null)
        {
            SettingsMenu.SetActive(false);
        }
    }
}
