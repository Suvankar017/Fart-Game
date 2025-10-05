using UnityEngine;
using UnityEngine.UIElements;

public class CeilingDetector : MonoBehaviour
{
    public enum CeilingDetectionMethod
    {
        /// <summary>
        /// Only check the very first collision contact. This option is slightly faster but less accurate than the other two options.
        /// </summary>
        OnlyCheckFirstContact,
        /// <summary>
        /// Check all contact points and register a ceiling hit as long as just one contact qualifies.
        /// </summary>
        CheckAllContacts,
        /// <summary>
        /// Calculate an average surface normal to check against.
        /// </summary>
        CheckAverageOfAllContacts
    }

    public float ceilingAngleLimit = 10.0f;
    public CeilingDetectionMethod ceilingDetectionMethod;
    public bool isInDebugMode = false;
    
    private Transform cacheTransform;
    private bool ceilingWasHit = false;
    private readonly float debugDrawDuration = 2.0f;

    private void Awake() => cacheTransform = transform;

    private void OnCollisionEnter2D(Collision2D collision) => CheckCollisionAngles(collision);

    private void OnCollisionStay2D(Collision2D collision) => CheckCollisionAngles(collision);

    //Check if a given collision qualifies as a ceiling hit;
    private void CheckCollisionAngles(Collision2D collision)
    {
        switch (ceilingDetectionMethod)
        {
            case CeilingDetectionMethod.OnlyCheckFirstContact:
            {
                float angle = Vector2.Angle(-cacheTransform.up, collision.contacts[0].normal);

                if (angle < ceilingAngleLimit)
                    ceilingWasHit = true;

                if (isInDebugMode)
                    Debug.DrawRay(collision.contacts[0].point, collision.contacts[0].normal, Color.red, debugDrawDuration);

                break;
            }
            case CeilingDetectionMethod.CheckAllContacts:
            {
                for (int i = 0; i < collision.contacts.Length; i++)
                {
                    //Calculate angle between hit normal and character;
                    float angle = Vector2.Angle(-cacheTransform.up, collision.contacts[i].normal);

                    //If angle is smaller than ceiling angle limit, register ceiling hit;
                    if (angle < ceilingAngleLimit)
                    {
                        ceilingWasHit = true;
                        break;
                    }

                    if (isInDebugMode)
                        Debug.DrawRay(collision.contacts[i].point, collision.contacts[i].normal, Color.red, debugDrawDuration);
                }

                break;
            }
            case CeilingDetectionMethod.CheckAverageOfAllContacts:
            {
                float angle = 0.0f;

                for (int i = 0; i < collision.contacts.Length; i++)
                {
                    //Calculate angle between hit normal and character and add it to total angle count;
                    angle += Vector2.Angle(-cacheTransform.up, collision.contacts[i].normal);

                    if (isInDebugMode)
                        Debug.DrawRay(collision.contacts[i].point, collision.contacts[i].normal, Color.red, debugDrawDuration);
                }

                //If average angle is smaller than the ceiling angle limit, register ceiling hit;
                if ((angle / collision.contacts.Length) < ceilingAngleLimit)
                    ceilingWasHit = true;

                break;
            }
        }
    }

    public bool HitCeiling() => ceilingWasHit;

    public void ResetFlags() => ceilingWasHit = false;
}
