using UnityEngine;

public abstract class CharacterController2D : MonoBehaviour
{
    public delegate void VectorEvent(Vector2 vector);

    public VectorEvent OnJump;
    public VectorEvent OnLand;

    public abstract Vector2 GetVelocity();
    public abstract Vector2 GetMovementVelocity();
    public abstract bool IsGrounded();
}
