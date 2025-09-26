using System.Collections.Generic;
using UnityEngine;

public abstract class SensorBase
{
    public enum CastDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public float castLength = 1.0f;
    public LayerMask layerMask = ~0;

    protected Transform transform;
    protected Vector2 origin;
    protected Vector2 hitNormal;
    protected Vector2 hitPosition;
    protected float hitDistance;
    protected CastDirection castDirection;
    protected bool hasDetectedHit;

    protected readonly HashSet<Collider2D> ignoreColliders;
    protected readonly List<Collider2D> hitColliders = new();
    protected readonly List<Transform> hitTransforms = new();

    public Vector2 Origin
    {
        get => origin;
        set => origin = transform.InverseTransformPoint(value);
    }

    public CastDirection Direction
    {
        get => castDirection;
        set => castDirection = value;
    }

    public bool HasDetectedHit => hasDetectedHit;

    public float HitDistance => hitDistance;

    public Vector2 HitPosition => hitPosition;

    public Vector2 HitNormal => hitNormal;

    public IReadOnlyList<Collider2D> HitColliders => hitColliders;

    public IReadOnlyList<Transform> HitTransforms => hitTransforms;

    public SensorBase(Transform transform, params Collider2D[] colliders)
    {
        this.transform = transform;
        hitPosition = Vector2.zero;
        hitNormal = Vector2.zero;
        hasDetectedHit = false;

        ignoreColliders = new HashSet<Collider2D>(colliders);
    }

    public abstract void DrawDebug();

    public void Cast()
    {
        ResetState();
        PerformCast();
    }

    public bool TryGetCollider(out Collider2D hitCollider)
    {
        hitCollider = null;

        if (hitColliders.Count > 0)
        {
            hitCollider = hitColliders[0];
            return true;
        }

        return false;
    }

    public bool TryGetTransform(out Transform hitTransform)
    {
        hitTransform = null;

        if (hitTransforms.Count > 0)
        {
            hitTransform = hitTransforms[0];
            return true;
        }

        return false;
    }

    public void AddColliderToIgnoreList(Collider2D collider)
    {
        if (collider != null && !ignoreColliders.Contains(collider))
            ignoreColliders.Add(collider);
    }

    public void RemoveColliderFromIgnoreList(Collider2D collider)
    {
        if (collider != null && ignoreColliders.Contains(collider))
            ignoreColliders.Remove(collider);
    }

    public void ClearIgnoreList() => ignoreColliders.Clear();

    protected abstract void PerformCast();

    protected Vector2 GetCastDirection()
    {
        return castDirection switch
        {
            CastDirection.Right => transform.right,
            CastDirection.Up => transform.up,
            CastDirection.Left => -transform.right,
            CastDirection.Down => -transform.up,
            _ => Vector2.zero,
        };
    }

    protected bool TryGetFirstValidHit(RaycastHit2D[] hits, out RaycastHit2D validHit)
    {
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && !ignoreColliders.Contains(hit.collider))
            {
                validHit = hit;
                return true;
            }
        }

        validHit = new RaycastHit2D();
        return false;
    }

    protected void SetHitData(RaycastHit2D hit)
    {
        hasDetectedHit = hit.collider != null;
        hitPosition = hit.point;
        hitNormal = hit.normal;
        hitDistance = hit.distance;

        if (hasDetectedHit)
        {
            hitColliders.Add(hit.collider);
            hitTransforms.Add(hit.transform);
        }
    }

    protected void ResetState()
    {
        hasDetectedHit = false;
        hitPosition = Vector2.zero;
        hitNormal = Vector2.zero;
        hitDistance = 0.0f;

        hitColliders.Clear();
        hitTransforms.Clear();
    }
}
