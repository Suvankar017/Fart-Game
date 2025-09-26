using UnityEngine;

public class SidescrollerController : AdvancedWalkerController
{
    //Calculate movement direction based on player input;
    protected override Vector3 CalculateMovementDirection()
    {
        //If no character input script is attached to this object, return;
        if (characterInput == null)
            return Vector3.zero;

        Vector3 velocity = Vector3.zero;

        //If no camera transform has been assigned, use the character's 'right' transform axis to calculate the movement direction;
        if (cameraTransform == null)
        {
            velocity += tr.right * characterInput.GetHorizontalMovementInput();
        }
        else
        {
            //If a camera transform has been assigned, use the assigned transform's 'right' axis for movement direction;
            //Project movement direction so movement stays parallel to the ground;
            velocity += Vector3.ProjectOnPlane(cameraTransform.right, tr.up).normalized * characterInput.GetHorizontalMovementInput();
        }

        return velocity;
    }
}
