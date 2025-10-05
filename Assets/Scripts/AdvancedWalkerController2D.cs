using UnityEngine;

public class AdvancedWalkerController2D : CharacterController2D
{
    #region Constants
    private const float RISING_FALLING_THRESHOLD = 0.001f;
    private const float AIR_CONTROL_WEIGHT_MULTIPLIER = 0.25f;
    #endregion

    #region Enums
    /// <summary>
    /// Represents the different states the character controller can be in
    /// </summary>
    public enum ControllerState
    {
        Grounded,   // Character is on solid ground
        Sliding,    // Character is sliding down a steep slope
        Falling,    // Character is falling through the air
        Rising,     // Character is moving upward through the air
        Jumping     // Character is in the active jump state
    }
    #endregion

    #region Component References
    [Header("Component References")]
    [Tooltip("Optional camera transform used for calculating movement direction relative to camera view")]
    public Transform cameraTransform;

    // Cached component references
    protected Transform cachedTransform;
    protected Mover characterMover;
    protected CharacterInput inputHandler;
    protected CeilingDetector ceilingDetector;
    protected PlatformController platformController;
    #endregion

    #region Movement Configuration
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 7.0f;
    [SerializeField] private float airControlRate = 2.0f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpSpeed = 10.0f;
    [SerializeField] private float jumpDuration = 0.2f;

    [Header("Physics Settings")]
    [SerializeField] private float gravity = 30.0f;
    [SerializeField] private float airFriction = 0.5f;
    [SerializeField] private float groundFriction = 100.0f;

    [Header("Slope Settings")]
    [SerializeField] private float slopeLimit = 80.0f;
    [SerializeField, Tooltip("How fast the character will slide down steep slopes")]
    private float slideGravity = 5.0f;

    [Header("Advanced Settings")]
    [SerializeField, Tooltip("Whether to calculate and apply momentum relative to the controller's transform")]
    private bool useLocalMomentum = false;
    #endregion

    #region State Variables
    protected ControllerState currentState = ControllerState.Falling;
    private Vector2 momentum = Vector2.zero;
    private Vector2 previousFrameVelocity = Vector2.zero;
    private Vector2 previousFrameMovementVelocity = Vector2.zero;
    #endregion

    #region Jump Input Variables
    private class JumpInputState
    {
        public bool isLocked = false;
        public bool wasPressed = false;
        public bool wasReleased = false;
        public bool isCurrentlyPressed = false;
        public float jumpStartTime = 0.0f;

        public void Reset()
        {
            wasPressed = false;
            wasReleased = false;
        }
    }

    private readonly JumpInputState jumpInput = new();
    #endregion

    #region Configuration Properties
    public float MovementSpeed
    {
        get => movementSpeed;
        set => movementSpeed = Mathf.Max(0.0f, value);
    }

    public float AirControlRate
    {
        get => airControlRate;
        set => airControlRate = Mathf.Max(0.0f, value);
    }

    public float JumpSpeed
    {
        get => jumpSpeed;
        set => jumpSpeed = Mathf.Max(0.0f, value);
    }

    public float JumpDuration
    {
        get => jumpDuration;
        set => jumpDuration = Mathf.Max(0.0f, value);
    }

    public float Gravity
    {
        get => gravity;
        set => gravity = Mathf.Max(0.0f, value);
    }

    public float AirFriction
    {
        get => airFriction;
        set => airFriction = Mathf.Max(0.0f, value);
    }

    public float GroundFriction
    {
        get => groundFriction;
        set => groundFriction = Mathf.Max(0.0f, value);
    }

    public float SlopeLimit
    {
        get => slopeLimit;
        set => slopeLimit = Mathf.Clamp(value, 0.0f, 90.0f);
    }

    public float SlideGravity
    {
        get => slideGravity;
        set => slideGravity = Mathf.Max(0.0f, value);
    }

    public bool UseLocalMomentum
    {
        get => useLocalMomentum;
        set => useLocalMomentum = value;
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
        ValidateComponents();
        Setup();
    }

    private void Update() => ProcessJumpInput();

    private void FixedUpdate() => UpdateController();

    private bool isGroundState;
    private float switchTime;
    private float lastCameraSize;
    private float lastSmoothDampTime;
    private Camera characterCamera;
    private PositionSmoother positionSmoother;

