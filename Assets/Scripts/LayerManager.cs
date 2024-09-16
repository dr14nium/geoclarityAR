using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PrefabKeyInfo
{
    public List<KeyInfo> informationKeys = new List<KeyInfo>();
}

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;

    [Header("Layer Menu")]
    public GameObject LayerMenu;
    public Button LayerButton;
    public Button LayerCloseButton;

    [Header("Prefab Settings")]
    public List<GameObject> ObjectPrefabs = new List<GameObject>();  // List of prefab objects
    public List<Button> prefabButtons = new List<Button>();          // List of prefab buttons
    public List<string> prefabNames = new List<string>();            // Names of prefabs (taken from ObjectPrefabs)
    public List<PrefabKeyInfo> prefabKeyInfoList = new List<PrefabKeyInfo>(); // List of KeyInfo for each prefab

    [Header("Default Object")]
    [Dropdown("ObjectPrefabs")]  // Dropdown to select the default object
    public string defaultPrefabName;  // Name of the default object

    private GameObject _currentActiveObject = null;  // Holds the currently active object

    // Property to access and set the active object
    public GameObject CurrentActiveObject
    {
        get { return _currentActiveObject; }
        set
        {
            if (_currentActiveObject != null)
            {
                _currentActiveObject.SetActive(false);  // Deactivate the previous object
            }

            _currentActiveObject = value;

            if (_currentActiveObject != null)
            {
                _currentActiveObject.SetActive(true);   // Activate the new object
                ToggleCollidersInActivePrefab(true);    // Activate colliders when measurement mode is inactive
            }
        }
    }

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
        LayerButton.onClick.AddListener(ToggleLayerMenu);
        LayerCloseButton.onClick.AddListener(CloseLayerMenu);

        // Add listeners to each prefab button
        for (int i = 0; i < prefabButtons.Count; i++)
        {
            int index = i;  // Capture index in closure
            prefabButtons[i].onClick.AddListener(() => OnPrefabButtonClicked(index));
        }

        PopulatePrefabNames();  // Populate prefabNames according to ObjectPrefabs
    }

    private void OnDisable()
    {
        LayerButton.onClick.RemoveListener(ToggleLayerMenu);
        LayerCloseButton.onClick.RemoveListener(CloseLayerMenu);

        // Remove listeners from each prefab button
        for (int i = 0; i < prefabButtons.Count; i++)
        {
            prefabButtons[i].onClick.RemoveListener(() => OnPrefabButtonClicked(i));
        }
    }

    // Method that gets triggered when a prefab button is clicked
    private void OnPrefabButtonClicked(int prefabIndex)
    {
        if (prefabIndex >= 0 && prefabIndex < prefabNames.Count)
        {
            // Activate the prefab and deactivate the Layer Menu
            ActivatePrefab(prefabNames[prefabIndex]);
            LayerMenu.SetActive(false);  // Deactivate the Layer Menu
        }
    }

    // Toggle the visibility of the Layer Menu
    private void ToggleLayerMenu()
    {
        if (MeasurementManager.Instance != null && MeasurementManager.Instance.IsMeasuring())
        {
            Debug.Log("[LAYER_MANAGER] Cannot open Layer Menu while Measurement Mode is active.");
            return;
        }

        LayerMenu.SetActive(!LayerMenu.activeSelf);
        Debug.Log("[LAYER_MANAGER] Layer Menu toggled.");
    }

    // Close the Layer Menu
    private void CloseLayerMenu()
    {
        LayerMenu.SetActive(false);
    }

    // Toggle colliders on or off for the currently active prefab
    public void ToggleCollidersInActivePrefab(bool isActive)
    {
        if (_currentActiveObject != null)
        {
            Collider[] colliders = _currentActiveObject.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = isActive;
            }
        }
    }

    // This function will be called by MeasurementManager when measurement mode is toggled
    public void SetCollidersActive(bool isActive)
    {
        ToggleCollidersInActivePrefab(isActive);
    }

    // Populate prefabNames based on ObjectPrefabs
    private void PopulatePrefabNames()
    {
        prefabNames.Clear();
        foreach (var prefab in ObjectPrefabs)
        {
            if (prefab != null)
            {
                prefabNames.Add(prefab.name);  // Add the prefab name to prefabNames
            }
        }
    }

    // Instantiate the default object based on the dropdown selection
    public void InstantiateDefaultObject()
    {
        if (ObjectPrefabs.Count > 0 && ARManager.Instance._currentAnchor != null)
        {
            ActivatePrefab(defaultPrefabName);  // Activate the default object by name
        }
    }

    // Activate a prefab by name
    public void ActivatePrefab(string prefabName)
    {
        string cleanPrefabName = prefabName.Replace("(Clone)", "").Trim();
        GameObject prefab = ObjectPrefabs.Find(p => p.name == cleanPrefabName);

        if (prefab != null && ARManager.Instance._currentAnchor != null)
        {
            if (_currentActiveObject != null)
            {
                Destroy(_currentActiveObject);  // Destroy the previously active object
            }

            GameObject newObject = Instantiate(prefab, ARManager.Instance._currentAnchor.transform.position, ARManager.Instance._currentAnchor.transform.rotation);
            newObject.transform.localScale = Vector3.one;

            CurrentActiveObject = newObject;
            ARManager.Instance.CurrentActiveObject = newObject;

            ShowOutlineSelectionBox(newObject);

            SnackBarManager.Instance.ShowSnackBar($"Prefab '{cleanPrefabName}' instantiated.", true);
        }
        else
        {
            SnackBarManager.Instance.ShowSnackBar($"Prefab '{cleanPrefabName}' not found or anchor not created.", true);
        }
    }

    // Show the outline selection box for the active object
    private void ShowOutlineSelectionBox(GameObject activeObject)
    {
        Outline outline = activeObject.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = true;  // Enable the outline selection box
        }
    }

    // Check if the Layer Menu is active
    public bool IsLayerMenuActive()
    {
        return LayerMenu.activeSelf;
    }

    // Get KeyInfo for a prefab by name
    public List<KeyInfo> GetKeyInfoForPrefab(string prefabName)
    {
        string cleanPrefabName = prefabName.Replace("(Clone)", "").Trim();

        for (int i = 0; i < ObjectPrefabs.Count; i++)
        {
            if (ObjectPrefabs[i].name == cleanPrefabName)
            {
                return prefabKeyInfoList[i].informationKeys;
            }
        }

        Debug.LogWarning($"No KeyInfo found for prefab '{cleanPrefabName}'.");
        return null;
    }
}