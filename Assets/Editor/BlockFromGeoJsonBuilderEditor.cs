using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlockFromGeoJsonBuilder))]
public class BlockFromGeoJsonBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BlockFromGeoJsonBuilder builder = (BlockFromGeoJsonBuilder)target;

        // GeoJSON file and world position anchor
        builder.apiUrl = EditorGUILayout.TextField("API URL", builder.apiUrl);
        builder.worldPositionAnchor = (WorldPositionAnchor)EditorGUILayout.ObjectField("World Position Anchor", builder.worldPositionAnchor, typeof(WorldPositionAnchor), true);
        builder.objectNamePrefix = EditorGUILayout.TextField("Object Name Prefix", builder.objectNamePrefix);

        // Add space before Mesh Type section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mesh Type", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(builder.createFullMesh);
        builder.createWireframe = EditorGUILayout.Toggle("Create Wireframe", builder.createWireframe);
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(builder.createWireframe);
        builder.createFullMesh = EditorGUILayout.Toggle("Create Full Mesh", builder.createFullMesh);
        EditorGUI.EndDisabledGroup();

        // Add space before Model Type section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Model Type", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(builder.create3DModel);
        builder.create2DModel = EditorGUILayout.Toggle("Create 2D Model", builder.create2DModel);
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(builder.create2DModel);
        builder.create3DModel = EditorGUILayout.Toggle("Create 3D Model", builder.create3DModel);
        EditorGUI.EndDisabledGroup();

        if (builder.create3DModel)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Height Options", EditorStyles.boldLabel);
            builder.useUniformHeight = EditorGUILayout.Toggle("Use Uniform Height", builder.useUniformHeight);
            if (builder.useUniformHeight)
            {
                builder.uniformHeight = EditorGUILayout.FloatField("Uniform Height", builder.uniformHeight);
            }
            else
            {
                builder.heightField = EditorGUILayout.TextField("Height Field", builder.heightField);
            }
        }

        // Only show the Visualization Options if either createWireframe or createFullMesh is selected
        if (builder.createWireframe || builder.createFullMesh)
        {
            // Add space before Visualization Options section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visualization Options", EditorStyles.boldLabel);

            if (builder.createWireframe)
            {
                builder.wireframeThickness = EditorGUILayout.FloatField("Wireframe Thickness", builder.wireframeThickness);

                // Multiple colors option
                builder.useMultipleColors = EditorGUILayout.Toggle("Use Multiple Colors", builder.useMultipleColors);
                if (builder.useMultipleColors)
                {
                    builder.colorCategoryField = EditorGUILayout.TextField("Color Category Field", builder.colorCategoryField);
                    EditorGUILayout.LabelField("Object Colors", EditorStyles.boldLabel);
                    SerializedProperty objectColors = serializedObject.FindProperty("objectColors");
                    EditorGUILayout.PropertyField(objectColors, new GUIContent("Object Colors"), true);
                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    builder.wireframeColor = EditorGUILayout.ColorField("Wireframe Color", builder.wireframeColor);
                }
            }

            if (builder.createFullMesh)
            {
                // Material options for Full Mesh
                builder.useMultipleMaterials = EditorGUILayout.Toggle("Use Multiple Materials", builder.useMultipleMaterials);

                if (builder.useMultipleMaterials)
                {
                    builder.objectCategoryField = EditorGUILayout.TextField("Object Category Field", builder.objectCategoryField);
                    EditorGUILayout.LabelField("Object Materials", EditorStyles.boldLabel);
                    SerializedProperty objectMaterials = serializedObject.FindProperty("objectMaterials");
                    EditorGUILayout.PropertyField(objectMaterials, new GUIContent("Object Materials"), true);
                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    builder.singleMaterial = (Material)EditorGUILayout.ObjectField("Single Material", builder.singleMaterial, typeof(Material), false);
                }
            }
        }

        // Create a horizontal layout for the buttons
        GUILayout.BeginHorizontal();

        // Button to clear all generated objects (left side)
        if (GUILayout.Button("Clear Objects"))
        {
            builder.ClearObjects();
            // Ensure the editor reflects changes immediately
            EditorUtility.SetDirty(builder);
        }

        // Button to generate objects (right side)
        if (GUILayout.Button("Generate Objects"))
        {
            // Note: Remove GenerateObjects() if you don't have this method anymore
            // If you use a different method to generate objects, replace this line accordingly
            builder.StartCoroutine(builder.FetchGeoJsonData());
            // Ensure the editor reflects changes immediately
            EditorUtility.SetDirty(builder);
        }

        GUILayout.EndHorizontal(); // End the horizontal layout

        if (GUI.changed)
        {
            EditorUtility.SetDirty(builder);
        }
    }
}
