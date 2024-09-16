using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class CustomPlaneVisualizer : MonoBehaviour
{
    [SerializeField]
    private Material transparentPlaneMaterial;

    private ARPlaneManager arPlaneManager;

    void Awake()
    {
        arPlaneManager = GetComponent<ARPlaneManager>();
        arPlaneManager.planesChanged += OnPlanesChanged;
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Apply the transparent material to new planes
        foreach (ARPlane plane in args.added)
        {
            plane.GetComponent<MeshRenderer>().material = transparentPlaneMaterial;
        }
    }
}
