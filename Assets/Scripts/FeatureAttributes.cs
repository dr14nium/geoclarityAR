using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FeatureAttributes", menuName = "ScriptableObjects/FeatureAttributes", order = 1)]
public class FeatureAttributes : ScriptableObject
{
    public List<string> keys = new List<string>();
    public List<string> values = new List<string>();

    // This method is for initialization from a dictionary
    public void Initialize(Dictionary<string, object> attributes)
    {
        keys.Clear();
        values.Clear();

        foreach (var attribute in attributes)
        {
            keys.Add(attribute.Key);
            values.Add(attribute.Value?.ToString() ?? "null");
        }
    }

    // This method allows you to get the value for a given key
    public string GetAttributeValue(string key)
    {
        int index = keys.IndexOf(key);
        if (index >= 0 && index < values.Count)
        {
            return values[index];
        }
        return "Not available";
    }

    // This method allows you to set the value for a given key
    public void SetAttributeValue(string key, string value)
    {
        int index = keys.IndexOf(key);
        if (index >= 0 && index < values.Count)
        {
            values[index] = value;
        }
    }
}