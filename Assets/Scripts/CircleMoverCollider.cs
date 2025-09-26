using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class CircleMoverCollider : MoverColliderBase
{
    private CircleCollider2D circleCollider;

    public CircleCollider2D CircleCollider => circleCollider;

    private void Awake()
    {
        circleCollider = TryGetComponent(out circleCollider) ? circleCollider : gameObject.AddComponent<CircleCollider2D>();
        Collider = circleCollider;
    }

    public override void RecalculateDimensions()
    {
        if (circleCollider == null)
            circleCollider = TryGetComponent(out circleCollider) ? circleCollider : gameObject.AddComponent<CircleCollider2D>();

        float radius = colliderHeight * 0.5f;
        Vector2 center = colliderOffset * colliderHeight;

        center += new Vector2(0.0f, stepHeightRatio * radius);
        radius *= 1.0f - stepHeightRatio;

        circleCollider.radius = radius;
        circleCollider.offset = center;
    }
}
