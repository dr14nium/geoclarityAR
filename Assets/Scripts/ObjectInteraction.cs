using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectInteraction : MonoBehaviour
{
    public FeatureAttributes featureAttributes;

    private void OnMouseDown()
    {
        if (IsPointerOverUIObject())
        {
            Debug.Log("Pointer is over a UI element, ignoring object interaction.");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log($"Raycast hit object: {hit.transform.name}");

            if (hit.transform == transform)
            {
                if (featureAttributes != null)
                {
                    Debug.Log($"[ObjectInteraction] Triggered on {gameObject.name} with FeatureAttributes.");
                    ObjectInfoManager.Instance.ShowObjectInformation(featureAttributes);
                }
                else
                {
                    Debug.LogWarning($"[ObjectInteraction] FeatureAttributes component is missing on {gameObject.name}.");
                }
            }
        }
    }

    private bool IsPointerOverUIObject()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                return true;
            }
        }

        return false;
    }
}