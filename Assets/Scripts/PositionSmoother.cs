using UnityEngine;

public class PositionSmoother : MonoBehaviour
{
    public enum UpdateType
    {
        Update,
        LateUpdate
    }

    public enum SmoothType
    {
        Lerp,
        SmoothDamp
    }

    public Transform target;
    public float lerpSpeed = 20.0f;
    public float smoothDampTime = 0.02f;
    public bool extrapolatePosition;
    public UpdateType updateType;
    public SmoothType smoothType;

    private Transform cacheTransform;
    private Vector3 refVelocity;
    private Vector3 currentPosition;
    private Vector3 localPositionOffset;

    private void Awake()
    {
        cacheTransform = transform;
        currentPosition = cacheTransform.position;
        localPositionOffset = cacheTransform.localPosition;

        if (target == null)
            target = cacheTransform.parent;
    }

    private void OnEnable()
    {
        ResetCurrentPosition();
    }

    private void Update()
    {
        if (updateType == UpdateType.Update)
            SmoothPosition();
    }

    private void LateUpdate()
    {
        if (updateType == UpdateType.LateUpdate)
            SmoothPosition();
    }

    private void SmoothPosition()
    {
        currentPosition = Smooth(currentPosition, target.position, lerpSpeed);
        cacheTransform.position = currentPosition;
    }

    private Vector3 Smooth(Vector3 current, Vector3 target, float speed)
    {
        Vector3 offset = cacheTransform.localToWorldMatrix * localPositionOffset;

        if (extrapolatePosition)
            target += target - (current - offset);

        target += offset;

        return smoothType switch
        {
            SmoothType.Lerp => Vector3.Lerp(current, target, Time.deltaTime * speed),
            SmoothType.SmoothDamp => Vector3.SmoothDamp(current, target, ref refVelocity, smoothDampTime),
            _ => Vector3.zero
        };
    }

    public void ResetCurrentPosition()
    {
        Vector3 offset = cacheTransform.localToWorldMatrix * localPositionOffset;
        currentPosition = target.position + offset;
    }
}
