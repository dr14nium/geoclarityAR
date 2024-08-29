using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PlaneVisualizerSetup : MonoBehaviour
{
    public ARPlaneManager planeManager;

    private void Start()
    {
        // Disable colliders for all detected planes
        foreach (var plane in planeManager.trackables)
        {
            var collider = plane.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        // Register event to disable colliders on newly detected planes
        planeManager.planesChanged += OnPlanesChanged;
    }

    private void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Disable colliders on newly added planes
        foreach (var plane in args.added)
        {
            var collider = plane.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    private void OnDestroy()
    {
        // Remove event when this object is destroyed
        planeManager.planesChanged -= OnPlanesChanged;
    }
}
