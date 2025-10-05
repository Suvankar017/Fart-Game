using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
{
    public float baseGravity = 30.0f;
    public float maxHeightGravity = 0.0f;
    public Transform maxHeightPoint;
    public float basePointOffset = 0.05f;

    private readonly List<Collider2D> colliders = new();

    private void Awake()
    {
        colliders.Clear();
        transform.GetComponentsInChildren(true, colliders);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            transform.GetComponentsInChildren(true, colliders);

        Vector3 center = transform.position;
        Vector3 scale = transform.localScale;
        
        if (colliders.Count > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Count; i++)
                bounds.Encapsulate(colliders[i].bounds);
            
            scale = bounds.size;
            center = bounds.center + new Vector3(0.0f, scale.y * 0.5f + basePointOffset);
        }

        Vector3 start = center - new Vector3(scale.x * 0.5f, 0.0f);
        Vector3 end = center + new Vector3(scale.x * 0.5f, 0.0f);
        Gizmos.DrawLine(start, end);

        if (maxHeightPoint != null)
        {
            Vector3 offset = maxHeightPoint.position - center;
            Gizmos.DrawLine(center, center + Vector3.up * offset.y);
        }
    }

    private float GetBaseHeight()
    {
        if (!TryGetBounds(out Bounds bounds))
            return bounds.center.y + basePointOffset;

        return bounds.center.y + bounds.extents.y + basePointOffset;
    }

    public float GetGravityAtHeight(float height)
    {
        if (maxHeightPoint == null || maxHeightGravity >= baseGravity)
            return baseGravity;

        float maxHeight = maxHeightPoint.position.y;
        float minHeight = GetBaseHeight();

        float t = (height - minHeight) / (maxHeight - minHeight);
        return Mathf.Lerp(baseGravity, maxHeightGravity, t);
    }

    public bool TryGetBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, transform.lossyScale);

        if (colliders.Count == 0)
            return false;

        bounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Count; i++)
            bounds.Encapsulate(colliders[i].bounds);

        return true;
    }

    public Vector2 ClosestBoundPoint(Vector2 point) => TryGetBounds(out Bounds bounds) ? bounds.ClosestPoint(point) : point;

    public Vector2 GetEdgeNormalOnBounds(Vector2 point)
    {
        if (!TryGetBounds(out Bounds bounds))
            return Vector2.zero;

        return GetBoundNormal(bounds, point);
    }

    private static Vector2 GetBoundNormal(Bounds bounds, Vector2 point)
    {
        // Step 1: Clamp point into the bounds -> closest point
        Vector2 closest = bounds.ClosestPoint(point);

        // Step 2: Get distances to each face
        float left = Mathf.Abs(closest.x - bounds.min.x);
        float right = Mathf.Abs(bounds.max.x - closest.x);
        float bottom = Mathf.Abs(closest.y - bounds.min.y);
        float top = Mathf.Abs(bounds.max.y - closest.y);

        // Step 3: Pick the smallest distance -> closest face normal
        float min = Mathf.Min(left, right, bottom, top);

        if (min == left) return Vector2.left;
        if (min == right) return Vector2.right;
        if (min == bottom) return Vector2.down;
        return Vector2.up;
    }
}
