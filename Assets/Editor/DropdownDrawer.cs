using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

[CustomPropertyDrawer(typeof(DropdownAttribute))]
public class DropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        DropdownAttribute dropdownAttribute = (DropdownAttribute)attribute;
        string listFieldName = dropdownAttribute.ListFieldName;

        // Dapatkan list dari field yang dituju
        List<string> dropdownList = GetDropdownList(property, listFieldName);

        if (dropdownList != null && dropdownList.Count > 0)
        {
            // Tampilkan dropdown di Inspector
            int index = Mathf.Max(dropdownList.IndexOf(property.stringValue), 0);
            index = EditorGUI.Popup(position, label.text, index, dropdownList.ToArray());
            property.stringValue = dropdownList[index];
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }

    private List<string> GetDropdownList(SerializedProperty property, string listFieldName)
    {
        // Mendapatkan target object yang dimaksud
        Object target = property.serializedObject.targetObject;
        FieldInfo field = target.GetType().GetField(listFieldName);

        if (field != null)
        {
            // Dapatkan list dari field yang dimaksud
            var list = field.GetValue(target) as List<GameObject>;
            if (list != null)
            {
                List<string> names = new List<string>();
                foreach (var item in list)
                {
                    if (item != null)
                    {
                        names.Add(item.name); // Ambil nama dari GameObject dalam list
                    }
                }
                return names;
            }
        }

        return null;
    }
}