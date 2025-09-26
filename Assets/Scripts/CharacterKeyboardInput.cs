using UnityEngine;

public class CharacterKeyboardInput : CharacterInput
{
    public string horizontalInputAxis = "Horizontal";
    public string verticalInputAxis = "Vertical";
    public KeyCode jumpKey = KeyCode.Space;

    //If this is enabled, Unity's internal input smoothing is bypassed;
    public bool useRawInput = true;

    public override float GetHorizontalMovementInput()
    {
        if (useRawInput)
            return Input.GetAxisRaw(horizontalInputAxis);
        else
            return Input.GetAxis(horizontalInputAxis);
    }

    public override float GetVerticalMovementInput()
    {
        if (useRawInput)
            return Input.GetAxisRaw(verticalInputAxis);
        else
            return Input.GetAxis(verticalInputAxis);
    }

    public override bool IsJumpKeyPressed()
    {
        return Input.GetKey(jumpKey);
    }
}
