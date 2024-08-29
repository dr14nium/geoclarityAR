using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;

public class BlockFromGeoJsonBuilder : MonoBehaviour
{
    public TextAsset geoJsonFile;
    public WorldPositionAnchor worldPositionAnchor;
    public string objectNamePrefix = "Object";

    public bool createWireframe;
    public bool createFullMesh;

    public float wireframeThickness = 0.1f; // Adjustable wireframe thickness from Inspector
    public Color wireframeColor = Color.black; // Default wireframe color

    public bool create2DModel;
    public bool create3DModel;

    public bool useUniformHeight; // New setting to choose whether to use uniform height
    public float uniformHeight = 10f; // Default uniform height value
    public string heightField = "height";
    public string objectCategoryField = "category";
    public List<ObjectMaterial> objectMaterials = new List<ObjectMaterial>();

    public Material singleMaterial;
    public bool useMultipleMaterials;

    // Properties for wireframe colors
    public bool useMultipleColors;
    public string colorCategoryField = "category";
    public List<ObjectColor> objectColors = new List<ObjectColor>();

    public void GenerateObjects()
    {
        ClearObjects();

        if (geoJsonFile == null)
        {
            Debug.LogError("GeoJSON file not set.");
            return;
        }

        if (worldPositionAnchor == null)
        {
            Debug.LogError("WorldPositionAnchor not set.");
            return;
        }

        if (!create2DModel && !create3DModel)
        {
            Debug.LogError("No model option selected (2D or 3D).");
            return;
        }

        if (!createWireframe && !createFullMesh)
        {
            Debug.LogError("No mesh option selected (wireframe or full mesh).");
            return;
        }

        var geoJson = JsonConvert.DeserializeObject<FeatureCollection>(geoJsonFile.text);
        if (geoJson == null || geoJson.Features == null)
        {
            Debug.LogError("Failed to parse GeoJSON or no features found.");
            return;
        }

        var features = geoJson.Features.ToList();
        int totalCreated = 0;

        foreach (var feature in features)
        {
            if (feature.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.Polygon)
            {
                Polygon polygon = feature.Geometry as Polygon;
                CreateObject(polygon, feature, totalCreated, 0);
                totalCreated++;
            }
            else if (feature.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.MultiPolygon)
            {
                MultiPolygon multiPolygon = feature.Geometry as MultiPolygon;
                for (int j = 0; j < multiPolygon.Coordinates.Count; j++)
                {
                    CreateObject(multiPolygon.Coordinates[j], feature, totalCreated, j);
                    totalCreated++;
                }
            }
        }

        Debug.Log($"{totalCreated} objects generated successfully.");
    }

