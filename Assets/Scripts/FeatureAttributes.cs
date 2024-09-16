using System.Collections.Generic;
using System.Text;
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
        else
        {
            Debug.LogError($"Key '{key}' not found in FeatureAttributes or mismatch in key-value pairs.");
            return "Not available"; // Beri return default yang aman
        }
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

    // This method returns all attributes as a formatted string
    public string GetInformation()
    {
        StringBuilder info = new StringBuilder();

        for (int i = 0; i < keys.Count; i++)
        {
            info.AppendLine($"{keys[i]}: {values[i]}");
        }

        return info.ToString();
    }
}
