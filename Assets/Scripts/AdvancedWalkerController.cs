using UnityEngine;

[RequireComponent(typeof(CharacterInput))]
public class AdvancedWalkerController : CharacterController2D
{
    //References to attached components;
    protected Transform tr;
    protected Mover mover;
    protected CharacterInput characterInput;
    protected CeilingDetector ceilingDetector;

    //Jump key variables;
    private bool jumpInputIsLocked = false;
    private bool jumpKeyWasPressed = false;
    private bool jumpKeyWasLetGo = false;
    private bool jumpKeyIsPressed = false;

    //Movement speed;
    public float movementSpeed = 7f;

    //How fast the controller can change direction while in the air;
    //Higher values result in more air control;
    public float airControlRate = 2f;

    //Jump speed;
    public float jumpSpeed = 10f;

    //Jump duration variables;
    public float jumpDuration = 0.2f;
    private float currentJumpStartTime = 0f;

    //'AirFriction' determines how fast the controller loses its momentum while in the air;
    //'GroundFriction' is used instead, if the controller is grounded;
    public float airFriction = 0.5f;
    public float groundFriction = 100f;

    //Current momentum;
    protected Vector3 momentum = Vector3.zero;

    //Saved velocity from last frame;
    private Vector3 savedVelocity = Vector3.zero;

    //Saved horizontal movement velocity from last frame;
    private Vector3 savedMovementVelocity = Vector3.zero;

    //Amount of downward gravity;
    public float gravity = 30f;
    [Tooltip("How fast the character will slide down steep slopes.")]
    public float slideGravity = 5f;

    //Acceptable slope angle limit;
    public float slopeLimit = 80f;

    [Tooltip("Whether to calculate and apply momentum relative to the controller's transform.")]
    public bool useLocalMomentum = false;

    //Enum describing basic controller states; 
    public enum ControllerState
    {
        Grounded,
        Sliding,
        Falling,
        Rising,
        Jumping
    }

    private ControllerState currentControllerState = ControllerState.Falling;

    [Tooltip("Optional camera transform used for calculating movement direction. If assigned, character movement will take camera view into account.")]
    public Transform cameraTransform;

    private void Awake()
    {
        mover = GetComponent<Mover>();
        tr = transform;
        characterInput = GetComponent<CharacterInput>();
        ceilingDetector = GetComponent<CeilingDetector>();

        if (characterInput == null)
            Debug.LogWarning("No character input script has been attached to this gameobject", gameObject);

        Setup();
    }

    protected virtual void Setup() { }

    private void Update()
    {
        HandleJumpKeyInput();
    }

    //Handle jump booleans for later use in FixedUpdate;
    private void HandleJumpKeyInput()
    {
        bool newJumpKeyPressedState = IsJumpKeyPressed();

        if (!jumpKeyIsPressed && newJumpKeyPressedState)
            jumpKeyWasPressed = true;

        if (jumpKeyIsPressed && !newJumpKeyPressedState)
        {
            jumpKeyWasLetGo = true;
            jumpInputIsLocked = false;
        }

        jumpKeyIsPressed = newJumpKeyPressedState;
    }

    void FixedUpdate()
    {
        ControllerUpdate();
    }

    //Update controller;
    //This function must be called every fixed update, in order for the controller to work correctly;
    private void ControllerUpdate()
    {
        //Check if mover is grounded;
        mover.CheckForGround();

        //Determine controller state;
        currentControllerState = DetermineControllerState();

        //Apply friction and gravity to 'momentum';
        HandleMomentum();

        //Check if the player has initiated a jump;
        HandleJumping();

        //Calculate movement velocity;
        Vector3 velocity = Vector3.zero;
        if (currentControllerState == ControllerState.Grounded)
            velocity = CalculateMovementVelocity();

        //If local momentum is used, transform momentum into world space first;
        Vector3 worldMomentum = momentum;
        if (useLocalMomentum)
            worldMomentum = tr.localToWorldMatrix * momentum;

        //Add current momentum to velocity;
        velocity += worldMomentum;

        //If player is grounded or sliding on a slope, extend mover's sensor range;
        //This enables the player to walk up/down stairs and slopes without losing ground contact;
        mover.SetUseExtendedSensorRange(IsGrounded());

        //Set mover velocity;		
        mover.SetVelocity(velocity);

        //Store velocity for next frame;
        savedVelocity = velocity;

        //Save controller movement velocity;
        savedMovementVelocity = CalculateMovementVelocity();

        //Reset jump key booleans;
        jumpKeyWasLetGo = false;
        jumpKeyWasPressed = false;

        //Reset ceiling detector, if one is attached to this gameobject;
        if (ceilingDetector != null)
            ceilingDetector.ResetFlags();
    }

    //Calculate and return movement direction based on player input;
    //This function can be overridden by inheriting scripts to implement different player controls;
    protected virtual Vector3 CalculateMovementDirection()
    {
        //If no character input script is attached to this object, return;
        if (characterInput == null)
            return Vector3.zero;

        Vector3 velocity = Vector3.zero;

        //If no camera transform has been assigned, use the character's transform axes to calculate the movement direction;
        if (cameraTransform == null)
        {
            velocity += tr.right * characterInput.GetHorizontalMovementInput();
            velocity += tr.forward * characterInput.GetVerticalMovementInput();
        }
        else
        {
            //If a camera transform has been assigned, use the assigned transform's axes for movement direction;
            //Project movement direction so movement stays parallel to the ground;
            velocity += Vector3.ProjectOnPlane(cameraTransform.right, tr.up).normalized * characterInput.GetHorizontalMovementInput();
            velocity += Vector3.ProjectOnPlane(cameraTransform.forward, tr.up).normalized * characterInput.GetVerticalMovementInput();
        }

        //If necessary, clamp movement vector to magnitude of 1f;
        if (velocity.magnitude > 1f)
            velocity.Normalize();

        return velocity;
    }

