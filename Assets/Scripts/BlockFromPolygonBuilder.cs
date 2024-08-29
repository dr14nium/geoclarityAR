using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;

public class BlockFromPolygonBuilder : MonoBehaviour
{
    private List<Vector3> floorPolygon;
    private float height;
    private WorldPositionAnchor worldPositionAnchor;
    private bool createWireframe;
    private Color wireframeColor = Color.black; // Default wireframe color

    public float wireframeThickness = 0.1f; // Adjustable wireframe thickness from Inspector

    public void Initialize(GeoJSON.Net.Geometry.Polygon geometry, IDictionary<string, object> properties, WorldPositionAnchor anchor, string heightField, bool isWireframe, Color color, bool useUniformHeight, float uniformHeight)
    {
        floorPolygon = new List<Vector3>();
        worldPositionAnchor = anchor;
        createWireframe = isWireframe;
        wireframeColor = color; // Set wireframe color from parameter

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

        // Create a new mesh
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (createWireframe)
        {
            DrawWireframeMesh(meshFilter);
        }
        else if (createFullMesh)
        {
            DrawFullMesh(meshFilter, meshRenderer);
        }
    }

    private void DrawWireframeMesh(MeshFilter meshFilter)
    {
        // Create a new GameObject for LineRenderer to handle all lines
        GameObject lineObject = new GameObject("WireframeLine");
        lineObject.transform.SetParent(this.transform);

        // Add LineRenderer component
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Set LineRenderer properties
        int totalPositions = floorPolygon.Count * 2 + (height > 0 ? floorPolygon.Count * 4 : 0);
        lineRenderer.positionCount = totalPositions;
        lineRenderer.startWidth = wireframeThickness;
        lineRenderer.endWidth = wireframeThickness;

        // Use sharedMaterial to prevent instantiation of a new material
        lineRenderer.sharedMaterial = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.sharedMaterial.color = wireframeColor; // Set color from parameter

        // Disable world space usage
        lineRenderer.useWorldSpace = false;

        int positionIndex = 0;

        // Add lines on the base
        for (int i = 0; i < floorPolygon.Count; i++)
        {
            int next = (i + 1) % floorPolygon.Count;
            lineRenderer.SetPosition(positionIndex++, floorPolygon[i]);
            lineRenderer.SetPosition(positionIndex++, floorPolygon[next]);
        }

        // Add vertical and roof lines if height > 0
        if (height > 0)
        {
            for (int i = 0; i < floorPolygon.Count; i++)
            {
                Vector3 bottomVertex = floorPolygon[i];
                Vector3 topVertex = floorPolygon[i] + Vector3.up * height;
                int next = (i + 1) % floorPolygon.Count;
                Vector3 nextTopVertex = floorPolygon[next] + Vector3.up * height;

                // Vertical lines
                lineRenderer.SetPosition(positionIndex++, bottomVertex);
                lineRenderer.SetPosition(positionIndex++, topVertex);

                // Roof lines
                lineRenderer.SetPosition(positionIndex++, topVertex);
                lineRenderer.SetPosition(positionIndex++, nextTopVertex);
            }
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
                indices.Add(i); // First triangle
                indices.Add(floorPolygon.Count + next);
                indices.Add(floorPolygon.Count + i);

                indices.Add(i); // Second triangle
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

        // Optional: Add mesh collider for the full mesh
        MeshCollider collider = gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = false;
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
