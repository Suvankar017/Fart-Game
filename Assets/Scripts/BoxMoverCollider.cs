using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BoxMoverCollider : MoverColliderBase
{
    private BoxCollider2D boxCollider;

    public BoxCollider2D BoxCollider => boxCollider;

    private void Awake()
    {
        boxCollider = TryGetComponent(out boxCollider) ? boxCollider : gameObject.AddComponent<BoxCollider2D>();
        Collider = boxCollider;
    }

    public override void RecalculateDimensions()
    {
        if (boxCollider == null)
            boxCollider = TryGetComponent(out boxCollider) ? boxCollider : gameObject.AddComponent<BoxCollider2D>();

        float height = colliderHeight;
        float width = colliderThickness;
        Vector2 center = colliderOffset * colliderHeight;

        center += new Vector2(0.0f, stepHeightRatio * height * 0.5f);
        height *= 1.0f - stepHeightRatio;

        boxCollider.size = new Vector2(width, height);
        boxCollider.offset = center;
    }
}