    //Calculate and return movement velocity based on player input, controller state, ground normal [...];
    protected virtual Vector3 CalculateMovementVelocity()
    {
        //Calculate (normalized) movement direction;
        Vector3 velocity = CalculateMovementDirection();

        //Multiply (normalized) velocity with movement speed;
        velocity *= movementSpeed;

        return velocity;
    }

    //Returns 'true' if the player presses the jump key;
    protected virtual bool IsJumpKeyPressed()
    {
        //If no character input script is attached to this object, return;
        if (characterInput == null)
            return false;

        return characterInput.IsJumpKeyPressed();
    }


    //Determine current controller state based on current momentum and whether the controller is grounded (or not);
    //Handle state transitions;
    private ControllerState DetermineControllerState()
    {
        //Check if vertical momentum is pointing upwards;
        bool isRising = IsRisingOrFalling() && (Vector3.Dot(GetMomentum(), tr.up) > 0f);
        //Check if controller is sliding;
        bool isSliding = mover.IsGrounded() && IsGroundTooSteep();

        //Grounded;
        if (currentControllerState == ControllerState.Grounded)
        {
            if (isRising)
            {
                OnGroundContactLost();
                return ControllerState.Rising;
            }
            if (!mover.IsGrounded())
            {
                OnGroundContactLost();
                return ControllerState.Falling;
            }
            if (isSliding)
            {
                OnGroundContactLost();
                return ControllerState.Sliding;
            }
            return ControllerState.Grounded;
        }

        //Falling;
        if (currentControllerState == ControllerState.Falling)
        {
            if (isRising)
            {
                return ControllerState.Rising;
            }
            if (mover.IsGrounded() && !isSliding)
            {
                OnGroundContactRegained();
                return ControllerState.Grounded;
            }
            if (isSliding)
            {
                return ControllerState.Sliding;
            }
            return ControllerState.Falling;
        }

        //Sliding;
        if (currentControllerState == ControllerState.Sliding)
        {
            if (isRising)
            {
                OnGroundContactLost();
                return ControllerState.Rising;
            }
            if (!mover.IsGrounded())
            {
                OnGroundContactLost();
                return ControllerState.Falling;
            }
            if (mover.IsGrounded() && !isSliding)
            {
                OnGroundContactRegained();
                return ControllerState.Grounded;
            }
            return ControllerState.Sliding;
        }

        //Rising;
        if (currentControllerState == ControllerState.Rising)
        {
            if (!isRising)
            {
                if (mover.IsGrounded() && !isSliding)
                {
                    OnGroundContactRegained();
                    return ControllerState.Grounded;
                }
                if (isSliding)
                {
                    return ControllerState.Sliding;
                }
                if (!mover.IsGrounded())
                {
                    return ControllerState.Falling;
                }
            }

            //If a ceiling detector has been attached to this gameobject, check for ceiling hits;
            if (ceilingDetector != null)
            {
                if (ceilingDetector.HitCeiling())
                {
                    OnCeilingContact();
                    return ControllerState.Falling;
                }
            }
            return ControllerState.Rising;
        }

        //Jumping;
        if (currentControllerState == ControllerState.Jumping)
        {
            //Check for jump timeout;
            if ((Time.time - currentJumpStartTime) > jumpDuration)
                return ControllerState.Rising;

            //Check if jump key was let go;
            if (jumpKeyWasLetGo)
                return ControllerState.Rising;

            //If a ceiling detector has been attached to this gameobject, check for ceiling hits;
            if (ceilingDetector != null)
            {
                if (ceilingDetector.HitCeiling())
                {
                    OnCeilingContact();
                    return ControllerState.Falling;
                }
            }
            return ControllerState.Jumping;
        }

        return ControllerState.Falling;
    }


    //Check if player has initiated a jump;
    private void HandleJumping()
    {
        if (currentControllerState == ControllerState.Grounded)
        {
            if ((jumpKeyIsPressed == true || jumpKeyWasPressed) && !jumpInputIsLocked)
            {
                //Call events;
                OnGroundContactLost();
                OnJumpStart();

                currentControllerState = ControllerState.Jumping;
            }
        }
    }


