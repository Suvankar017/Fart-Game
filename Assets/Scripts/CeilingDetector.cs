using UnityEngine;

public class CeilingDetector : MonoBehaviour
{
    private bool ceilingWasHit = false;

    //Angle limit for ceiling hits;
    public float ceilingAngleLimit = 10f;

    //Ceiling detection methods;
    //'OnlyCheckFirstContact' - Only check the very first collision contact. This option is slightly faster but less accurate than the other two options.
    //'CheckAllContacts' - Check all contact points and register a ceiling hit as long as just one contact qualifies.
    //'CheckAverageOfAllContacts' - Calculate an average surface normal to check against.
    public enum CeilingDetectionMethod
    {
        OnlyCheckFirstContact,
        CheckAllContacts,
        CheckAverageOfAllContacts
    }

    public CeilingDetectionMethod ceilingDetectionMethod;

    //If enabled, draw debug information to show hit positions and hit normals;
    public bool isInDebugMode = false;
    //How long debug information is drawn on the screen;
    private float debugDrawDuration = 2.0f;

    private Transform tr;

    private void Awake()
    {
        tr = transform;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckCollisionAngles(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckCollisionAngles(collision);
    }

    //Check if a given collision qualifies as a ceiling hit;
    private void CheckCollisionAngles(Collision2D collision)
    {
        float angle = 0f;

        if (ceilingDetectionMethod == CeilingDetectionMethod.OnlyCheckFirstContact)
        {
            //Calculate angle between hit normal and character;
            angle = Vector3.Angle(-tr.up, collision.contacts[0].normal);

            //If angle is smaller than ceiling angle limit, register ceiling hit;
            if (angle < ceilingAngleLimit)
                ceilingWasHit = true;

            //Draw debug information;
            if (isInDebugMode)
                Debug.DrawRay(collision.contacts[0].point, collision.contacts[0].normal, Color.red, debugDrawDuration);
        }

        if (ceilingDetectionMethod == CeilingDetectionMethod.CheckAllContacts)
        {
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                //Calculate angle between hit normal and character;
                angle = Vector3.Angle(-tr.up, collision.contacts[i].normal);

                //If angle is smaller than ceiling angle limit, register ceiling hit;
                if (angle < ceilingAngleLimit)
                    ceilingWasHit = true;

                //Draw debug information;
                if (isInDebugMode)
                    Debug.DrawRay(collision.contacts[i].point, collision.contacts[i].normal, Color.red, debugDrawDuration);
            }
        }

        if (ceilingDetectionMethod == CeilingDetectionMethod.CheckAverageOfAllContacts)
        {
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                //Calculate angle between hit normal and character and add it to total angle count;
                angle += Vector3.Angle(-tr.up, collision.contacts[i].normal);

                //Draw debug information;
                if (isInDebugMode)
                    Debug.DrawRay(collision.contacts[i].point, collision.contacts[i].normal, Color.red, debugDrawDuration);
            }

            //If average angle is smaller than the ceiling angle limit, register ceiling hit;
            if (angle / ((float)collision.contacts.Length) < ceilingAngleLimit)
                ceilingWasHit = true;
        }
    }

    //Return whether ceiling was hit during the last frame;
    public bool HitCeiling()
    {
        return ceilingWasHit;
    }

    //Reset ceiling hit flags;
    public void ResetFlags()
    {
        ceilingWasHit = false;
    }
}
