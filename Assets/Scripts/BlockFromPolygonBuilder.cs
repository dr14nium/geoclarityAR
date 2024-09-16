using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;

public class BlockFromPolygonBuilder : MonoBehaviour
{
    private List<Vector3> floorPolygon;
    private float height;
    private WorldPositionAnchor worldPositionAnchor;
    private bool createWireframe;
    private Color wireframeColor = Color.black;  // Warna wireframe default

    public float wireframeThickness = 0.1f;  // Ketebalan wireframe yang bisa diatur dari Inspector

    public void Initialize(GeoJSON.Net.Geometry.Polygon geometry, IDictionary<string, object> properties, WorldPositionAnchor anchor, string heightField, bool isWireframe, Color color, bool useUniformHeight, float uniformHeight)
    {
        floorPolygon = new List<Vector3>();
        worldPositionAnchor = anchor;
        createWireframe = isWireframe;
        wireframeColor = color;  // Setel warna wireframe dari parameter

        // Convert GeoJSON coordinates to Unity world coordinates using WorldPositionAnchor
        foreach (var coord in geometry.Coordinates[0].Coordinates)
        {
            Vector2d worldPos = worldPositionAnchor.LatLonToWorldPosition(coord.Latitude, coord.Longitude);
            floorPolygon.Add(new Vector3((float)worldPos.x, 0, (float)worldPos.y));
        }

        // Determine the height based on uniform height or height field
        if (useUniformHeight)
        {
            height = uniformHeight; // Use the uniform height provided
        }
        else if (!string.IsNullOrEmpty(heightField) && properties.ContainsKey(heightField) && properties[heightField] != null)
        {
            if (properties[heightField] is double)
            {
                height = (float)(double)properties[heightField];
            }
            else if (properties[heightField] is float)
            {
                height = (float)(float)properties[heightField];
            }
            else if (properties[heightField] is int)
            {
                height = (int)properties[heightField];
            }
            else if (properties[heightField] is long)
            {
                height = (float)(long)properties[heightField];
            }
            else
            {
                Debug.LogWarning($"Height field '{heightField}' is not a recognized numeric type. Using default height.");
                height = 10f; // Default height if the field is not found or invalid
            }
        }
        else
        {
            height = 0f; // No height for flat land parcels
        }
    }

    public void Draw(bool createFullMesh)
    {
        if (floorPolygon.Count < 3)
        {
            Debug.LogError("Not enough vertices to create a polygon.");
            return;
        }

        // Hanya tambahkan MeshRenderer dan MeshFilter jika createFullMesh == true
        if (createFullMesh)
        {
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            DrawFullMesh(meshFilter, meshRenderer);
        }

        if (createWireframe)
        {
            DrawWireframeMesh(null); // Tidak perlu MeshFilter untuk wireframe
        }
    }

    private void DrawWireframeMesh(MeshFilter meshFilter)
    {
        GameObject wireframeObject = new GameObject("WireframeTube");
        wireframeObject.transform.SetParent(this.transform, false); // Set parent to this object
        wireframeObject.transform.localPosition = Vector3.zero;
        wireframeObject.transform.localRotation = Quaternion.identity;
        wireframeObject.transform.localScale = Vector3.one;

        MeshRenderer meshRenderer = wireframeObject.AddComponent<MeshRenderer>();
        Mesh tubeMesh = new Mesh();

        // Adjust material and color
        Material tubeMaterial = new Material(Shader.Find("Unlit/Color"));
        tubeMaterial.color = wireframeColor; 
        meshRenderer.sharedMaterial = tubeMaterial;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        float radius = wireframeThickness / 2f; // Set radius from thickness
        int radialSegments = 8; // Number of subdivisions around the tube

        for (int i = 0; i < floorPolygon.Count; i++)
        {
            Vector3 start = floorPolygon[i];
            Vector3 end = floorPolygon[(i + 1) % floorPolygon.Count];
            
            CreateTube(vertices, triangles, start, end, radius, radialSegments);
        }

        // If height > 0, add vertical lines and roof
        if (height > 0)
        {
            for (int i = 0; i < floorPolygon.Count; i++)
            {
                Vector3 bottom = floorPolygon[i];
                Vector3 top = floorPolygon[i] + Vector3.up * height;
                Vector3 nextBottom = floorPolygon[(i + 1) % floorPolygon.Count];
                Vector3 nextTop = nextBottom + Vector3.up * height;

                // Vertical lines
                CreateTube(vertices, triangles, bottom, top, radius, radialSegments);
                // Roof lines
                CreateTube(vertices, triangles, top, nextTop, radius, radialSegments);
            }
        }

        tubeMesh.vertices = vertices.ToArray();
        tubeMesh.triangles = triangles.ToArray();

        tubeMesh.RecalculateNormals();
        tubeMesh.RecalculateBounds();

        MeshFilter tubeMeshFilter = wireframeObject.AddComponent<MeshFilter>();
        tubeMeshFilter.mesh = tubeMesh;
    }

    private void CreateTube(List<Vector3> vertices, List<int> triangles, Vector3 start, Vector3 end, float radius, int radialSegments)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 up = Vector3.up;
        if (Vector3.Dot(up, direction) > 0.99f)
        {
            up = Vector3.right;
        }
        Vector3 right = Vector3.Cross(direction, up).normalized;
        up = Vector3.Cross(right, direction).normalized;

        int baseIndex = vertices.Count;

        // Create the vertices around the start and end points
        for (int i = 0; i <= radialSegments; i++)
        {
            float angle = (float)i / radialSegments * Mathf.PI * 2f;
            Vector3 offset = right * Mathf.Cos(angle) * radius + up * Mathf.Sin(angle) * radius;

            vertices.Add(start + offset); // Bottom circle
            vertices.Add(end + offset); // Top circle
        }