    private void LateUpdate()
    {
        if (positionSmoother == null)
            positionSmoother = characterCamera.GetComponentInParent<PositionSmoother>();

        Debug.DrawRay(cachedTransform.position, GetMomentum(), Color.cyan);

        if (currentState is ControllerState.Grounded or ControllerState.Sliding)
        {
            if (!isGroundState)
            {
                isGroundState = true;
                switchTime = Time.time;
                lastCameraSize = Mathf.Clamp(characterCamera.orthographicSize, 5.0f, 7.0f);
                lastSmoothDampTime = positionSmoother.smoothDampTime;
            }

            float duration = (lastCameraSize - 5.0f) / 4.0f;
            float t = Mathf.Approximately(duration, 0.0f) ? 0.0f : Mathf.Clamp01((Time.time - switchTime) / duration);
            characterCamera.orthographicSize = Mathf.LerpUnclamped(lastCameraSize, 5.0f, t);

            //positionSmoother.smoothDampTime = Mathf.Lerp(positionSmoother.smoothDampTime, 0.5f, Time.deltaTime * 3.0f);

            duration = (0.5f - lastSmoothDampTime) / 0.2f;
            t = Mathf.Approximately(duration, 0.0f) ? 0.0f : Mathf.Clamp01((Time.time - switchTime) / duration);
            positionSmoother.smoothDampTime = Mathf.Lerp(lastSmoothDampTime, 0.5f, t);
        }
        else
        {
            if (isGroundState)
            {
                isGroundState = false;
                switchTime = Time.time;
                lastCameraSize = Mathf.Clamp(characterCamera.orthographicSize, 5.0f, 7.0f);
                lastSmoothDampTime = positionSmoother.smoothDampTime;
            }

            float duration = (7.0f - lastCameraSize) / 1.0f;
            float t = Mathf.Approximately(duration, 0.0f) ? 0.0f : Mathf.Clamp01((Time.time - switchTime) / duration);
            characterCamera.orthographicSize = Mathf.LerpUnclamped(lastCameraSize, 7.0f, t);

            //positionSmoother.smoothDampTime = Mathf.Lerp(positionSmoother.smoothDampTime, 0.04f, Time.deltaTime * 3.0f);

            duration = (lastSmoothDampTime - 0.04f) / 0.2f;
            t = Mathf.Approximately(duration, 0.0f) ? 0.0f : Mathf.Clamp01((Time.time - switchTime) / duration);
            positionSmoother.smoothDampTime = Mathf.Lerp(lastSmoothDampTime, 0.04f, t);
        }
    }
    #endregion

    #region Component Initialization
    private void InitializeComponents()
    {
        cachedTransform = transform;
        characterCamera = Camera.main;
        characterMover = GetComponent<Mover>();
        inputHandler = GetComponent<CharacterInput>();
        ceilingDetector = GetComponent<CeilingDetector>();
        platformController = GetComponent<PlatformController>();
    }

    private void ValidateComponents()
    {
        if (inputHandler == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No CharacterInput script attached. Controller will not respond to input.", gameObject);
        }
    }

    protected virtual void Setup() { }
    #endregion

    #region Input Processing
    private void ProcessJumpInput()
    {
        bool currentJumpPressed = IsJumpInputPressed();

        // Detect jump key press
        if (!jumpInput.isCurrentlyPressed && currentJumpPressed)
            jumpInput.wasPressed = true;

        // Detect jump key release
        if (jumpInput.isCurrentlyPressed && !currentJumpPressed)
        {
            jumpInput.wasReleased = true;
            jumpInput.isLocked = false;
        }

        jumpInput.isCurrentlyPressed = currentJumpPressed;
    }

    protected virtual bool IsJumpInputPressed() => (inputHandler != null) && inputHandler.IsJumpKeyPressed();
    #endregion

    #region Main Update Loop
    private void UpdateController()
    {
        // Update ground detection
        characterMover.CheckForGround();

        // Update controller state
        UpdateControllerState();

        // Apply physics to momentum
        ApplyPhysicsToMomentum();

        // Handle jump initiation
        ProcessJumpAttempt();

        // Calculate final velocity
        Vector2 finalVelocity = CalculateFinalVelocity();

        // Configure mover settings
        ConfigureMoverSettings();

        // Apply velocity to mover
        characterMover.SetVelocity(finalVelocity);

        // Store values for next frame
        CacheFrameData(finalVelocity);

        // Reset input flags
        ResetFrameFlags();
    }