    //Apply friction to both vertical and horizontal momentum based on 'friction' and 'gravity';
    //Handle movement in the air;
    //Handle sliding down steep slopes;
    private void HandleMomentum()
    {
        //If local momentum is used, transform momentum into world coordinates first;
        if (useLocalMomentum)
            momentum = tr.localToWorldMatrix * momentum;

        Vector3 verticalMomentum = Vector3.zero;
        Vector3 horizontalMomentum = Vector3.zero;

        //Split momentum into vertical and horizontal components;
        if (momentum != Vector3.zero)
        {
            verticalMomentum = VectorMath.ExtractDotVector(momentum, tr.up);
            horizontalMomentum = momentum - verticalMomentum;
        }

        //Add gravity to vertical momentum;
        verticalMomentum -= tr.up * gravity * Time.deltaTime;

        //Remove any downward force if the controller is grounded;
        if (currentControllerState == ControllerState.Grounded && Vector3.Dot(verticalMomentum, tr.up) < 0f)
            verticalMomentum = Vector3.zero;

        //Manipulate momentum to steer controller in the air (if controller is not grounded or sliding);
        if (!IsGrounded())
        {
            Vector3 movementVelocity = CalculateMovementVelocity();

            //If controller has received additional momentum from somewhere else;
            if (horizontalMomentum.magnitude > movementSpeed)
            {
                //Prevent unwanted accumulation of speed in the direction of the current momentum;
                if (Vector3.Dot(movementVelocity, horizontalMomentum.normalized) > 0f)
                    movementVelocity = VectorMath.RemoveDotVector(movementVelocity, horizontalMomentum.normalized);

                //Lower air control slightly with a multiplier to add some 'weight' to any momentum applied to the controller;
                float _airControlMultiplier = 0.25f;
                horizontalMomentum += movementVelocity * Time.deltaTime * airControlRate * _airControlMultiplier;
            }
            //If controller has not received additional momentum;
            else
            {
                //Clamp _horizontal velocity to prevent accumulation of speed;
                horizontalMomentum += movementVelocity * Time.deltaTime * airControlRate;
                horizontalMomentum = Vector3.ClampMagnitude(horizontalMomentum, movementSpeed);
            }
        }

        //Steer controller on slopes;
        if (currentControllerState == ControllerState.Sliding)
        {
            //Calculate vector pointing away from slope;
            Vector3 pointDownVector = Vector3.ProjectOnPlane(mover.GetGroundNormal(), tr.up).normalized;

            //Calculate movement velocity;
            Vector3 slopeMovementVelocity = CalculateMovementVelocity();
            //Remove all velocity that is pointing up the slope;
            slopeMovementVelocity = VectorMath.RemoveDotVector(slopeMovementVelocity, pointDownVector);

            //Add movement velocity to momentum;
            horizontalMomentum += slopeMovementVelocity * Time.fixedDeltaTime;
        }

        //Apply friction to horizontal momentum based on whether the controller is grounded;
        if (currentControllerState == ControllerState.Grounded)
            horizontalMomentum = VectorMath.IncrementVectorTowardsTargetVector(horizontalMomentum, Vector3.zero, groundFriction, Time.deltaTime);
        else
            horizontalMomentum = VectorMath.IncrementVectorTowardsTargetVector(horizontalMomentum, Vector3.zero, airFriction, Time.deltaTime);

        //Add horizontal and vertical momentum back together;
        momentum = horizontalMomentum + verticalMomentum;

        //Additional momentum calculations for sliding;
        if (currentControllerState == ControllerState.Sliding)
        {
            //Project the current momentum onto the current ground normal if the controller is sliding down a slope;
            momentum = Vector3.ProjectOnPlane(momentum, mover.GetGroundNormal());

            //Remove any upwards momentum when sliding;
            if (Vector3.Dot(momentum, tr.up) > 0f)
                momentum = VectorMath.RemoveDotVector(momentum, tr.up);

            //Apply additional slide gravity;
            Vector3 slideDirection = Vector3.ProjectOnPlane(-tr.up, mover.GetGroundNormal()).normalized;
            momentum += slideDirection * slideGravity * Time.deltaTime;
        }

        //If controller is jumping, override vertical velocity with jumpSpeed;
        if (currentControllerState == ControllerState.Jumping)
        {
            momentum = VectorMath.RemoveDotVector(momentum, tr.up);
            momentum += tr.up * jumpSpeed;
        }

        if (useLocalMomentum)
            momentum = tr.worldToLocalMatrix * momentum;
    }


    //Events;

    //This function is called when the player has initiated a jump;
    private void OnJumpStart()
    {
        //If local momentum is used, transform momentum into world coordinates first;
        if (useLocalMomentum)
            momentum = tr.localToWorldMatrix * momentum;

        //Add jump force to momentum;
        momentum += tr.up * jumpSpeed;

        //Set jump start time;
        currentJumpStartTime = Time.time;

        //Lock jump input until jump key is released again;
        jumpInputIsLocked = true;

        //Call event;
        if (OnJump != null)
            OnJump(momentum);

        if (useLocalMomentum)
            momentum = tr.worldToLocalMatrix * momentum;
    }

    //This function is called when the controller has lost ground contact, i.e. is either falling or rising, or generally in the air;
    private void OnGroundContactLost()
    {
        //If local momentum is used, transform momentum into world coordinates first;
        if (useLocalMomentum)
            momentum = tr.localToWorldMatrix * momentum;

        //Get current movement velocity;
        Vector3 velocity = GetMovementVelocity();

        //Check if the controller has both momentum and a current movement velocity;
        if (velocity.sqrMagnitude >= 0f && momentum.sqrMagnitude > 0f)
        {
            //Project momentum onto movement direction;
            Vector3 projectedMomentum = Vector3.Project(momentum, velocity.normalized);
            //Calculate dot product to determine whether momentum and movement are aligned;
            float dot = Vector3.Dot(projectedMomentum.normalized, velocity.normalized);

            //If current momentum is already pointing in the same direction as movement velocity,
            //Don't add further momentum (or limit movement velocity) to prevent unwanted speed accumulation;
            if (projectedMomentum.sqrMagnitude >= velocity.sqrMagnitude && dot > 0f)
                velocity = Vector3.zero;
            else if (dot > 0f)
                velocity -= projectedMomentum;
        }

        //Add movement velocity to momentum;
        momentum += velocity;

        if (useLocalMomentum)
            momentum = tr.worldToLocalMatrix * momentum;
    }

    //This function is called when the controller has landed on a surface after being in the air;
    private void OnGroundContactRegained()
    {
        //Call 'OnLand' event;
        if (OnLand != null)
        {
            Vector3 collisionVelocity = momentum;
            //If local momentum is used, transform momentum into world coordinates first;
            if (useLocalMomentum)
                collisionVelocity = tr.localToWorldMatrix * collisionVelocity;

            OnLand(collisionVelocity);
        }

    }

    //This function is called when the controller has collided with a ceiling while jumping or moving upwards;
    private void OnCeilingContact()
    {
        //If local momentum is used, transform momentum into world coordinates first;
        if (useLocalMomentum)
            momentum = tr.localToWorldMatrix * momentum;

        //Remove all vertical parts of momentum;
        momentum = VectorMath.RemoveDotVector(momentum, tr.up);

        if (useLocalMomentum)
            momentum = tr.worldToLocalMatrix * momentum;
    }

    //Helper functions;

