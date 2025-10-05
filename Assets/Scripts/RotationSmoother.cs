using UnityEngine;

public class RotationSmoother : MonoBehaviour
{
    public enum UpdateType
    {
        Update,
        LateUpdate
    }

    public Transform target;
    public float smoothSpeed = 20.0f;
    public bool extrapolateRotation;
    public UpdateType updateType;

    private Transform cacheTransform;
    private Quaternion currentRotation;

    private void Awake()
    {
        cacheTransform = transform;
        currentRotation = cacheTransform.rotation;

        if (target == null)
            target = cacheTransform.parent;
    }

    private void OnEnable()
    {
        ResetCurrentRotation();
    }

    private void Update()
    {
        if (updateType == UpdateType.Update)
            SmoothRotation();
    }

    private void LateUpdate()
    {
        if (updateType == UpdateType.LateUpdate)
            SmoothRotation();
    }

    private void SmoothRotation()
    {
        currentRotation = Smooth(currentRotation, target.rotation, smoothSpeed);
        cacheTransform.rotation = currentRotation;
    }

    private Quaternion Smooth(Quaternion current, Quaternion target, float speed)
    {
        if (extrapolateRotation && Quaternion.Angle(current, target) < 90.0f)
        {
            Quaternion diff = target * Quaternion.Inverse(current);
            target *= diff;
        }

        return Quaternion.Slerp(current, target, speed * Time.deltaTime);
    }

    public void ResetCurrentRotation()
    {
        currentRotation = target.rotation;
    }
}