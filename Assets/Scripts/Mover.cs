using UnityEngine;

[RequireComponent(typeof(MoverColliderBase))]
public class Mover : MonoBehaviour
{
    [Header("Mover Options")]
    [Range(0.0f, 1.0f)]
    public float stepHeightRatio = 0.25f;
    public bool useStepHeight = false;
    public float stepHeight = 0.25f;

    [Header("Collider Options")]
    public float colliderHeight = 2.0f;
    public float colliderThickness = 1.0f;
    public Vector2 colliderOffset = new(0.0f, 0.5f);

    [Header("Sensor Options")]
    public bool isInDebugMode = true;

    private SensorBase sensor;
    private new Transform transform;
    private new Rigidbody2D rigidbody;
    private MoverColliderBase moverCollider;
    private Vector2 currentGroundAdjustmentVelocity = Vector2.zero;
    private int currentLayer = -1;
    private float baseSensorRange = 0.0f;
    private bool isUsingExtendedSensorRange = true;
    private bool isGrounded = false;

    void OnValidate()
    {
        if (gameObject.activeInHierarchy)
            RecalculateColliderDimensions();
    }

    private void Reset()
    {
        Setup();
    }

    private void Awake()
    {
        Setup();

        Collider2D collider = TryGetComponent(out Collider2D col) ? col : gameObject.AddComponent<CapsuleCollider2D>();
        sensor = new RaycastSensor(transform, collider);

        RecalculateColliderDimensions();
    }

    private void LateUpdate()
    {
        if (isInDebugMode)
            sensor.DrawDebug();
    }

    #region Public Methods

    public void CheckForGround()
    {
        if (currentLayer != gameObject.layer)
            RecalculateSensorLayerMask();

        Check();
    }

    public void SetVelocity(Vector2 velocity) => rigidbody.linearVelocity = velocity + currentGroundAdjustmentVelocity;

    public bool IsGrounded() => isGrounded;

    public void SetUseExtendedSensorRange(bool useExtendedRange) => isUsingExtendedSensorRange = useExtendedRange;

    public void SetColliderHeight(float height)
    {
        if (Mathf.Approximately(colliderHeight, height))
            return;

        colliderHeight = Mathf.Max(0.0f, height);
        RecalculateColliderDimensions();
    }

    public void SetColliderThickness(float thickness)
    {
        if (Mathf.Approximately(colliderThickness, thickness))
            return;

        colliderThickness = Mathf.Max(0.0f, thickness);
        RecalculateColliderDimensions();
    }

    public void SetStepHeightRatio(float stepHeightRatio)
    {
        this.stepHeightRatio = Mathf.Clamp01(stepHeightRatio);
        RecalculateColliderDimensions();
    }

    public Vector2 GetGroundNormal() => sensor.HitNormal;

    public Vector2 GetGroundPoint() => sensor.HitPosition;

    public Collider2D GetGroundCollider() => sensor.TryGetCollider(out Collider2D collider) ? collider : null;

    #endregion

    #region Private Methods

    private void Setup()
    {
        transform = GetComponent<Transform>();
        rigidbody = TryGetComponent(out rigidbody) ? rigidbody : gameObject.AddComponent<Rigidbody2D>();
        moverCollider = TryGetComponent(out moverCollider) ? moverCollider : gameObject.AddComponent<CapsuleMoverCollider>();

        rigidbody.freezeRotation = true;
        rigidbody.gravityScale = 0.0f;
    }

    private void RecalculateColliderDimensions()
    {
        if (moverCollider == null)
            Setup();

        UpdateColliderProperties();
        moverCollider.RecalculateDimensions();

        if (sensor != null)
            RecalibrateSensor();
    }

    private void UpdateColliderProperties()
    {
        if (useStepHeight)
        {
            stepHeight = Mathf.Clamp(stepHeight, 0.0f, colliderHeight);
            stepHeightRatio = stepHeight / colliderHeight;
        }
        else
        {
            stepHeightRatio = Mathf.Clamp01(stepHeightRatio);
            stepHeight = colliderHeight * stepHeightRatio;
        }

        moverCollider.colliderHeight = colliderHeight;
        moverCollider.colliderThickness = colliderThickness;
        moverCollider.colliderOffset = colliderOffset;
        moverCollider.stepHeightRatio = stepHeightRatio;
    }

    private void RecalibrateSensor()
    {
        sensor.Origin = GetColliderCenter();
        sensor.Direction = SensorBase.CastDirection.Down;

        RecalculateSensorLayerMask();

        const float safetyDistanceFactor = 0.001f;

        float length = colliderHeight * ((1.0f - stepHeightRatio) * 0.5f + stepHeightRatio);
        baseSensorRange = length * (1.0f + safetyDistanceFactor) * transform.localScale.x;
        sensor.castLength = length * transform.localScale.x;
    }

    private Vector3 GetColliderCenter() => moverCollider.Collider != null ? moverCollider.Collider.bounds.center : transform.position;

    private void RecalculateSensorLayerMask()
    {
        int layerMask = 0;
        int objectLayer = gameObject.layer;

        for (int i = 0; i < 32; i++)
        {
            if (!Physics.GetIgnoreLayerCollision(objectLayer, i))
                layerMask |= 1 << i;
        }

        int layerIndex = LayerMask.NameToLayer("Ignore Raycast");
        if (layerMask == (layerMask | (1 << layerIndex)))
        {
            layerMask ^= 1 << layerIndex;
        }

        sensor.layerMask = layerMask;
        currentLayer = objectLayer;
    }

    private void Check()
    {
        currentGroundAdjustmentVelocity = Vector2.zero;

        float extendedLength = isUsingExtendedSensorRange ? colliderHeight * transform.localScale.x * stepHeightRatio : 0.0f;
        sensor.castLength = baseSensorRange + extendedLength;

        sensor.Cast();

        if (!sensor.HasDetectedHit)
        {
            isGrounded = false;
            return;
        }

        isGrounded = true;

        float distance = sensor.HitDistance;
        float upperLimit = colliderHeight * transform.localScale.x * (1.0f - stepHeightRatio) * 0.5f;
        float middle = upperLimit + colliderHeight * transform.localScale.x * stepHeightRatio;
        float distanceToGo = middle - distance;

        currentGroundAdjustmentVelocity = transform.up * (distanceToGo / Time.fixedDeltaTime);
    }

    #endregion
}
