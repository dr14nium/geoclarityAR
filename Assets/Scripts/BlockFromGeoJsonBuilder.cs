using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using UnityEngine.Networking;

public class BlockFromGeoJsonBuilder : MonoBehaviour
{
    public string apiUrl = "http://localhost:3000/getGeoJson";
    public WorldPositionAnchor worldPositionAnchor;
    public string objectNamePrefix = "Object";

    public bool createWireframe;
    public bool createFullMesh;

    public float wireframeThickness = 0.1f;
    public Color wireframeColor = Color.black;

    public bool create2DModel;
    public bool create3DModel;

    public bool useUniformHeight;
    public float uniformHeight = 10f;
    public string heightField = "height";
    public string objectCategoryField = "category";
    public List<ObjectMaterial> objectMaterials = new List<ObjectMaterial>();

    public Material singleMaterial;
    public bool useMultipleMaterials;

    public bool useMultipleColors;
    public string colorCategoryField = "category";
    public List<ObjectColor> objectColors = new List<ObjectColor>();

    void Start()
    {
        StartCoroutine(FetchGeoJsonData());
    }
    
    public IEnumerator<object> FetchGeoJsonData()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch GeoJSON data: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            GenerateObjects(json);
        }
    }


    private void GenerateObjects(string geoJsonText)
    {
        ClearObjects();

        var geoJson = JsonConvert.DeserializeObject<FeatureCollection>(geoJsonText);
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

        // Tentukan warna wireframe atau material berdasarkan kategori jika multiple colors/materials digunakan
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
            useUniformHeight,   
            uniformHeight);
        blockBuilder.wireframeThickness = this.wireframeThickness; 
        blockBuilder.Draw(createFullMesh);

        // Material assignment dan penambahan MeshRenderer/MeshFilter hanya jika full mesh dibuat
        if (createFullMesh)
        {
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>() ?? obj.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>() ?? obj.AddComponent<MeshFilter>();

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

            // Buat mesh collider yang sesuai
            CreateIntegratedMeshCollider(obj);
            
            // Tambahkan FeatureAttributesHolder dan ObjectInteraction ke object parent
            var featureAttributes = ScriptableObject.CreateInstance<FeatureAttributes>();
            featureAttributes.Initialize(new Dictionary<string, object>(feature.Properties));

            var featureAttributesHolder = obj.AddComponent<FeatureAttributesHolder>();
            featureAttributesHolder.featureAttributes = featureAttributes;

            var objectInteraction = obj.AddComponent<ObjectInteraction>();
            objectInteraction.featureAttributes = featureAttributes;
        }
        else if (createWireframe)
        {
            // Jika wireframe dipilih, tambahkan collider mesh tanpa MeshRenderer atau MeshFilter
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

        // Ensure MeshCollider is only added once
        if (colliderObject.GetComponent<MeshCollider>() == null)
        {
            var meshCollider = colliderObject.AddComponent<MeshCollider>();
            meshCollider.convex = false;
        }

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
        // Ensure MeshCollider is only added once
        if (obj.GetComponent<MeshCollider>() == null)
        {
            var meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.convex = false;
        }
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