    private void CreateObject(Polygon geometry, Feature feature, int featureIndex, int polygonIndex)
    {
        string objectName = $"{objectNamePrefix}_{featureIndex}_{polygonIndex}";
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(this.transform, false);

        // Determine the wireframe color or material based on the category if multiple colors/materials are used
        Color wireframeColorToUse = wireframeColor; // Default wireframe color
        if (createWireframe && useMultipleColors)
        {
            string categoryValue = feature.Properties.ContainsKey(colorCategoryField) ? feature.Properties[colorCategoryField].ToString() : null;
            if (!string.IsNullOrEmpty(categoryValue))
            {
                var matchingColor = objectColors.FirstOrDefault(c => c.objectCategory == categoryValue)?.color;
                if (matchingColor.HasValue)
                {
                    wireframeColorToUse = matchingColor.Value;
                    Debug.Log($"Assigned color for {objectName} with category: {categoryValue}");
                }
                else
                {
                    Debug.LogWarning($"No matching color found for category: {categoryValue}. Object: {objectName} will use default color.");
                }
            }
        }

        var blockBuilder = obj.AddComponent<BlockFromPolygonBuilder>();
        blockBuilder.Initialize(
            geometry, 
            feature.Properties, 
            worldPositionAnchor, 
            create3DModel ? heightField : "", 
            createWireframe, 
            wireframeColorToUse, 
            useUniformHeight,   // Added this parameter
            uniformHeight);     // Added this parameter
        blockBuilder.wireframeThickness = this.wireframeThickness; // Set wireframe thickness
        blockBuilder.Draw(createFullMesh);

        // Material assignment only if full mesh is created
        if (createFullMesh)
        {
            var meshRenderer = obj.GetComponent<MeshRenderer>() ?? obj.AddComponent<MeshRenderer>();

            if (useMultipleMaterials)
            {
                string categoryValue = feature.Properties.ContainsKey(objectCategoryField) ? feature.Properties[objectCategoryField].ToString() : null;
                if (!string.IsNullOrEmpty(categoryValue))
                {
                    var matchingMaterial = objectMaterials.FirstOrDefault(m => m.objectCategory == categoryValue)?.material;
                    if (matchingMaterial != null)
                    {
                        meshRenderer.material = matchingMaterial;
                        Debug.Log($"Assigned material for {objectName} with category: {categoryValue}");
                    }
                    else
                    {
                        Debug.LogWarning($"No matching material found for category: {categoryValue}. Object: {objectName} will have no material assigned.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Category field is missing or empty for feature {featureIndex}. Object: {objectName} will have no material assigned.");
                }
            }
            else if (singleMaterial != null)
            {
                meshRenderer.material = singleMaterial;
                Debug.Log($"Assigned single material for {objectName}");
            }
            else
            {
                Debug.LogWarning($"No material assigned for {objectName}");
            }

            // Create the appropriate mesh collider
            CreateIntegratedMeshCollider(obj);
            
            // Add FeatureAttributesHolder and ObjectInteraction to the parent object
            var featureAttributes = ScriptableObject.CreateInstance<FeatureAttributes>();
            featureAttributes.Initialize(new Dictionary<string, object>(feature.Properties));

            var featureAttributesHolder = obj.AddComponent<FeatureAttributesHolder>();
            featureAttributesHolder.featureAttributes = featureAttributes;

            var objectInteraction = obj.AddComponent<ObjectInteraction>();
            objectInteraction.featureAttributes = featureAttributes;
        }
        else if (createWireframe)
        {
            // If wireframe is selected, FeatureAttributesHolder and ObjectInteraction are added only to ColliderMesh
            CreateSeparateMeshCollider(obj, geometry, feature.Properties, create3DModel ? heightField : "");
        }

        Debug.Log($"Created object: {objectName}");
    }

    private void CreateSeparateMeshCollider(GameObject parentObject, Polygon geometry, IDictionary<string, object> properties, string heightField)
    {
        // Create a child object for the collider
        GameObject colliderObject = new GameObject("ColliderMesh");
        colliderObject.transform.SetParent(parentObject.transform, false);

        // Initialize BlockFromPolygonBuilder on the child object
        var blockBuilder = colliderObject.AddComponent<BlockFromPolygonBuilder>();
        blockBuilder.Initialize(
            geometry, 
            properties, 
            worldPositionAnchor, 
            heightField, 
            false, 
            Color.clear, 
            useUniformHeight,   // Added this parameter
            uniformHeight);     // Added this parameter
        blockBuilder.Draw(true); // Create full mesh for collider

        // Add MeshCollider to the child object
        var meshCollider = colliderObject.AddComponent<MeshCollider>();
        meshCollider.convex = false;

        // Disable MeshRenderer on the child object so that the collider is invisible
        MeshRenderer renderer = colliderObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false; // Collider is invisible
        }

        // Add FeatureAttributesHolder and ObjectInteraction to the child object
        var featureAttributes = ScriptableObject.CreateInstance<FeatureAttributes>();
        featureAttributes.Initialize(new Dictionary<string, object>(properties));

        var featureAttributesHolder = colliderObject.AddComponent<FeatureAttributesHolder>();
        featureAttributesHolder.featureAttributes = featureAttributes;

        var objectInteraction = colliderObject.AddComponent<ObjectInteraction>();
        objectInteraction.featureAttributes = featureAttributes;

        Debug.Log($"ColliderMesh created and components added on: {colliderObject.name}");
    }

    private void CreateIntegratedMeshCollider(GameObject obj)
    {
        var meshCollider = obj.AddComponent<MeshCollider>();
        meshCollider.convex = false;
    }

    public void ClearObjects()
    {
        List<GameObject> children = new List<GameObject>();

        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }

        foreach (GameObject child in children)
        {
            DestroyImmediate(child);
        }

        Debug.Log("All generated objects cleared.");
    }
}

[System.Serializable]
public class ObjectMaterial
{
    public string objectCategory;
    public Material material;
}

[System.Serializable]
public class ObjectColor
{
    public string objectCategory;
    public Color color;
}