        // Create triangles for the tube
        for (int i = 0; i < radialSegments; i++)
        {
            int startBottom = baseIndex + i * 2;
            int endBottom = baseIndex + (i + 1) * 2;
            int startTop = startBottom + 1;
            int endTop = endBottom + 1;

            // First triangle
            triangles.Add(startBottom);
            triangles.Add(endBottom);
            triangles.Add(startTop);

            // Second triangle
            triangles.Add(startTop);
            triangles.Add(endBottom);
            triangles.Add(endTop);
        }
    }

    private void DrawFullMesh(MeshFilter meshFilter, MeshRenderer meshRenderer)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>(floorPolygon);
        List<int> indices = new List<int>();

        // Triangulate the bottom face using Triangle.NET
        TriangulateFace(vertices, indices, 0, floorPolygon.Count, false);

        if (height > 0)
        {
            // Add vertices for the top polygon
            for (int i = 0; i < floorPolygon.Count; i++)
            {
                vertices.Add(floorPolygon[i] + Vector3.up * height);
            }

            // Triangulate the top face
            int topOffset = floorPolygon.Count;
            TriangulateFace(vertices, indices, topOffset, topOffset + floorPolygon.Count, true);

            // Create sides along the edges of the polygon
            for (int i = 0; i < floorPolygon.Count; i++)
            {
                int next = (i + 1) % floorPolygon.Count;

                // Create the side faces (two triangles per side)
                indices.Add(i);  // First triangle
                indices.Add(floorPolygon.Count + next);
                indices.Add(floorPolygon.Count + i);

                indices.Add(i);  // Second triangle
                indices.Add(next);
                indices.Add(floorPolygon.Count + next);
            }
        }

        // Create double-sided mesh
        int[] originalIndices = indices.ToArray();
        int vertexCount = vertices.Count;

        // Duplicate vertices for backface
        for (int i = 0; i < vertexCount; i++)
        {
            vertices.Add(vertices[i]);
        }

        // Duplicate and flip triangles for backface
        for (int i = 0; i < originalIndices.Length; i += 3)
        {
            indices.Add(originalIndices[i + 2] + vertexCount);
            indices.Add(originalIndices[i + 1] + vertexCount);
            indices.Add(originalIndices[i] + vertexCount);
        }

        // Set the mesh topology to triangles for full mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();

        // Recalculate normals to ensure correct shading
        mesh.RecalculateNormals();

        // Ensure the mesh is visually complete
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        // Set additional mesh renderer properties to disable backface culling
        if (meshRenderer != null)
        {
            Material material = meshRenderer.sharedMaterial;
            if (material != null)
            {
                meshRenderer.sharedMaterial = material;
                material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // Disable backface culling
                material.SetFloat("_Glossiness", 0f); // Ensure material is not overly shiny
            }
        }

        // Tambahkan MeshCollider di parent object yang sama
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;

        // Pastikan MeshCollider memiliki transformasi yang sinkron
        meshCollider.transform.localPosition = Vector3.zero;
        meshCollider.transform.localRotation = Quaternion.identity;
        meshCollider.transform.localScale = Vector3.one;
    }

    private void TriangulateFace(List<Vector3> vertices, List<int> indices, int startIndex, int endIndex, bool isTopFace)
    {
        // Create a new Polygon object for Triangle.NET
        var polygon = new Polygon();

        // Add vertices to the polygon
        for (int i = startIndex; i < endIndex; i++)
        {
            polygon.Add(new Vertex(vertices[i].x, vertices[i].z));
        }

        // Add constraint edges to ensure the triangulation respects the polygon boundary
        for (int i = 0; i < endIndex - startIndex; i++)
        {
            int next = (i + 1) % (endIndex - startIndex);
            polygon.Add(new Segment(polygon.Points[i], polygon.Points[next]));
        }

        // Triangulate the polygon
        var mesh = (TriangleNet.Mesh)polygon.Triangulate();

        // Convert Triangle.NET triangles back to Unity mesh
        foreach (var triangle in mesh.Triangles)
        {
            // Get the vertices of the triangle
            var vertex1 = triangle.GetVertex(0);
            var vertex2 = triangle.GetVertex(1);
            var vertex3 = triangle.GetVertex(2);

            // Convert Triangle.NET vertices to Unity Vector3
            Vector3 point1 = new Vector3((float)vertex1.X, vertices[startIndex].y, (float)vertex1.Y);
            Vector3 point2 = new Vector3((float)vertex2.X, vertices[startIndex].y, (float)vertex2.Y);
            Vector3 point3 = new Vector3((float)vertex3.X, vertices[startIndex].y, (float)vertex3.Y);

            // Ensure correct winding order for the face
            if (isTopFace)
            {
                if (Vector3.Cross(point2 - point1, point3 - point1).y < 0)
                {
                    Vector3 temp = point2;
                    point2 = point3;
                    point3 = temp;
                }
            }
            else
            {
                if (Vector3.Cross(point2 - point1, point3 - point1).y > 0)
                {
                    Vector3 temp = point2;
                    point2 = point3;
                    point3 = temp;
                }
            }

            // Add the vertices to the Unity mesh
            int index1 = AddVertex(vertices, point1);
            int index2 = AddVertex(vertices, point2);
            int index3 = AddVertex(vertices, point3);

            // Add the indices to form the triangle
            indices.Add(index1);
            indices.Add(index2);
            indices.Add(index3);
        }
    }

    private int AddVertex(List<Vector3> vertices, Vector3 point)
    {
        int index = vertices.IndexOf(point);
        if (index == -1)
        {
            vertices.Add(point);
            index = vertices.Count - 1;
        }
        return index;
    }
}