using UnityEngine;

public abstract class MoverColliderBase : MonoBehaviour
{
    [System.NonSerialized]
    public float colliderHeight;
    [System.NonSerialized]
    public float colliderThickness;
    [System.NonSerialized]
    public Vector2 colliderOffset;
    [System.NonSerialized]
    public float stepHeightRatio;

    public virtual Collider2D Collider { get; protected set; }

    public abstract void RecalculateDimensions();
}
