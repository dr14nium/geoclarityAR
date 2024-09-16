using UnityEngine;

public class DropdownAttribute : PropertyAttribute
{
    public string ListFieldName;

    public DropdownAttribute(string listFieldName)
    {
        ListFieldName = listFieldName;
    }
}