    //Returns 'true' if vertical momentum is above a small threshold;
    private bool IsRisingOrFalling()
    {
        //Calculate current vertical momentum;
        Vector3 verticalMomentum = VectorMath.ExtractDotVector(GetMomentum(), tr.up);

        //Setup threshold to check against;
        //For most applications, a value of '0.001f' is recommended;
        const float limit = 0.001f;

        //Return true if vertical momentum is above '_limit';
        return (verticalMomentum.magnitude > limit);
    }

    //Returns true if angle between controller and ground normal is too big (> slope limit), i.e. ground is too steep;
    private bool IsGroundTooSteep()
    {
        if (!mover.IsGrounded())
            return true;

        return Vector3.Angle(mover.GetGroundNormal(), tr.up) > slopeLimit;
    }


    public override Vector2 GetMovementVelocity()
    {
        return savedMovementVelocity;
    }

    public override Vector2 GetVelocity()
    {
        return savedVelocity;
    }

    public override bool IsGrounded()
    {
        return currentControllerState == ControllerState.Grounded || currentControllerState == ControllerState.Sliding;
    }

    //Get current momentum;
    public Vector3 GetMomentum()
    {
        Vector3 worldMomentum = momentum;
        if (useLocalMomentum)
            worldMomentum = tr.localToWorldMatrix * momentum;

        return worldMomentum;
    }

    //Returns 'true' if controller is sliding;
    public bool IsSliding()
    {
        return currentControllerState == ControllerState.Sliding;
    }

    //Add momentum to controller;
    public void AddMomentum(Vector3 momentum)
    {
        if (useLocalMomentum)
            this.momentum = tr.localToWorldMatrix * this.momentum;

        this.momentum += momentum;

        if (useLocalMomentum)
            this.momentum = tr.worldToLocalMatrix * this.momentum;
    }

    //Set controller momentum directly;
    public void SetMomentum(Vector3 newMomentum)
    {
        if (useLocalMomentum)
            momentum = tr.worldToLocalMatrix * newMomentum;
        else
            momentum = newMomentum;
    }
}

/// <summary>
/// Advanced 2D character controller with state-based movement, momentum system, and jump mechanics.
/// Handles grounded movement, air control, jumping, and sliding on steep slopes.
/// </summary>
//[RequireComponent(typeof(CharacterInput))]
//public class AWC : CharacterController2D
//{
//    #region Constants
//    private const float RISING_FALLING_THRESHOLD = 0.001f;
//    private const float AIR_CONTROL_WEIGHT_MULTIPLIER = 0.25f;
//    #endregion

//    #region Enums
//    /// <summary>
//    /// Represents the different states the character controller can be in
//    /// </summary>
//    public enum ControllerState
//    {
//        Grounded,   // Character is on solid ground
//        Sliding,    // Character is sliding down a steep slope
//        Falling,    // Character is falling through the air
//        Rising,     // Character is moving upward through the air
//        Jumping     // Character is in the active jump state
//    }
//    #endregion

//    #region Component References
//    [Header("Component References")]
//    [Tooltip("Optional camera transform used for calculating movement direction relative to camera view")]
//    public Transform cameraTransform;

//    // Cached component references
//    private Transform cachedTransform;
//    private Mover characterMover;
//    private CharacterInput inputHandler;
//    private CeilingDetector ceilingDetector;
//    #endregion

//    #region Movement Configuration
//    [Header("Movement Settings")]
//    [SerializeField] private float movementSpeed = 7.0f;
//    [SerializeField] private float airControlRate = 2.0f;

//    [Header("Jump Settings")]
//    [SerializeField] private float jumpSpeed = 10.0f;
//    [SerializeField] private float jumpDuration = 0.2f;

//    [Header("Physics Settings")]
//    [SerializeField] private float gravity = 30.0f;
//    [SerializeField] private float airFriction = 0.5f;
//    [SerializeField] private float groundFriction = 100.0f;

//    [Header("Slope Settings")]
//    [SerializeField] private float slopeLimit = 80.0f;
//    [SerializeField, Tooltip("How fast the character will slide down steep slopes")]
//    private float slideGravity = 5.0f;

//    [Header("Advanced Settings")]
//    [SerializeField, Tooltip("Whether to calculate and apply momentum relative to the controller's transform")]
//    private bool useLocalMomentum = false;
//    #endregion

//    #region State Variables
//    private ControllerState currentState = ControllerState.Falling;
//    private Vector2 momentum = Vector2.zero;
//    private Vector2 previousFrameVelocity = Vector2.zero;
//    private Vector2 previousFrameMovementVelocity = Vector2.zero;
//    #endregion

//    #region Jump Input Variables
//    private class JumpInputState
//    {
//        public bool isLocked = false;
//        public bool wasPressed = false;
//        public bool wasReleased = false;
//        public bool isCurrentlyPressed = false;
//        public float jumpStartTime = 0.0f;

//        public void Reset()
//        {
//            wasPressed = false;
//            wasReleased = false;
//        }
//    }

//    private readonly JumpInputState jumpInput = new();
//    #endregion

//    #region Unity Lifecycle
//    private void Awake()
//    {
//        InitializeComponents();
//        ValidateComponents();
//        Setup();
//    }

//    protected virtual void Setup() { }

//    private void Update() => ProcessJumpInput();

//    private void FixedUpdate() => UpdateController();
//    #endregion

//    #region Component Initialization
//    /// <summary>
//    /// Initialize and cache component references
//    /// </summary>
//    private void InitializeComponents()
//    {
//        cachedTransform = transform;
//        characterMover = GetComponent<Mover>();
//        inputHandler = GetComponent<CharacterInput>();
//        ceilingDetector = GetComponent<CeilingDetector>();
//    }

//    /// <summary>
//    /// Validate that required components are present
//    /// </summary>
//    private void ValidateComponents()
//    {
//        if (inputHandler == null)
//        {
//            Debug.LogWarning($"[{gameObject.name}] No CharacterInput script attached. Controller will not respond to input.", gameObject);
//        }
//    }
//    #endregion