    /// <summary>
    /// Calculate the final velocity to be applied to the mover
    /// </summary>
    private Vector2 CalculateFinalVelocity()
    {
        Vector2 velocity = Vector2.zero;

        // Add movement velocity only when grounded
        if (currentState == ControllerState.Grounded)
            velocity = CalculateMovementVelocity();

        // Add momentum (convert to world space if needed)
        Vector2 worldMomentum = ConvertMomentumToWorldSpace(momentum);
        velocity += worldMomentum;

        return velocity;
    }

    /// <summary>
    /// Configure mover settings based on current state
    /// </summary>
    private void ConfigureMoverSettings()
    {
        // Enable extended sensor range when grounded for better stair/slope handling
        characterMover.SetUseExtendedSensorRange(IsGrounded());
    }

    /// <summary>
    /// Cache data needed for next frame
    /// </summary>
    private void CacheFrameData(Vector2 velocity)
    {
        previousFrameVelocity = velocity;
        previousFrameMovementVelocity = CalculateMovementVelocity();
    }

    /// <summary>
    /// Reset flags that should only last one frame
    /// </summary>
    private void ResetFrameFlags()
    {
        jumpInput.Reset();

        if (ceilingDetector != null)
            ceilingDetector.ResetFlags();
    }
    #endregion

    #region Movement Calculation
    /// <summary>
    /// Calculate normalized movement direction based on input and camera orientation
    /// </summary>
    protected virtual Vector2 CalculateMovementDirection()
    {
        if (inputHandler == null)
            return Vector2.zero;

        Vector2 direction = (cameraTransform == null) ? cachedTransform.right : VectorMath.ProjectOnEdge(cameraTransform.right, cachedTransform.up).normalized;
        float horizontalInput = inputHandler.GetHorizontalMovementInput();

        return direction * horizontalInput;
    }

    /// <summary>
    /// Calculate movement velocity by applying speed to movement direction
    /// </summary>
    protected virtual Vector2 CalculateMovementVelocity()
    {
        Vector2 direction = CalculateMovementDirection();
        return direction * movementSpeed;
    }
    #endregion

    #region State Management
    /// <summary>
    /// Update the controller state based on current conditions
    /// </summary>
    private void UpdateControllerState() => currentState = DetermineNewControllerState();

    /// <summary>
    /// Determine what the new controller state should be based on current conditions
    /// </summary>
    private ControllerState DetermineNewControllerState()
    {
        bool isRising = IsMovingVertically() && IsMovingUpward();
        bool isOnSteepSlope = characterMover.IsGrounded() && IsGroundTooSteep();

        return currentState switch
        {
            ControllerState.Grounded => HandleGroundedState(isRising, isOnSteepSlope),
            ControllerState.Falling => HandleFallingState(isRising, isOnSteepSlope),
            ControllerState.Sliding => HandleSlidingState(isRising),
            ControllerState.Rising => HandleRisingState(isRising, isOnSteepSlope),
            ControllerState.Jumping => HandleJumpingState(),
            _ => ControllerState.Falling
        };
    }

    private ControllerState HandleGroundedState(bool isRising, bool isOnSteepSlope)
    {
        if (isRising)
        {
            OnGroundContactLost();
            return ControllerState.Rising;
        }

        if (!characterMover.IsGrounded())
        {
            OnGroundContactLost();
            return ControllerState.Falling;
        }

        if (isOnSteepSlope)
        {
            OnGroundContactLost();
            return ControllerState.Sliding;
        }

        return ControllerState.Grounded;
    }

    private ControllerState HandleFallingState(bool isRising, bool isOnSteepSlope)
    {
        if (isRising)
            return ControllerState.Rising;

        if (characterMover.IsGrounded())
        {
            if (isOnSteepSlope)
                return ControllerState.Sliding;

            OnGroundContactRegained();
            return ControllerState.Grounded;
        }

        return ControllerState.Falling;
    }

    private ControllerState HandleSlidingState(bool isRising)
    {
        if (isRising)
        {
            OnGroundContactLost();
            return ControllerState.Rising;
        }

        if (!characterMover.IsGrounded())
        {
            OnGroundContactLost();
            return ControllerState.Falling;
        }

        if (characterMover.IsGrounded() && !IsGroundTooSteep())
        {
            OnGroundContactRegained();
            return ControllerState.Grounded;
        }

        return ControllerState.Sliding;
    }

