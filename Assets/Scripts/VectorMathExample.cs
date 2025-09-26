using UnityEngine;

public class VectorMathExample : MonoBehaviour
{
    public Transform vectorPointA;
    public Transform vectorPointB;
    public Transform normalPointA;
    public Transform normalPointB;

    private void OnDrawGizmos()
    {
        if (vectorPointA && vectorPointB && normalPointA && normalPointB)
        {
            Vector2 vectorA = vectorPointA.position;
            Vector2 vectorB = vectorPointB.position;
            Vector2 normalA = normalPointA.position;
            Vector2 normalB = normalPointB.position;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(vectorA, vectorB);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(normalA, normalB);

            Vector2 projectedVector = VectorMath.ProjectOnPlane(vectorB - vectorA, (normalB - normalA).normalized);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(vectorA, vectorA + projectedVector);
        }
    }
}