//    #region Input Processing
//    /// <summary>
//    /// Process jump input and update jump state flags
//    /// </summary>
//    private void ProcessJumpInput()
//    {
//        bool currentJumpPressed = IsJumpInputPressed();

//        // Detect jump key press
//        if (!jumpInput.isCurrentlyPressed && currentJumpPressed)
//        {
//            jumpInput.wasPressed = true;
//        }

//        // Detect jump key release
//        if (jumpInput.isCurrentlyPressed && !currentJumpPressed)
//        {
//            jumpInput.wasReleased = true;
//            jumpInput.isLocked = false;
//        }

//        jumpInput.isCurrentlyPressed = currentJumpPressed;
//    }

//    /// <summary>
//    /// Check if jump input is currently pressed
//    /// </summary>
//    protected virtual bool IsJumpInputPressed()
//    {
//        return (inputHandler != null) && inputHandler.IsJumpKeyPressed();
//    }
//    #endregion

//    #region Main Update Loop
//    /// <summary>
//    /// Main controller update method called every fixed timestep
//    /// </summary>
//    private void UpdateController()
//    {
//        // Update ground detection
//        characterMover.CheckForGround();

//        // Update controller state
//        UpdateControllerState();

//        // Apply physics to momentum
//        ApplyPhysicsToMomentum();

//        // Handle jump initiation
//        ProcessJumpAttempt();

//        // Calculate final velocity
//        Vector2 finalVelocity = CalculateFinalVelocity();

//        // Configure mover settings
//        ConfigureMoverSettings();

//        // Apply velocity to mover
//        characterMover.SetVelocity(finalVelocity);

//        // Store values for next frame
//        CacheFrameData(finalVelocity);

//        // Reset input flags
//        ResetFrameFlags();
//    }

//    /// <summary>
//    /// Calculate the final velocity to be applied to the mover
//    /// </summary>
//    private Vector2 CalculateFinalVelocity()
//    {
//        Vector2 velocity = Vector2.zero;

//        // Add movement velocity only when grounded
//        if (currentState == ControllerState.Grounded)
//        {
//            velocity = CalculateMovementVelocity();
//        }

//        // Add momentum (convert to world space if needed)
//        Vector3 worldMomentum = ConvertMomentumToWorldSpace(momentum);
//        velocity += worldMomentum;

//        return velocity;
//    }

//    /// <summary>
//    /// Configure mover settings based on current state
//    /// </summary>
//    private void ConfigureMoverSettings()
//    {
//        // Enable extended sensor range when grounded for better stair/slope handling
//        characterMover.SetUseExtendedSensorRange(IsGrounded());
//    }

//    /// <summary>
//    /// Cache data needed for next frame
//    /// </summary>
//    private void CacheFrameData(Vector2 velocity)
//    {
//        previousFrameVelocity = velocity;
//        previousFrameMovementVelocity = CalculateMovementVelocity();
//    }

//    /// <summary>
//    /// Reset flags that should only last one frame
//    /// </summary>
//    private void ResetFrameFlags()
//    {
//        jumpInput.Reset();

//        if (ceilingDetector != null)
//        {
//            ceilingDetector.ResetFlags();
//        }
//    }
//    #endregion

//    #region Movement Calculation
//    /// <summary>
//    /// Calculate normalized movement direction based on input and camera orientation
//    /// </summary>
//    protected virtual Vector2 CalculateMovementDirection()
//    {
//        if (inputHandler == null)
//            return Vector2.zero;

//        Vector2 direction = Vector2.zero;
//        float horizontalInput = inputHandler.GetHorizontalMovementInput();
//        float verticalInput = inputHandler.GetVerticalMovementInput();

//        if (cameraTransform == null)
//        {
//            // Use character's local axes
//            direction += (Vector2)cachedTransform.right * horizontalInput;
//        }
//        else
//        {
//            // Use camera-relative movement
//            Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, cachedTransform.up).normalized;
//            Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, cachedTransform.up).normalized;

//            direction += cameraRight * horizontalInput;
//            direction += cameraForward * verticalInput;
//        }

//        // Normalize to prevent faster diagonal movement
//        if (direction.magnitude > 1f)
//        {
//            direction.Normalize();
//        }

//        return direction;
//    }

//    /// <summary>
//    /// Calculate movement velocity by applying speed to movement direction
//    /// </summary>
//    protected virtual Vector2 CalculateMovementVelocity()
//    {
//        Vector2 direction = CalculateMovementDirection();
//        return direction * movementSpeed;
//    }
//    #endregion

//    #region State Management
//    /// <summary>
//    /// Update the controller state based on current conditions
//    /// </summary>
//    private void UpdateControllerState()
//    {
//        currentState = DetermineNewControllerState();
//    }

//    /// <summary>
//    /// Determine what the new controller state should be based on current conditions
//    /// </summary>
//    private ControllerState DetermineNewControllerState()
//    {
//        bool isRising = IsMovingVertically() && IsMovingUpward();
//        bool isOnSteepSlope = characterMover.IsGrounded() && IsGroundTooSteep();

//        return currentState switch
//        {
//            ControllerState.Grounded => HandleGroundedState(isRising, isOnSteepSlope),
//            ControllerState.Falling => HandleFallingState(isRising, isOnSteepSlope),
//            ControllerState.Sliding => HandleSlidingState(isRising),
//            ControllerState.Rising => HandleRisingState(isRising, isOnSteepSlope),
//            ControllerState.Jumping => HandleJumpingState(),
//            _ => ControllerState.Falling
//        };
//    }

//    private ControllerState HandleGroundedState(bool isRising, bool isOnSteepSlope)
//    {
//        if (isRising)
//        {
//            OnGroundContactLost();
//            return ControllerState.Rising;
//        }

