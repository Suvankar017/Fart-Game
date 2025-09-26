using UnityEngine;

[RequireComponent(typeof(CapsuleCollider2D))]
public class CapsuleMoverCollider : MoverColliderBase
{
    private CapsuleCollider2D capsuleCollider;
    
    public CapsuleCollider2D CapsuleCollider => capsuleCollider;

    public override Collider2D Collider
    {
        get
        {
            if (capsuleCollider == null)
                capsuleCollider = TryGetComponent(out CapsuleCollider2D col) ? col : gameObject.AddComponent<CapsuleCollider2D>();
            return capsuleCollider;
        }

        protected set => base.Collider = value;
    }

    private void Awake()
    {
        capsuleCollider = TryGetComponent(out capsuleCollider) ? capsuleCollider : gameObject.AddComponent<CapsuleCollider2D>();
        Collider = capsuleCollider;
    }

    public override void RecalculateDimensions()
    {
        if (capsuleCollider == null)
            capsuleCollider = TryGetComponent(out capsuleCollider) ? capsuleCollider : gameObject.AddComponent<CapsuleCollider2D>();

        float height = colliderHeight;
        float radius = colliderThickness * 0.5f;
        Vector2 center = colliderOffset * colliderHeight;

        center += new Vector2(0.0f, stepHeightRatio * height * 0.5f);
        height *= 1.0f - stepHeightRatio;

        if (height * 0.5f < radius)
            radius = height * 0.5f;

        capsuleCollider.size = new Vector2(radius * 2.0f, height);
        capsuleCollider.offset = center;
    }
}
