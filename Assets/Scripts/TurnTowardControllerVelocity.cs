using UnityEngine;

public class TurnTowardControllerVelocity : MonoBehaviour
{
    public CharacterController2D controller;
    public bool ignoreControllerMomentum;

    private Transform cacheTransform;
    private int currentDirection;

    private void Awake()
    {
        cacheTransform = transform;
        currentDirection = (cacheTransform.localScale.x >= 0.0f) ? 1 : -1;

        if (controller == null && cacheTransform.parent != null)
            controller = cacheTransform.parent.GetComponent<CharacterController2D>();

        if (controller == null)
        {
            Debug.LogWarning($"No controller script has been assigned to this {typeof(TurnTowardControllerVelocity).Name} component!", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        Vector2 velocity = ignoreControllerMomentum ? controller.GetMovementVelocity() : controller.GetVelocity();

        velocity = VectorMath.ProjectOnEdge(velocity, cacheTransform.up);

        const float magnitudeThreshold = 0.001f;

        if (velocity.magnitude < magnitudeThreshold)
            return;

        velocity.Normalize();

        int newDirection = (Vector2.Dot(velocity, cacheTransform.right) >= 0.0f) ? 1 : -1;

        if (newDirection != currentDirection)
        {
            currentDirection = newDirection;
            FlipSprite(currentDirection);
        }
    }

    private void FlipSprite(int direction)
    {
        Vector3 scale = cacheTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        cacheTransform.localScale = scale;
    }
}