//        if (!characterMover.IsGrounded())
//        {
//            OnGroundContactLost();
//            return ControllerState.Falling;
//        }

//        if (isOnSteepSlope)
//        {
//            OnGroundContactLost();
//            return ControllerState.Sliding;
//        }

//        return ControllerState.Grounded;
//    }

//    private ControllerState HandleFallingState(bool isRising, bool isOnSteepSlope)
//    {
//        if (isRising) return ControllerState.Rising;

//        if (characterMover.IsGrounded())
//        {
//            if (isOnSteepSlope) return ControllerState.Sliding;

//            OnGroundContactRegained();
//            return ControllerState.Grounded;
//        }

//        return ControllerState.Falling;
//    }

//    private ControllerState HandleSlidingState(bool isRising)
//    {
//        if (isRising)
//        {
//            OnGroundContactLost();
//            return ControllerState.Rising;
//        }

//        if (!characterMover.IsGrounded())
//        {
//            OnGroundContactLost();
//            return ControllerState.Falling;
//        }

//        if (characterMover.IsGrounded() && !IsGroundTooSteep())
//        {
//            OnGroundContactRegained();
//            return ControllerState.Grounded;
//        }

//        return ControllerState.Sliding;
//    }

//    private ControllerState HandleRisingState(bool isRising, bool isOnSteepSlope)
//    {
//        if (CheckCeilingCollision())
//        {
//            OnCeilingContact();
//            return ControllerState.Falling;
//        }

//        if (!isRising)
//        {
//            if (characterMover.IsGrounded())
//            {
//                if (isOnSteepSlope) return ControllerState.Sliding;

//                OnGroundContactRegained();
//                return ControllerState.Grounded;
//            }

//            return ControllerState.Falling;
//        }

//        return ControllerState.Rising;
//    }

//    private ControllerState HandleJumpingState()
//    {
//        // Check for jump timeout
//        if (Time.time - jumpInput.jumpStartTime > jumpDuration)
//        {
//            return ControllerState.Rising;
//        }

//        // Check if jump key was released
//        if (jumpInput.wasReleased)
//        {
//            return ControllerState.Rising;
//        }

//        // Check for ceiling collision
//        if (CheckCeilingCollision())
//        {
//            OnCeilingContact();
//            return ControllerState.Falling;
//        }

//        return ControllerState.Jumping;
//    }
//    #endregion

//    #region Jump Handling
//    /// <summary>
//    /// Check if player is trying to jump and handle jump initiation
//    /// </summary>
//    private void ProcessJumpAttempt()
//    {
//        if (currentState == ControllerState.Grounded &&
//            (jumpInput.isCurrentlyPressed || jumpInput.wasPressed) &&
//            !jumpInput.isLocked)
//        {
//            InitiateJump();
//        }
//    }

//    /// <summary>
//    /// Start a jump by changing state and applying jump force
//    /// </summary>
//    private void InitiateJump()
//    {
//        OnGroundContactLost();
//        OnJumpStart();
//        currentState = ControllerState.Jumping;
//    }
//    #endregion

//    #region Physics and Momentum
//    /// <summary>
//    /// Apply physics forces and friction to momentum
//    /// </summary>
//    private void ApplyPhysicsToMomentum()
//    {
//        // Convert to world space for calculations if needed
//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.localToWorldMatrix * momentum;
//        }

//        // Split momentum into components
//        var (verticalMomentum, horizontalMomentum) = SplitMomentumComponents();

//        // Apply gravity
//        verticalMomentum = ApplyGravity(verticalMomentum);

//        // Handle air control and ground friction
//        horizontalMomentum = ApplyHorizontalForces(horizontalMomentum);

//        // Handle sliding physics
//        if (currentState == ControllerState.Sliding)
//        {
//            (verticalMomentum, horizontalMomentum) = ApplySlidingPhysics(verticalMomentum, horizontalMomentum);
//        }

//        // Override momentum for jumping
//        if (currentState == ControllerState.Jumping)
//        {
//            verticalMomentum = cachedTransform.up * jumpSpeed;
//        }

//        // Recombine momentum
//        momentum = horizontalMomentum + verticalMomentum;

//        // Convert back to local space if needed
//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.worldToLocalMatrix * momentum;
//        }
//    }

//    /// <summary>
//    /// Split momentum into vertical and horizontal components
//    /// </summary>
//    private (Vector3 vertical, Vector3 horizontal) SplitMomentumComponents()
//    {
//        if (momentum == Vector3.zero)
//        {
//            return (Vector3.zero, Vector3.zero);
//        }

//        Vector3 vertical = VectorMath.ExtractDotVector(momentum, cachedTransform.up);
//        Vector3 horizontal = momentum - vertical;
//        return (vertical, horizontal);
//    }

//    /// <summary>
//    /// Apply gravity to vertical momentum
//    /// </summary>
//    private Vector3 ApplyGravity(Vector3 verticalMomentum)
//    {
//        verticalMomentum -= cachedTransform.up * gravity * Time.deltaTime;

//        // Remove downward force when grounded
//        if (currentState == ControllerState.Grounded &&
//            Vector3.Dot(verticalMomentum, cachedTransform.up) < 0f)
//        {
//            verticalMomentum = Vector3.zero;
//        }

//        return verticalMomentum;
//    }

//    /// <summary>
//    /// Apply air control or ground friction to horizontal momentum
//    /// </summary>
//    private Vector3 ApplyHorizontalForces(Vector3 horizontalMomentum)
//    {
//        if (!IsGrounded())
//        {
//            horizontalMomentum = ApplyAirControl(horizontalMomentum);
//        }
//        else if (currentState == ControllerState.Sliding)
//        {
//            horizontalMomentum = ApplySlopeMovement(horizontalMomentum);
//        }