    private ControllerState HandleRisingState(bool isRising, bool isOnSteepSlope)
    {
        if (CheckCeilingCollision())
        {
            OnCeilingContact();
            return ControllerState.Falling;
        }

        if (!isRising)
        {
            if (characterMover.IsGrounded())
            {
                if (isOnSteepSlope)
                    return ControllerState.Sliding;

                OnGroundContactRegained();
                return ControllerState.Grounded;
            }

            return ControllerState.Falling;
        }

        return ControllerState.Rising;
    }

    private ControllerState HandleJumpingState()
    {
        // Check for jump timeout
        //if (Time.time - jumpInput.jumpStartTime > jumpDuration)
        //    return ControllerState.Rising;

        // Check if jump key was released
        if (jumpInput.wasReleased)
            return ControllerState.Rising;

        // Check for ceiling collision
        if (CheckCeilingCollision())
        {
            OnCeilingContact();
            return ControllerState.Falling;
        }

        return ControllerState.Jumping;
    }
    #endregion

    #region Jump Handling
    /// <summary>
    /// Check if player is trying to jump and handle jump initiation
    /// </summary>
    private void ProcessJumpAttempt()
    {
        if (currentState != ControllerState.Grounded || !(jumpInput.isCurrentlyPressed || jumpInput.wasPressed) || jumpInput.isLocked)
            return;

        InitiateJump();
    }

    /// <summary>
    /// Start a jump by changing state and applying jump force
    /// </summary>
    private void InitiateJump()
    {
        OnGroundContactLost();
        OnJumpStart();
        currentState = ControllerState.Jumping;
    }
    #endregion

    #region Physics and Momentum
    /// <summary>
    /// Apply physics forces and friction to momentum
    /// </summary>
    private void ApplyPhysicsToMomentum()
    {
        // Convert to world space for calculations if needed
        if (useLocalMomentum)
            momentum = cachedTransform.localToWorldMatrix * momentum;

        // Split momentum into components
        (Vector2 verticalMomentum, Vector2 horizontalMomentum) = SplitMomentumComponents();

        // Apply gravity
        verticalMomentum = ApplyGravity(verticalMomentum);

        // Handle air control and ground friction
        horizontalMomentum = ApplyHorizontalForces(horizontalMomentum);

        // Handle sliding physics
        if (currentState == ControllerState.Sliding)
        {
            (verticalMomentum, horizontalMomentum) = ApplySlidingPhysics(verticalMomentum, horizontalMomentum);
        }

        // Override momentum for jumping
        if (currentState == ControllerState.Jumping)
        {
            verticalMomentum = cachedTransform.up * jumpSpeed;
        }

        // Recombine momentum
        momentum = horizontalMomentum + verticalMomentum;

        // Convert back to local space if needed
        if (useLocalMomentum)
            momentum = cachedTransform.worldToLocalMatrix * momentum;
    }

    /// <summary>
    /// Split momentum into vertical and horizontal components
    /// </summary>
    private (Vector2 vertical, Vector2 horizontal) SplitMomentumComponents()
    {
        if (momentum == Vector2.zero)
        {
            return (Vector2.zero, Vector2.zero);
        }

        Vector2 vertical = VectorMath.ExtractDotVector(momentum, cachedTransform.up);
        Vector2 horizontal = momentum - vertical;
        return (vertical, horizontal);
    }

    /// <summary>
    /// Apply gravity to vertical momentum
    /// </summary>
    private Vector2 ApplyGravity(Vector2 verticalMomentum)
    {
        if (platformController != null)
            gravity = platformController.GetGravityAtHeight(cachedTransform.position);

        // Scale gravity based on height (simulating atmospheric thinning)
        verticalMomentum -= gravity * Time.fixedDeltaTime * (Vector2)cachedTransform.up;

        // Remove downward force when grounded
        if (currentState == ControllerState.Grounded && Vector2.Dot(verticalMomentum, cachedTransform.up) < 0.0f)
            verticalMomentum = Vector2.zero;

        return verticalMomentum;
    }

    /// <summary>
    /// Apply air control or ground friction to horizontal momentum
    /// </summary>
    private Vector2 ApplyHorizontalForces(Vector2 horizontalMomentum)
    {
        if (!IsGrounded())
        {
            horizontalMomentum = ApplyAirControl(horizontalMomentum);
        }
        else if (currentState == ControllerState.Sliding)
        {
            horizontalMomentum = ApplySlopeMovement(horizontalMomentum);
        }

        // Apply friction
        float frictionRate = currentState == ControllerState.Grounded ? groundFriction : airFriction;
        horizontalMomentum = VectorMath.IncrementVectorTowardsTargetVector(horizontalMomentum, Vector2.zero, frictionRate, Time.fixedDeltaTime);

        return horizontalMomentum;
    }

