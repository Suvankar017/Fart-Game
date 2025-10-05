using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class PlatformController : MonoBehaviour
{
    public float defaultGravity = 30.0f;
    public Transform platformContainer;
    public Transform maxHeightPoint;

    private Platform[] platforms;

    private void Awake()
    {
        if (platformContainer == null)
            platforms = FindObjectsByType<Platform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        else
            platforms = platformContainer.GetComponentsInChildren<Platform>();

        if (platforms != null && maxHeightPoint != null)
        {
            foreach (Platform platform in platforms)
                platform.maxHeightPoint = maxHeightPoint;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Transform cacheTransform = transform;
        Vector2 currentPosition = cacheTransform.position;

        float distance = float.MaxValue;
        Platform closestPlatform = null;
        Vector2 closestPoint = Vector2.zero;

        foreach (Platform platform in platforms)
        {
            Vector2 cp = platform.ClosestBoundPoint(currentPosition);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cp, 0.1f);

            Vector2 normal = platform.GetEdgeNormalOnBounds(currentPosition);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(cp, cp + normal);

            if (Vector2.Dot(Vector2.up, normal) > 0.9f)
            {
                float d = Vector2.Distance(currentPosition, cp);
                if (d < distance)
                {
                    distance = d;
                    closestPlatform = platform;
                    closestPoint = cp;
                }
            }
            else
            {
                Vector2 dir = (currentPosition - cp).normalized;
                if (Vector2.Dot(dir, Vector2.up) > 0.0f)
                {
                    float d = Vector2.Distance(currentPosition, cp);
                    if (d < distance)
                    {
                        distance = d;
                        closestPlatform = platform;
                        closestPoint = cp;
                    }
                }
            }
        }

        if (closestPlatform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(closestPoint, 0.2f);

            float gravity = closestPlatform.GetGravityAtHeight(currentPosition.y);
            Vector2 gravityVector = Vector2.down * gravity;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(currentPosition, currentPosition + gravityVector);

            UnityEditor.Handles.Label(currentPosition + Vector2.up * 0.2f, $"Gravity: {gravity:F2}");
        }
    }

    public float GetGravityAtHeight(Vector2 point)
    {
        Platform closestPlatform = null;
        Vector2 closestPoint = Vector2.zero;
        float closestdistance = float.MaxValue;

        foreach (Platform platform in platforms)
        {
            Vector2 closestBoundPoint = platform.ClosestBoundPoint(point);
            Vector2 boundNormal = platform.GetEdgeNormalOnBounds(point);

            if (Vector2.Dot(Vector2.up, boundNormal) > 0.9f)
            {
                float distance = Vector2.Distance(point, closestBoundPoint);
                if (distance < closestdistance)
                {
                    closestdistance = distance;
                    closestPlatform = platform;
                    closestPoint = closestBoundPoint;
                }
            }
            else
            {
                Vector2 closestPointToPointDir = (point - closestBoundPoint).normalized;
                if (Vector2.Dot(closestPointToPointDir, Vector2.up) > 0.0f)
                {
                    float distance = Vector2.Distance(point, closestBoundPoint);
                    if (distance < closestdistance)
                    {
                        closestdistance = distance;
                        closestPlatform = platform;
                        closestPoint = closestBoundPoint;
                    }
                }
            }
        }

        if (closestPlatform == null)
            return defaultGravity;

        return closestPlatform.GetGravityAtHeight(point.y);
    }
}