//        // Apply friction
//        float frictionRate = currentState == ControllerState.Grounded ? groundFriction : airFriction;
//        horizontalMomentum = VectorMath.IncrementVectorTowardsTargetVector(
//            horizontalMomentum, Vector3.zero, frictionRate, Time.deltaTime);

//        return horizontalMomentum;
//    }

//    /// <summary>
//    /// Apply air control to horizontal momentum
//    /// </summary>
//    private Vector3 ApplyAirControl(Vector3 horizontalMomentum)
//    {
//        Vector3 movementVelocity = CalculateMovementVelocity();

//        if (horizontalMomentum.magnitude > movementSpeed)
//        {
//            // Prevent speed accumulation in momentum direction
//            if (Vector3.Dot(movementVelocity, horizontalMomentum.normalized) > 0f)
//            {
//                movementVelocity = VectorMath.RemoveDotVector(movementVelocity, horizontalMomentum.normalized);
//            }

//            horizontalMomentum += movementVelocity * Time.deltaTime * airControlRate * AIR_CONTROL_WEIGHT_MULTIPLIER;
//        }
//        else
//        {
//            horizontalMomentum += movementVelocity * Time.deltaTime * airControlRate;
//            horizontalMomentum = Vector3.ClampMagnitude(horizontalMomentum, movementSpeed);
//        }

//        return horizontalMomentum;
//    }

//    /// <summary>
//    /// Apply movement control while on slopes
//    /// </summary>
//    private Vector3 ApplySlopeMovement(Vector3 horizontalMomentum)
//    {
//        Vector3 slopeDownDirection = Vector3.ProjectOnPlane(characterMover.GetGroundNormal(), cachedTransform.up).normalized;
//        Vector3 slopeMovementVelocity = CalculateMovementVelocity();

//        // Remove velocity pointing up the slope
//        slopeMovementVelocity = VectorMath.RemoveDotVector(slopeMovementVelocity, slopeDownDirection);
//        horizontalMomentum += slopeMovementVelocity * Time.fixedDeltaTime;

//        return horizontalMomentum;
//    }

//    /// <summary>
//    /// Apply sliding physics when on steep slopes
//    /// </summary>
//    private (Vector3 vertical, Vector3 horizontal) ApplySlidingPhysics(Vector3 verticalMomentum, Vector3 horizontalMomentum)
//    {
//        Vector3 combinedMomentum = horizontalMomentum + verticalMomentum;

//        // Project momentum onto slope surface
//        combinedMomentum = Vector3.ProjectOnPlane(combinedMomentum, characterMover.GetGroundNormal());

//        // Remove any upward momentum
//        if (Vector3.Dot(combinedMomentum, cachedTransform.up) > 0f)
//        {
//            combinedMomentum = VectorMath.RemoveDotVector(combinedMomentum, cachedTransform.up);
//        }

//        // Apply slide gravity
//        Vector3 slideDirection = Vector3.ProjectOnPlane(-cachedTransform.up, characterMover.GetGroundNormal()).normalized;
//        combinedMomentum += slideDirection * slideGravity * Time.deltaTime;

//        // Split back into components
//        Vector3 newVertical = VectorMath.ExtractDotVector(combinedMomentum, cachedTransform.up);
//        Vector3 newHorizontal = combinedMomentum - newVertical;

//        return (newVertical, newHorizontal);
//    }
//    #endregion

//    #region Event Handlers
//    /// <summary>
//    /// Called when a jump is initiated
//    /// </summary>
//    private void OnJumpStart()
//    {
//        // Convert to world space for calculations
//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.localToWorldMatrix * momentum;
//        }

//        // Add jump force
//        momentum += cachedTransform.up * jumpSpeed;

//        // Set jump timing
//        jumpInput.jumpStartTime = Time.time;
//        jumpInput.isLocked = true;

//        // Trigger event
//        OnJump?.Invoke(momentum);

//        // Convert back to local space
//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.worldToLocalMatrix * momentum;
//        }
//    }

//    /// <summary>
//    /// Called when the controller loses ground contact
//    /// </summary>
//    private void OnGroundContactLost()
//    {
//        // Convert to world space
//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.localToWorldMatrix * momentum;
//        }

//        Vector3 movementVelocity = GetMovementVelocity();

//        // Prevent unwanted speed accumulation
//        if (movementVelocity.sqrMagnitude >= 0f && momentum.sqrMagnitude > 0f)
//        {
//            Vector3 projectedMomentum = Vector3.Project(momentum, movementVelocity.normalized);
//            float alignment = Vector3.Dot(projectedMomentum.normalized, movementVelocity.normalized);

//            if (projectedMomentum.sqrMagnitude >= movementVelocity.sqrMagnitude && alignment > 0f)
//            {
//                movementVelocity = Vector3.zero;
//            }
//            else if (alignment > 0f)
//            {
//                movementVelocity -= projectedMomentum;
//            }
//        }

//        momentum += movementVelocity;

//        // Convert back to local space
//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.worldToLocalMatrix * momentum;
//        }
//    }

//    /// <summary>
//    /// Called when the controller regains ground contact
//    /// </summary>
//    private void OnGroundContactRegained()
//    {
//        Vector3 landingVelocity = momentum;

//        if (useLocalMomentum)
//        {
//            landingVelocity = cachedTransform.localToWorldMatrix * landingVelocity;
//        }

//        OnLand?.Invoke(landingVelocity);
//    }

//    /// <summary>
//    /// Called when the controller hits a ceiling
//    /// </summary>
//    private void OnCeilingContact()
//    {
//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.localToWorldMatrix * momentum;
//        }

//        // Remove vertical momentum
//        momentum = VectorMath.RemoveDotVector(momentum, cachedTransform.up);

//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.worldToLocalMatrix * momentum;
//        }
//    }
//    #endregion

