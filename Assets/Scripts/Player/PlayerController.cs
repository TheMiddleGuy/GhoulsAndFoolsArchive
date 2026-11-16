using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BasicMovement : MonoBehaviour
{
    private InputActionMap _playerActionMap;
    private Rigidbody _rb;
    public Animator Anim;
    [Header("Aiming")]
    [SerializeField] private float _aimSpeed = 8f;
    [SerializeField] private float _minAim = 0.7f;  //Minimum joystick input to register aiming
    [Tooltip("Player, right joystick input.")]
    private Vector2 _aimInputDirection;
    [Tooltip("Final direction the player will look towards.")]
    private Vector3 _lookDirection = Vector3.zero;

    [Header("Moving")]
    [SerializeField] private float _moveSpeed = 6f;
    [SerializeField] private float _minMove = 0.05f;    //Prevents very slight joycon drift
    [SerializeField] private float _backwardMovementMultiplier = 0.7f;  //The movement multiplier for aiming backwards
    [SerializeField] private float _vacuumMovementSpeed = 0.7f; //The movement multiplier value for using the vacuum
    private float _vacuumMovementMultiplier = 1f;
    [SerializeField] private float _vacuumingGhostMultiplier = 0.4f;
    [SerializeField] private float _moveAcceleration = 4f;  //Acceleration, especially noticeable when you quickly turn 180°
    [Tooltip("Player, left joystick input.")]
    private Vector2 _moveInputDirection;
    [Tooltip("Final direction the player will move towards.")]
    private Vector3 _moveDirection;

    private Vector3 _externalVelocity;
    private Vector3 _currentExternalVelocity;
    [SerializeField] private float _externalVelocityLerpSpeed = 5f;

    private bool _isPushing;
    [SerializeField] private float _animSpeedMultiplier = 2f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _walkParticles;
    [SerializeField] private float _maxMoveParticlesEmission = 3f;
    [SerializeField] private float _minMoveParticlesEmission = 0f;

    [Header("Death")]
    [SerializeField] private bool _dead = false; //SerializeField for testing purpose

    [Header("Death Minigame Settings")]
    [SerializeField] private float _maxMinigameDistance = 20f;
    [SerializeField] private float _minHeartbeatInterval = 0.3f; // Time between full cycles when going RIGHT way
    [SerializeField] private float _maxHeartbeatInterval = 1.2f; // Time between full cycles when going WRONG way

    [Header("Improved Heartbeat (Thump)")]
    // We use very short bursts to avoid the 'buzzing' sensation
    [SerializeField] private float _lubDuration = 0.06f;  // Extremely short
    [SerializeField] private float _lubStrength = 1.0f;   // Full power to overcome inertia
    [SerializeField] private float _gapDuration = 0.1f;
    [SerializeField] private float _dubDuration = 0.04f;  // Even shorter
    [SerializeField] private float _dubStrength = 0.5f;

    private float _heartbeatTimer = 0f;
    private int _beatPhase = 0; // 0 = Resting, 1 = Lub, 2 = Gap, 3 = Dub
    [Header("Virtual Minigame Logic")]
    [SerializeField] private float _minigameAreaRadius = 10f;
    [SerializeField] private float _virtualPlayerSpeed = 8f;
    [SerializeField] private float _virtualTargetSpeed = 3f;
    [SerializeField] private float _proximityExp = 3.5f; // Higher = steeper curve (stays 0-1 longer)

    private Vector2 _virtualPlayerPos;
    private Vector2 _virtualTargetPos;
    private Vector2 _targetWanderDir;
    private float _targetWanderTimer;

    public GameObject PickUpManager;
    private PlayerPickUp _playerPickUp;
    public int PlayerId;
    public static Action<int, float> OnProximityStatusChanged;
    public bool Dead
    {
        get => _dead;
        set
        {
            if (value == true && !_dead) // Just died
            {
                // Randomize positions within the 10m radius
                _virtualPlayerPos = UnityEngine.Random.insideUnitCircle * _minigameAreaRadius;
                _virtualTargetPos = UnityEngine.Random.insideUnitCircle * _minigameAreaRadius;
                PickNewWanderDirection();
            }
            _dead = value;
        }
    }

    private void PickNewWanderDirection()
    {
        _targetWanderDir = UnityEngine.Random.insideUnitCircle.normalized;
        _targetWanderTimer = UnityEngine.Random.Range(1f, 3f);
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        PlayerInput input = GetComponent<PlayerInput>();
        _playerActionMap = input.actions.FindActionMap("Player");

        _playerActionMap.FindAction("Move").performed += context => _moveInputDirection = context.ReadValue<Vector2>();
        _playerActionMap.FindAction("Move").canceled += context => _moveInputDirection = Vector2.zero;
        _playerActionMap.FindAction("Aim").performed += context => _aimInputDirection = context.ReadValue<Vector2>();
        _playerActionMap.FindAction("Aim").canceled += context => _aimInputDirection = Vector2.zero;
        _playerPickUp = PickUpManager.GetComponent<PlayerPickUp>(); // Cache the pickup script
        
    }
    //private void Start() 
    //{
    //    //PlayerId = GetComponent<PlayerColor>().PlayerId;
    //    Debug.Log($"Player {name} has ID {PlayerId}");
    //}

    private void OnEnable() => _playerActionMap.Enable();
    private void OnDisable() => _playerActionMap.Disable();

    private void Update()
    {
        Debug.Log($"Player {name} has ID {PlayerId}");
        if (_dead)
        {
            // Ensure walking stops if the player dies
            if (Anim != null) 
            {
                Anim.speed = 0f;
            }
            HandleDeathMinigame();
            return;
        }

        HandleAiming();
        UpdateAnimation(); // <--- Add this call
    }
    private void UpdateAnimation()
    {
        if (Anim == null || _rb == null) return;

        // 1. Check Movement
        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        float currentPhysicalSpeed = horizontalVelocity.magnitude;
        bool isMoving = currentPhysicalSpeed > 0.1f && _moveInputDirection.magnitude >= _minMove;

        // 2. Check Item Status
        bool holdingItem = _playerPickUp.IsHoldingItem();

        // 3. Update Animator Parameters
        Anim.SetBool("IsWalking", isMoving);
        Anim.SetBool("HasItem", holdingItem);

        // 4. Handle Playback Speed
        if (isMoving)
        {
            float speedRatio = (currentPhysicalSpeed / _moveSpeed) * _animSpeedMultiplier;
            Anim.speed = Mathf.Clamp(speedRatio, 0.2f, 3.0f);
        }
        else
        {
            Anim.speed = 1f; // Normal speed for Idle or Idle_With_Item
        }
    }
    private void FixedUpdate()
    {
        if (_dead) return;
        HandleMoving();
    }

    //Handles the aiming input
    private void HandleAiming()
    {
        if(_isPushing)
        {
            RotatePlayer();
            return;
        }

        //If the aiming input is registered, aiming decides player rotation
        if (_aimInputDirection.magnitude >= _minAim)
        {
            _lookDirection = new Vector3(_aimInputDirection.x, 0f, _aimInputDirection.y).normalized;
            RotatePlayer();
        }
        //Otherwise if the player is moving, movement decides player rotation
        else if (_moveInputDirection.magnitude >= _minMove)
        {
            _lookDirection = new Vector3(_moveInputDirection.x, 0f, _moveInputDirection.y).normalized;
            RotatePlayer();
        }
    }

    //Rotates the player towards their current _lookDirection
    private void RotatePlayer()
    {
        Quaternion targetRotation = Quaternion.LookRotation(_lookDirection);
        _rb.MoveRotation(Quaternion.Lerp(
            _rb.rotation,
            targetRotation,
            _aimSpeed * Time.deltaTime
        ));
    }

    public void AddExternalVelocity(Vector3 velocity)
    {
        _externalVelocity += velocity;
    }

    //Handles the player movement
    private void HandleMoving()
    {
        if (_isPushing) return;

        // 1. Keep your existing moveDirection calculation
        _moveDirection = Vector3.MoveTowards(
            _moveDirection,
            new Vector3(_moveInputDirection.x, 0f, _moveInputDirection.y),
            _moveAcceleration * Time.fixedDeltaTime
        );

        float alignment01 = (Vector3.Dot(_moveDirection.normalized, _lookDirection.normalized) + 1f) * 0.5f;
        float speedMultiplier = Mathf.Lerp(_backwardMovementMultiplier, 1f, alignment01);

        // 2. Calculate what our TARGET velocity should be
        _currentExternalVelocity = Vector3.Lerp(
            _currentExternalVelocity,
            _externalVelocity,
            _externalVelocityLerpSpeed * Time.fixedDeltaTime
        );

        float vacuumingGhostMultiplier = 1f;
        if (_externalVelocity != Vector3.zero)
        {
            vacuumingGhostMultiplier = _vacuumingGhostMultiplier;
        }

        Vector3 targetVelocity =
            _moveDirection * (_moveSpeed * speedMultiplier * _vacuumMovementMultiplier * vacuumingGhostMultiplier)
            + _currentExternalVelocity;

        _externalVelocity = Vector3.zero;

        // 3. Calculate the difference between where we ARE and where we WANT to be
        Vector3 velocityChange = targetVelocity - _rb.linearVelocity;

        // 4. Ignore vertical velocity so we don't mess up gravity
        velocityChange.y = 0;

        // 5. Apply the change as an Impulse
        // This allows the Rigidbody to 'slide' because we aren't overriding 
        // the physics engine's collision response every frame.
        _rb.AddForce(velocityChange, ForceMode.VelocityChange);

        MovementParticles();
    }

    private void MovementParticles()
    {
        float currentSpeed = _rb.linearVelocity.magnitude;

        float speed01 = Mathf.Clamp01(currentSpeed / _moveSpeed);

        float emissionRate = Mathf.Lerp(
            _minMoveParticlesEmission,
            _maxMoveParticlesEmission,
            speed01
        );

        ParticleSystem.EmissionModule emission = _walkParticles.emission;
        emission.rateOverTime = emissionRate;

        if (currentSpeed > 0.1f)
        {
            if (!_walkParticles.isPlaying)
                _walkParticles.Play();
        }
        else
        {
            if (_walkParticles.isPlaying)
                _walkParticles.Stop();
        }
    }

    public bool IsVisible()
    {
        return true;
    }

    public void SetPushing(bool state, Vector3? lookDirection)
    {
        if (lookDirection != null)
        {
            _lookDirection = (Vector3)lookDirection;
        }
        _isPushing = state;
    }

    public Vector3 GetInputDirection()
    {
        return new Vector3(_moveInputDirection.x, 0, _moveInputDirection.y);
    }

    public void StartVacuumSlowness()
    {
        _vacuumMovementMultiplier = _vacuumMovementSpeed;
    }

    public void EndVacuumSlowness()
    {
        _vacuumMovementMultiplier = 1f;
    }
    private void HandleDeathMinigame()
    {
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null || health.IsAlive || health.CurrentReviveGhost == null) return;

        ReviveGhostBehavior ghostBehavior = health.CurrentReviveGhost.GetComponent<ReviveGhostBehavior>();
        PlayerControllerRumble rumbleManager = GetComponent<PlayerControllerRumble>();

        // --- 1. VIRTUAL MOVEMENT ---

        // Move Virtual Player based on Joystick
        _virtualPlayerPos += _moveInputDirection * (_virtualPlayerSpeed * Time.deltaTime);
        _virtualPlayerPos = Vector2.ClampMagnitude(_virtualPlayerPos, _minigameAreaRadius);

        // TARGET LOGIC: Decide whether to Wander or Flee
        float distToPlayer = Vector2.Distance(_virtualTargetPos, _virtualPlayerPos);
        float fleeThreshold = 4f; // Distance at which the target notices the player

        if (distToPlayer < fleeThreshold)
        {
            // FLEE: Move directly away from the player
            Vector2 fleeDir = (_virtualTargetPos - _virtualPlayerPos).normalized;
            _virtualTargetPos += fleeDir * (_virtualTargetSpeed * 1.5f * Time.deltaTime); // Extra speed boost when fleeing
        }
        else
        {
            // WANDER: Move in the random direction
            _targetWanderTimer -= Time.deltaTime;
            if (_targetWanderTimer <= 0) PickNewWanderDirection();
            _virtualTargetPos += _targetWanderDir * (_virtualTargetSpeed * Time.deltaTime);
        }

        // Keep target inside the 10m circle
        _virtualTargetPos = Vector2.ClampMagnitude(_virtualTargetPos, _minigameAreaRadius);
        // --- 2. CALCULATE PROXIMITY SCORE (Non-Linear) ---

        float dist = Vector2.Distance(_virtualPlayerPos, _virtualTargetPos);
        float maxPossibleDist = _minigameAreaRadius * 2f;

        // Normalize distance (0 = on target, 1 = max distance)
        float normalizedDist = Mathf.Clamp01(dist / maxPossibleDist);

        // Invert so 1 = on target, 0 = far away
        float closeness = 1f - normalizedDist;

        // Apply Power Curve: Stay low (0-1) for a long time, spike to 5 at the end
        // Formula: 5 * (closeness ^ exponent)
        float proximityScore = 5f * Mathf.Pow(closeness, _proximityExp);

        ghostBehavior.SetPlayerMiniGameStatus(proximityScore);
        // Tell the UI that the score has updated!
        OnProximityStatusChanged?.Invoke(PlayerId,proximityScore);

        // --- 3. HEARTBEAT ALIGNMENT ---

        // Direction from virtual player to virtual target
        Vector2 dirToTarget = (_virtualTargetPos - _virtualPlayerPos).normalized;
        float alignment = Vector2.Dot(_moveInputDirection.normalized, dirToTarget);

        float currentInterval = _maxHeartbeatInterval;
        if (_moveInputDirection.magnitude > 0.1f)
        {
            // Map alignment [-1, 1] to [0, 1] for Lerping
            float normalizedAlignment = (alignment + 1f) / 2f;
            currentInterval = Mathf.Lerp(_maxHeartbeatInterval, _minHeartbeatInterval, normalizedAlignment);
        }
        else
        {
            // IDLE PROXIMITY MODE: Interval based on how far away the target is
            float dist2 = Vector2.Distance(_virtualPlayerPos, _virtualTargetPos);
            float maxDist = _minigameAreaRadius * 2f;

            // 0 = touching, 1 = max distance
            float normalizedDist2 = Mathf.Clamp01(dist2 / maxDist);

            // Closer = Faster heartbeat
            currentInterval = Mathf.Lerp(_minHeartbeatInterval, _maxHeartbeatInterval, normalizedDist2);
        }

        // --- 4. EXECUTE RHYTHM ---

        _heartbeatTimer -= Time.deltaTime;
        if (_heartbeatTimer <= 0f)
        {
            switch (_beatPhase)
            {
                case 0: // Lub
                    if (rumbleManager != null) rumbleManager.Pulse(_lubStrength, 0.0f);
                    _heartbeatTimer = _lubDuration;
                    _beatPhase = 1;
                    break;
                case 1: // Gap
                    if (rumbleManager != null) rumbleManager.StopRumble();
                    _heartbeatTimer = _gapDuration;
                    _beatPhase = 2;
                    break;
                case 2: // Dub
                    if (rumbleManager != null) rumbleManager.Pulse(_dubStrength, 0.0f);
                    _heartbeatTimer = _dubDuration;
                    _beatPhase = 3;
                    break;
                case 3: // Rest
                    if (rumbleManager != null) rumbleManager.StopRumble();
                    _heartbeatTimer = currentInterval;
                    _beatPhase = 0;
                    break;
            }
        }
    }
}