    /// <summary>
    /// Apply air control to horizontal momentum
    /// </summary>
    private Vector2 ApplyAirControl(Vector2 horizontalMomentum)
    {
        Vector2 movementVelocity = CalculateMovementVelocity();

        if (horizontalMomentum.magnitude > movementSpeed)
        {
            // Prevent speed accumulation in momentum direction
            if (Vector2.Dot(movementVelocity, horizontalMomentum.normalized) > 0.0f)
                movementVelocity = VectorMath.RemoveDotVector(movementVelocity, horizontalMomentum.normalized);

            horizontalMomentum += AIR_CONTROL_WEIGHT_MULTIPLIER * airControlRate * Time.fixedDeltaTime * movementVelocity;
        }
        else
        {
            horizontalMomentum += airControlRate * Time.fixedDeltaTime * movementVelocity;
            horizontalMomentum = Vector2.ClampMagnitude(horizontalMomentum, movementSpeed);
        }

        return horizontalMomentum;
    }

    /// <summary>
    /// Apply movement control while on slopes
    /// </summary>
    private Vector2 ApplySlopeMovement(Vector2 horizontalMomentum)
    {
        Vector2 slopeDownDirection = VectorMath.ProjectOnEdge(characterMover.GetGroundNormal(), cachedTransform.up).normalized;
        Vector2 slopeMovementVelocity = CalculateMovementVelocity();

        // Remove velocity pointing up the slope
        slopeMovementVelocity = VectorMath.RemoveDotVector(slopeMovementVelocity, slopeDownDirection);
        horizontalMomentum += slopeMovementVelocity * Time.fixedDeltaTime;

        return horizontalMomentum;
    }

