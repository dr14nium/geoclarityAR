using System;
using UnityEngine;

public class WorldPositionAnchor : MonoBehaviour
{
    [Tooltip("Reference Latitude for the coordinate system origin.")]
    public double referenceLat = -7.786170591154554;

    [Tooltip("Reference Longitude for the coordinate system origin.")]
    public double referenceLon = 110.41113960021086;

    private const double EarthRadius = 6378137.0;

    public Vector2d Coordinates
    {
        get
        {
            return WorldPositionToLatLon(new Vector2d(transform.position.x, transform.position.z));
        }
        set
        {
            var worldPosition = LatLonToWorldPosition(value.x, value.y);
            transform.position = new Vector3((float)worldPosition.x, transform.position.y, (float)worldPosition.y);
        }
    }

    public Vector2d LatLonToWorldPosition(double lat, double lon)
    {
        double latOffset = (lat - referenceLat) * Math.PI / 180 * EarthRadius;
        double lonOffset = (lon - referenceLon) * Math.PI / 180 * EarthRadius * Math.Cos(referenceLat * Math.PI / 180);

        return new Vector2d(lonOffset, latOffset);
    }

    public Vector2d WorldPositionToLatLon(Vector2d position)
    {
        double latOffset = position.y / EarthRadius * (180 / Math.PI);
        double lonOffset = position.x / (EarthRadius * Math.Cos(referenceLat * Math.PI / 180)) * (180 / Math.PI);

        return new Vector2d(referenceLat + latOffset, referenceLon + lonOffset);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 1);
        Gizmos.DrawIcon(transform.position, "BuildSettings.Web", true);
    }
}

public struct Vector2d
{
    public double x;
    public double y;

    public Vector2d(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2d operator +(Vector2d a, Vector2d b)
    {
        return new Vector2d(a.x + b.x, a.y + b.y);
    }

    public static Vector2d operator -(Vector2d a, Vector2d b)
    {
        return new Vector2d(a.x - b.x, a.y - b.y);
    }
}
