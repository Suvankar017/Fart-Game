using UnityEngine;

public class SidescrollerController2D : AdvancedWalkerController2D
{
    protected override Vector2 CalculateMovementDirection()
    {
        if (inputHandler == null)
            return Vector2.zero;

        Vector2 horizontalDirection = (cameraTransform == null) ? cachedTransform.right : VectorMath.ProjectOnEdge(cameraTransform.right, cachedTransform.up).normalized;
        float horizontalInput = inputHandler.GetHorizontalMovementInput();

        Vector2 direction = horizontalDirection * horizontalInput;

        if (currentState is ControllerState.Jumping)
        {
            Vector2 verticalDirection = cachedTransform.up;
            float verticalInput = inputHandler.GetVerticalMovementInput();

            direction += verticalDirection * verticalInput;
        }

        if (direction.sqrMagnitude > 1.0f)
            direction.Normalize();

        return direction;
    }
}