    /// <summary>
    /// Apply sliding physics when on steep slopes
    /// </summary>
    private (Vector2 vertical, Vector2 horizontal) ApplySlidingPhysics(Vector2 verticalMomentum, Vector2 horizontalMomentum)
    {
        Vector2 combinedMomentum = horizontalMomentum + verticalMomentum;

        // Project momentum onto slope surface
        combinedMomentum = VectorMath.ProjectOnEdge(combinedMomentum, characterMover.GetGroundNormal());

        // Remove any upward momentum
        if (Vector2.Dot(combinedMomentum, cachedTransform.up) > 0.0f)
            combinedMomentum = VectorMath.RemoveDotVector(combinedMomentum, cachedTransform.up);

        // Apply slide gravity
        Vector2 slideDirection = VectorMath.ProjectOnEdge(-cachedTransform.up, characterMover.GetGroundNormal()).normalized;
        combinedMomentum += slideGravity * Time.fixedDeltaTime * slideDirection;

        // Split back into components
        Vector2 newVertical = VectorMath.ExtractDotVector(combinedMomentum, cachedTransform.up);
        Vector2 newHorizontal = combinedMomentum - newVertical;

        return (newVertical, newHorizontal);
    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// Called when a jump is initiated
    /// </summary>
    private void OnJumpStart()
    {
        // Convert to world space for calculations
        if (useLocalMomentum)
            momentum = cachedTransform.localToWorldMatrix * momentum;

        // Add jump force
        momentum += (Vector2)cachedTransform.up * jumpSpeed;

        // Set jump timing
        jumpInput.jumpStartTime = Time.time;
        jumpInput.isLocked = true;

        // Trigger event
        OnJump?.Invoke(momentum);

        // Convert back to local space
        if (useLocalMomentum)
            momentum = cachedTransform.worldToLocalMatrix * momentum;
    }

    /// <summary>
    /// Called when the controller loses ground contact
    /// </summary>
    private void OnGroundContactLost()
    {
        // Convert to world space
        if (useLocalMomentum)
            momentum = cachedTransform.localToWorldMatrix * momentum;

        Vector2 movementVelocity = GetMovementVelocity();

        // Prevent unwanted speed accumulation
        if (movementVelocity.sqrMagnitude >= 0.0f && momentum.sqrMagnitude > 0.0f)
        {
            Vector2 projectedMomentum = VectorMath.Project(momentum, movementVelocity.normalized);
            float alignment = Vector2.Dot(projectedMomentum.normalized, movementVelocity.normalized);

            if (projectedMomentum.sqrMagnitude >= movementVelocity.sqrMagnitude && alignment > 0.0f)
                movementVelocity = Vector2.zero;
            else if (alignment > 0.0f)
                movementVelocity -= projectedMomentum;
        }

        momentum += movementVelocity;

        // Convert back to local space
        if (useLocalMomentum)
            momentum = cachedTransform.worldToLocalMatrix * momentum;
    }

    /// <summary>
    /// Called when the controller regains ground contact
    /// </summary>
    private void OnGroundContactRegained()
    {
        Vector2 landingVelocity = momentum;

        if (useLocalMomentum)
            landingVelocity = cachedTransform.localToWorldMatrix * landingVelocity;

        OnLand?.Invoke(landingVelocity);
    }

    /// <summary>
    /// Called when the controller hits a ceiling
    /// </summary>
    private void OnCeilingContact()
    {
        if (useLocalMomentum)
            momentum = cachedTransform.localToWorldMatrix * momentum;

        // Remove vertical momentum
        momentum = VectorMath.RemoveDotVector(momentum, cachedTransform.up);

        if (useLocalMomentum)
            momentum = cachedTransform.worldToLocalMatrix * momentum;
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Check if the controller is moving vertically beyond the threshold
    /// </summary>
    private bool IsMovingVertically()
    {
        Vector2 verticalMomentum = VectorMath.ExtractDotVector(GetMomentum(), cachedTransform.up);
        return verticalMomentum.magnitude > RISING_FALLING_THRESHOLD;
    }

    /// <summary>
    /// Check if the controller is moving upward
    /// </summary>
    private bool IsMovingUpward() => Vector2.Dot(GetMomentum(), cachedTransform.up) > 0.0f;

    /// <summary>
    /// Check if the ground angle exceeds the slope limit
    /// </summary>
    private bool IsGroundTooSteep() => !characterMover.IsGrounded() || Vector2.Angle(characterMover.GetGroundNormal(), cachedTransform.up) > slopeLimit;

    /// <summary>
    /// Check if the controller hit a ceiling
    /// </summary>
    private bool CheckCeilingCollision() => ceilingDetector != null && ceilingDetector.HitCeiling();

    /// <summary>
    /// Convert momentum to world space if local momentum is used
    /// </summary>
    private Vector2 ConvertMomentumToWorldSpace(Vector2 localMomentum) => useLocalMomentum ? cachedTransform.localToWorldMatrix * localMomentum : localMomentum;
    #endregion

    #region Public Interface
    /// <summary>
    /// Get the current movement velocity (input-based movement only)
    /// </summary>
    public override Vector2 GetMovementVelocity() => previousFrameMovementVelocity;

    /// <summary>
    /// Get the total velocity including momentum
    /// </summary>
    public override Vector2 GetVelocity() => previousFrameVelocity;

    /// <summary>
    /// Check if the controller is currently grounded
    /// </summary>
    public override bool IsGrounded() => currentState is ControllerState.Grounded or ControllerState.Sliding;

    /// <summary>
    /// Get the current momentum in world space
    /// </summary>
    public Vector2 GetMomentum() => ConvertMomentumToWorldSpace(momentum);

    /// <summary>
    /// Check if the controller is currently sliding down a slope
    /// </summary>
    public bool IsSliding() => currentState is ControllerState.Sliding;

    /// <summary>
    /// Get the current controller state
    /// </summary>
    public ControllerState GetCurrentState() => currentState;

    /// <summary>
    /// Add momentum to the controller
    /// </summary>
    public void AddMomentum(Vector2 additionalMomentum)
    {
        if (useLocalMomentum)
            momentum = cachedTransform.localToWorldMatrix * momentum;

        momentum += additionalMomentum;

        if (useLocalMomentum)
            momentum = cachedTransform.worldToLocalMatrix * momentum;
    }

    /// <summary>
    /// Set the controller's momentum directly
    /// </summary>
    public void SetMomentum(Vector2 newMomentum) => momentum = useLocalMomentum ? cachedTransform.worldToLocalMatrix * newMomentum : newMomentum;

    /// <summary>
    /// Reset the controller's momentum to zero
    /// </summary>
    public void ResetMomentum() => momentum = Vector2.zero;
    #endregion

}
