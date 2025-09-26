using UnityEngine;

public abstract class CharacterInput : MonoBehaviour
{
    public abstract float GetHorizontalMovementInput();
    public abstract float GetVerticalMovementInput();
    public abstract bool IsJumpKeyPressed();
}