//    #region Helper Methods
//    /// <summary>
//    /// Check if the controller is moving vertically beyond the threshold
//    /// </summary>
//    private bool IsMovingVertically()
//    {
//        Vector3 verticalMomentum = VectorMath.ExtractDotVector(GetMomentum(), cachedTransform.up);
//        return verticalMomentum.magnitude > RISING_FALLING_THRESHOLD;
//    }

//    /// <summary>
//    /// Check if the controller is moving upward
//    /// </summary>
//    private bool IsMovingUpward()
//    {
//        return Vector3.Dot(GetMomentum(), cachedTransform.up) > 0f;
//    }

//    /// <summary>
//    /// Check if the ground angle exceeds the slope limit
//    /// </summary>
//    private bool IsGroundTooSteep()
//    {
//        if (!characterMover.IsGrounded()) return true;
//        return Vector3.Angle(characterMover.GetGroundNormal(), cachedTransform.up) > slopeLimit;
//    }

//    /// <summary>
//    /// Check if the controller hit a ceiling
//    /// </summary>
//    private bool CheckCeilingCollision()
//    {
//        return ceilingDetector?.HitCeiling() ?? false;
//    }

//    /// <summary>
//    /// Convert momentum to world space if local momentum is used
//    /// </summary>
//    private Vector3 ConvertMomentumToWorldSpace(Vector3 localMomentum)
//    {
//        return useLocalMomentum ? cachedTransform.localToWorldMatrix * localMomentum : localMomentum;
//    }
//    #endregion

//    #region Public Interface
//    /// <summary>
//    /// Get the current movement velocity (input-based movement only)
//    /// </summary>
//    public override Vector2 GetMovementVelocity()
//    {
//        return previousFrameMovementVelocity;
//    }

//    /// <summary>
//    /// Get the total velocity including momentum
//    /// </summary>
//    public override Vector2 GetVelocity()
//    {
//        return previousFrameVelocity;
//    }

//    /// <summary>
//    /// Check if the controller is currently grounded
//    /// </summary>
//    public override bool IsGrounded()
//    {
//        return currentState == ControllerState.Grounded || currentState == ControllerState.Sliding;
//    }

//    /// <summary>
//    /// Get the current momentum in world space
//    /// </summary>
//    public Vector3 GetMomentum()
//    {
//        return ConvertMomentumToWorldSpace(momentum);
//    }

//    /// <summary>
//    /// Check if the controller is currently sliding down a slope
//    /// </summary>
//    public bool IsSliding()
//    {
//        return currentState == ControllerState.Sliding;
//    }

//    /// <summary>
//    /// Get the current controller state
//    /// </summary>
//    public ControllerState GetCurrentState()
//    {
//        return currentState;
//    }

//    /// <summary>
//    /// Add momentum to the controller
//    /// </summary>
//    public void AddMomentum(Vector3 additionalMomentum)
//    {
//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.localToWorldMatrix * momentum;
//        }

//        momentum += additionalMomentum;

//        if (useLocalMomentum)
//        {
//            momentum = cachedTransform.worldToLocalMatrix * momentum;
//        }
//    }

//    /// <summary>
//    /// Set the controller's momentum directly
//    /// </summary>
//    public void SetMomentum(Vector3 newMomentum)
//    {
//        momentum = useLocalMomentum ? cachedTransform.worldToLocalMatrix * newMomentum : newMomentum;
//    }

//    /// <summary>
//    /// Reset the controller's momentum to zero
//    /// </summary>
//    public void ResetMomentum()
//    {
//        momentum = Vector3.zero;
//    }
//    #endregion

//    #region Configuration Properties
//    /// <summary>
//    /// Movement speed when grounded
//    /// </summary>
//    public float MovementSpeed
//    {
//        get => movementSpeed;
//        set => movementSpeed = Mathf.Max(0f, value);
//    }

//    /// <summary>
//    /// Rate of air control while not grounded
//    /// </summary>
//    public float AirControlRate
//    {
//        get => airControlRate;
//        set => airControlRate = Mathf.Max(0f, value);
//    }

//    /// <summary>
//    /// Initial speed applied when jumping
//    /// </summary>
//    public float JumpSpeed
//    {
//        get => jumpSpeed;
//        set => jumpSpeed = Mathf.Max(0f, value);
//    }

//    /// <summary>
//    /// Maximum duration of active jump state
//    /// </summary>
//    public float JumpDuration
//    {
//        get => jumpDuration;
//        set => jumpDuration = Mathf.Max(0f, value);
//    }

//    /// <summary>
//    /// Downward acceleration due to gravity
//    /// </summary>
//    public float Gravity
//    {
//        get => gravity;
//        set => gravity = Mathf.Max(0f, value);
//    }

//    /// <summary>
//    /// Friction applied when in the air
//    /// </summary>
//    public float AirFriction
//    {
//        get => airFriction;
//        set => airFriction = Mathf.Max(0f, value);
//    }

//    /// <summary>
//    /// Friction applied when on the ground
//    /// </summary>
//    public float GroundFriction
//    {
//        get => groundFriction;
//        set => groundFriction = Mathf.Max(0f, value);
//    }

//    /// <summary>
//    /// Maximum angle (in degrees) for walkable slopes
//    /// </summary>
//    public float SlopeLimit
//    {
//        get => slopeLimit;
//        set => slopeLimit = Mathf.Clamp(value, 0f, 90f);
//    }

//    /// <summary>
//    /// Additional downward force when sliding on steep slopes
//    /// </summary>
//    public float SlideGravity
//    {
//        get => slideGravity;
//        set => slideGravity = Mathf.Max(0f, value);
//    }

//    /// <summary>
//    /// Whether momentum calculations use local or world space
//    /// </summary>
//    public bool UseLocalMomentum
//    {
//        get => useLocalMomentum;
//        set => useLocalMomentum = value;
//    }
//    #endregion
//}
