using UnityEngine;

public class RaycastSensor : SensorBase
{
    public RaycastSensor(Transform transform, params Collider2D[] colliders) : base(transform, colliders)
    {

    }

    public override void DrawDebug()
    {
        Vector2 worldOrigin = transform.TransformPoint(origin);
        Vector2 worldDirection = GetCastDirection();

        Debug.DrawRay(worldOrigin, worldDirection * castLength, Color.magenta);

        if (hasDetectedHit)
        {
            const float markerSize = 0.05f;
            Debug.DrawRay(hitPosition, hitNormal, Color.green);
            Debug.DrawLine(hitPosition + Vector2.up * markerSize, hitPosition - Vector2.up * markerSize, Color.green);
            Debug.DrawLine(hitPosition + Vector2.right * markerSize, hitPosition - Vector2.right * markerSize, Color.green);
        }
    }

    protected override void PerformCast()
    {
        Vector2 originWS = transform.TransformPoint(origin);
        Vector2 directionWS = GetCastDirection();

        RaycastHit2D[] hits = Physics2D.RaycastAll(originWS, directionWS, castLength, layerMask);

        if (hits.Length == 0)
            return;

        System.Array.Sort(hits, (hitA, hitB) => hitA.distance.CompareTo(hitB.distance));

        if (TryGetFirstValidHit(hits, out RaycastHit2D validHit))
            SetHitData(validHit);
    }
}
