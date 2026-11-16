using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class BasicGhostBehavior : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private float _rotationSpeed = 10f;
    private Vector3 _currentVelocity = Vector3.zero;

    [Header("Bobbing")]
    [SerializeField] private float _bobBaseHeight = -2.9f;
    [SerializeField] private float _bobSpeed = 2f;
    [SerializeField] private float _bobAmount = 0.1f;

    [Header("Ghost Shader")]
    [SerializeField] private Renderer _ghostRenderer;
    private MaterialPropertyBlock _propertyBlock;
    [SerializeField] private float _shaderTransitionLerpSpeed = 5f;

    private int _emissionPropertyID;
    [SerializeField] private string _shaderEmissionProperty = "_emissionT";
    private float _emissionT;

    private int _alphaPropertyID;
    [SerializeField] private string _shaderAlphaProperty = "_alphaT";
    private float _alphaT;
    [SerializeField] private float _ghostSneakyAlpha = 0.2f;
    [SerializeField] private float _ghostVisibleAlpha = 0.8f;
    [SerializeField] private float _ghostInvisibleAlpha = 0f;

    [Header("Ghost Scale")]
    [SerializeField] private GameObject _ghostRendererComponent;
    private Vector3 _ghostOriginalScale;
    [SerializeField] private float _ghostScaleLerpSpeed = 1f;

    [Header("Ghost Light")]
    [SerializeField] private Light _patrolLight;
    [SerializeField] private float _patrolLightMaxIntensity = 1f;
    [SerializeField] private Light _attackLight;
    [SerializeField] private float _attackLightMaxIntensity = 1f;

    [SerializeField] private float _lightLerpSpeed = 2f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _sweatParticles;
    [SerializeField] private ParticleSystem _poof;
    [SerializeField] private ParticleSystem _appear;

    [Header("Collisions")]
    [SerializeField] private string _solidGhostLayerName = "GhostSolid";
    [SerializeField] private string _phaseGhostLayerName = "GhostPhase";

    private int _solidGhostLayer;
    private int _phaseGhostLayer;

    [Header("Patrolling")]
    public List<Vector3> PatrolPoints = new List<Vector3>();
    [SerializeField] private float _patrolSpeed = 2f;

    private int _patrolIndex = 0;
    private Transform _currentTarget;

    [SerializeField] private Vector2 _waitDuration = new Vector2(1, 3);
    private float _waitTimer;

    [SerializeField] private float _stareDuration = 0.5f;
    private float _stareTime = 0f;

    [Header("Chasing")]
    [SerializeField] private LayerMask _friendlyLayer;
    [SerializeField] private float _detectionRadius = 10f;

    [SerializeField] private float _minChaseSpeed = 3f;
    [SerializeField] private float _maxChaseSpeed = 5.5f;

    [SerializeField] private float _distanceMinChaseSpeed = 7f;

    [SerializeField] private float _distanceTransitionStart = 3.5f;

    [Header("Light")]
    [SerializeField] private float _lightRetreatSpeed = 2.2f;
    [SerializeField] private float _lightFearDuration = 0.25f;
    [SerializeField] private float _acceleration = 8f;

    private Transform _currentLightSource;
    private bool _isInLight = false;
    private float _outOfLightTimer = 0f;

    [Header("Fleeing")]
    [SerializeField] private float _fleeingSpeed = 3f;

    [SerializeField] private float _chaseDuration = 8f;
    private float _chaseTimer = 0f;

    [SerializeField] private float _timeInLightBeforeFleeing = 8f;
    private float _timeInLight = 0f;

    [Header("Attacking")]
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackDuration = 3f;
    [SerializeField] private float _attackScareOffDuration = 1f;

    private float _attackTimer = 0f;
    private float _attackScareOffTimer = 0f;
    private Transform _attackingTarget;

    private PlayerHealth _disabledPlayer = null;
    private bool _playerDisabledByThisGhost = false;

    private bool _attackFrozenByLight = false;

    private PlayerControllerRumble _attackedPlayerRumble;

    [Header("Vacuum")]
    [SerializeField] private float _vacuumRefreshGrace = 0.12f;
    private float _lastVacuumRefreshTime = -999f;

    [SerializeField] private float _vacuumMaxMoveSpeed = 8f;
    [SerializeField] private float _vacuumSuckDuration = 2.5f;
    private float _vacuumSuckTimer = 0f;
    [SerializeField] private float _vacuumDistanceSpeedMultiplier = 2f;
    [SerializeField] private float _vacuumDistanceDeadZone = 0.1f;

    private bool _isBeingSuperVacuumed;
    [SerializeField] private float _superVacuumSuckDuration = 2.5f;

    private Transform _currentVacuumPoint;
    private float _vacuumMaxDistance = 1f;

    private bool _previouslyFleeing;

    [Header("Ghost Death")]
    [SerializeField] private ParticleSystem _deathParticles;

    [SerializeField] private GameObject _specialKeyPrefab;   //For dropping the special key when last ghost is killed
    public static event System.Action GhostKilled;

    [Header("Rays")]
    [SerializeField] private LayerMask _worldLayer;

    public float rayOffsetHeight = 1.0f; // Aim for the ghost's chest, not its feet
    public GameObject GhostModel;

    [Header("Audio")]
    [SerializeField] private AudioClip[] _ghostAttacks;
    [SerializeField] private AudioSource _effectsSource;
    [SerializeField] private AudioClip _hiss;

    [Header("Debug")]
    [SerializeField] private bool _killGhost = false;

    [Header("Attack UI")]
    [SerializeField] private GameObject _slider;
    [SerializeField] private UnityEngine.UI.Slider _attackSlider;
    [SerializeField] private Image _sliderFill;
    private PlayerHealth _attackUiOwner;
    private bool _ownsAttackSlider = false;

    [Header("Faces")]
    [SerializeField] private Image _face;
    [SerializeField] private Sprite _facePatrolling;
    [SerializeField] private Sprite _faceStare;
    [SerializeField] private Sprite _faceChase;
    [SerializeField] private Sprite _faceInLight;
    [SerializeField] private Sprite _faceFleeing;
    [SerializeField] private Sprite _faceAttack;
    [SerializeField] private Sprite _faceVacuumed;
    private List<GameObject> _playerList = new List<GameObject>();

    private enum GhostState
    {
        Patrol,
        Stare,
        Chase,
        InLight,
        Fleeing,
        Attack,
        Vacuumed
    }
    private GhostState _currentState = GhostState.Fleeing;

    //State behavior
    private void FixedUpdate()
    {
        UpdateGhostCollisionLayer();

        switch (_currentState)
        {
            case GhostState.Patrol:
                DoPatrol();
                break;

            case GhostState.Stare:
                DoStare();
                break;

            case GhostState.Chase:
                DoChase();
                break;

            case GhostState.InLight:
                BehaviorInLight();
                break;

            case GhostState.Fleeing:
                DoFleeing();
                break;

            case GhostState.Attack:
                DoAttack();
                break;

            case GhostState.Vacuumed:
                DoVacuum();
                break;
        }
    }

    private void Start()
    {
        if (PatrolPoints == null)
            PatrolPoints = new List<Vector3>();

        if (_ghostRenderer == null)
            _ghostRenderer = GetComponentInChildren<Renderer>();

        if (_slider != null)
            _slider.SetActive(false);

        _propertyBlock = new MaterialPropertyBlock();
        _emissionPropertyID = Shader.PropertyToID(_shaderEmissionProperty);
        _alphaPropertyID = Shader.PropertyToID(_shaderAlphaProperty);

        SetGhostEmission(0f);
        SetGhostAlpha(_ghostSneakyAlpha);
        if (_ghostRendererComponent != null)
        {
            _ghostOriginalScale = _ghostRendererComponent.transform.localScale;
        }
        SetGhostScale(0f);
        SetGhostLighting(_patrolLight, 0f);
        SetGhostLighting(_attackLight, 0f);

        _solidGhostLayer = LayerMask.NameToLayer(_solidGhostLayerName);
        _phaseGhostLayer = LayerMask.NameToLayer(_phaseGhostLayerName);

        GhostStartFleeing();
        UpdatePlayerList();
    }

    private void Update()
    {
        if (_killGhost) KillGhost();

        DoAnimation();

        //Vacuum overrides all other states
        //Checks for updates every _vacuumRefreshGrace seconds
        if (Time.time - _lastVacuumRefreshTime <= _vacuumRefreshGrace && _currentVacuumPoint != null)
        {
            if (_currentState == GhostState.Attack)
            {
                ReleasePlayer();
            }

            _currentState = GhostState.Vacuumed;
            return;
        }

        if (_currentState == GhostState.Fleeing)
        {
            // During endgame, ghosts should not remain in or enter fleeing state - instead target closest player
            if (GhostSpawner.IsEndgame)
            {
                // Try to acquire the closest player and switch to chase if one is found
                _currentTarget = DetectPlayer();
                if (_currentTarget != null)
                {
                    _currentState = GhostState.Chase;
                }
                else
                {
                    _currentState = GhostState.Patrol;
                }
            }
            else
            {
                return;
            }
        }

        //Light overrides all states (except vacuuming)
        if (_isInLight)
        {
            
        }

        //Don't override attacking state (Unless when in player flashlight)
        if (_currentState == GhostState.Attack || _currentState == GhostState.Chase)
            return;

    }

    private void DoAnimation()
    {
        float newY =
            _bobBaseHeight +
            Mathf.Sin(Time.time * _bobSpeed) * _bobAmount;

        GhostModel.transform.localPosition =
            new Vector3(0, newY, 0);
    }

    #region Player Detection

    private Transform DetectPlayer()
    {
        // If the game is in endgame mode, always target the closest alive player from the cached player list
        if (GhostSpawner.IsEndgame)
        {
            Transform closestTransform = null;
            float closestDistance = float.MaxValue;

            foreach (var playerObj in _playerList)
            {
                if (playerObj == null) continue;

                PlayerHealth hp = playerObj.GetComponent<PlayerHealth>();
                if (hp == null || !hp.IsAlive) continue;

                float distance = Vector3.Distance(transform.position, playerObj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTransform = playerObj.transform;
                }
            }

            return closestTransform;
        }

        Debug.Log("looking for player");
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            _detectionRadius,
            _friendlyLayer.value
        );

        if (hits == null || hits.Length == 0)
            return null;

        Transform closest = null;
        float closestDist = float.MaxValue;

        LayerMask combinedMask = _friendlyLayer | _worldLayer;

        foreach (var hit in hits)
        {
            PlayerHealth hp = hit.GetComponent<PlayerHealth>();
            if (hp == null || !hp.IsAlive) continue;

            Vector3 direction = hit.transform.position - transform.position;
            float distance = direction.magnitude;

            if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit rayHit, distance, combinedMask))
            {
                if (((1 << rayHit.collider.gameObject.layer) & _friendlyLayer.value) == 0)
                {
                    continue;
                }
            }

            if (distance < closestDist)
            {
                closestDist = distance;
                closest = hit.transform;
            }
        }

        return closest;
    }

    #endregion

    #region Patrolling

    //Patrolling Behavior
    private void DoPatrol()
    {
        if (PatrolPoints == null || PatrolPoints.Count == 0)
            return;

        UpdateGhostAlpha(_ghostVisibleAlpha);
        UpdateGhostEmission(0f);
        UpdateGhostScale(1f);
        UpdateGhostLighting(_patrolLight, _patrolLightMaxIntensity);
        UpdateGhostLighting(_attackLight, 0f);
        UpdateSweatParticles(false, 0f);
        _face.sprite = _facePatrolling;

        Vector3 targetPos = PatrolPoints[_patrolIndex];

        if (_waitTimer <= 0)
        {
            MoveTowards(targetPos, _patrolSpeed);
        }
        else
        {
            MoveTowards(targetPos, 0);
            _waitTimer -= Time.fixedDeltaTime;
        }

        //If close enough to a patrol point, switch to a new one
        if (Vector3.Distance(transform.position, targetPos) < 0.25f)
        {
            if (PatrolPoints.Count > 1)
            {
                int newIndex = _patrolIndex;

                //Picks a new point that is not the same point
                while (newIndex == _patrolIndex)
                {
                    newIndex = Random.Range(0, PatrolPoints.Count);
                }

                _patrolIndex = newIndex;
            }
            else    //If there is (by any chance) only 1 point, it always picks that point
            {
                _patrolIndex = 0;
            }

            _waitTimer = Random.Range(_waitDuration.x, _waitDuration.y);
        }


        Transform player = DetectPlayer();

        if (player != null)
        {
            _currentTarget = player;
            _stareTime = 0f;
            _currentVelocity = Vector3.zero;
            _currentState = GhostState.Stare;
            return;
        }
    }

    #endregion

    private void DoStare()
    {
        if (_currentTarget == null)
        {
            _currentState = GhostState.Patrol;
            return;
        }

        UpdateGhostAlpha(_ghostVisibleAlpha);
        UpdateGhostEmission(0f);
        UpdateGhostScale(1f);
        UpdateGhostLighting(_patrolLight, _patrolLightMaxIntensity * 0.5f);
        UpdateGhostLighting(_attackLight, 0f);
        UpdateSweatParticles(false, 0f);
        _face.sprite = _faceStare;

        _currentVelocity = Vector3.zero;

        LookTowards(_currentTarget.position);

        _stareTime += Time.fixedDeltaTime;

        if (_stareTime >= _stareDuration)
        {
            _stareTime = 0f;
            _currentState = GhostState.Chase;
            RandomSoundEffect();
        }
    }

    #region Chase

    //Chasing behavior
    private void DoChase()
    {
        if (_currentTarget == null)
        {
            _currentState = GhostState.Patrol;
            _chaseTimer = 0f;
            return;
        }

        _chaseTimer += Time.deltaTime;

        if (_chaseTimer >= _chaseDuration)
        {
            GhostStartFleeing();
            return;
        }

        float distance = Vector3.Distance(transform.position, _currentTarget.position);

        float chaseSpeed = _minChaseSpeed;

        //If the ghost is further than the distance for the minimum chasing speed, the chaseSpeed (inverse)Lerps between the max and minimum chasing speed
        if (distance > _distanceMinChaseSpeed)
        {
            float normalizedDistance = Mathf.InverseLerp(
                _distanceMinChaseSpeed,
                _detectionRadius,
                distance
            );

            chaseSpeed = Mathf.Lerp(_minChaseSpeed, _maxChaseSpeed, normalizedDistance);
        }

        // During endgame, prevent ghosts from reaching full top speed when far away
        if (GhostSpawner.IsEndgame)
        {
            // Cap chase speed to halfway between min and max to avoid instant arrivals
            float capT = 0.3f;
            float cappedMax = Mathf.Lerp(_minChaseSpeed, _maxChaseSpeed, capT);
            chaseSpeed = Mathf.Min(chaseSpeed, cappedMax);
        }

        float requiredTransition = Mathf.InverseLerp(
            _distanceTransitionStart,
            _attackRange,
            distance
        );

        UpdateGhostAlpha(_ghostSneakyAlpha + (_ghostVisibleAlpha - _ghostSneakyAlpha) * requiredTransition);
        UpdateGhostEmission(requiredTransition);
        UpdateGhostScale(1f + requiredTransition * 0.25f);
        UpdateGhostLighting(_patrolLight, 0f);
        UpdateGhostLighting(_attackLight, _attackLightMaxIntensity * requiredTransition);
        UpdateSweatParticles(false, 0f);
        _face.sprite = _faceChase;

        MoveTowards(_currentTarget.position, chaseSpeed);

        //If close enough to player --> Attacking state
        if (distance <= _attackRange)
        {
            StartAttack();
        }
    }

    #endregion

    #region Flashlight Behavior

    //Called continuously from the flashlight trigger when the ghost is in said trigger
    public void StayInLight(Transform lightSource)
    {
        if (lightSource == null) return;

        if (_currentState == GhostState.Fleeing || _currentState == GhostState.Vacuumed)
            return;


        Vector3 targetPos = transform.position + Vector3.up * rayOffsetHeight;
        Vector3 direction = targetPos - lightSource.position;
        float distance = direction.magnitude;


        if (Physics.Raycast(lightSource.position, direction.normalized, out RaycastHit hit, distance, _worldLayer))
        {
            return;
        }


        _isInLight = true;
        _currentLightSource = lightSource;
        _outOfLightTimer = 0f;

        _timeInLight += Time.deltaTime;

        float t = Mathf.Clamp01(_timeInLight / _timeInLightBeforeFleeing);
        float sweatAmount = Mathf.Lerp(0f, 5f, t);

        UpdateSweatParticles(true, sweatAmount);

        if (_timeInLight >= _timeInLightBeforeFleeing)
        {
            if (_currentState == GhostState.Attack)
            {
                ReleasePlayer();
            }

            GhostStartFleeing();
            return;
        }

        //Reset chase timeout counter when a ghost is scared by light
        _chaseTimer = 0f;

        if (_currentState == GhostState.Attack)
        {
            _attackFrozenByLight = true;
            _currentVelocity = Vector3.zero;
            return;
        }
        Debug.Log("Gotta hiss");
        PlaySFX(_hiss);
        _currentState = GhostState.InLight;
    }

    private void BehaviorInLight()
    {
        if (_currentLightSource != null)
        {
            _outOfLightTimer = 0f;
        }
        else
        {
            _outOfLightTimer += Time.deltaTime;
        }

        if (!_isInLight || _outOfLightTimer > _lightFearDuration)
        {
            StopBeingInLight();
            return;
        }

        UpdateGhostAlpha(_ghostVisibleAlpha);
        UpdateGhostEmission(0f);
        UpdateGhostScale(1f);
        UpdateGhostLighting(_patrolLight, 0.5f);
        UpdateGhostLighting(_attackLight, 0f);
        _face.sprite = _faceInLight;

        Vector3 awayDirection = GetDirectionAwayFromLight();

        MoveInDirection(awayDirection, _lightRetreatSpeed);

        // Require StayInLight() to refresh this again next frame
        _currentLightSource = null;
    }

    private void StopBeingInLight()
    {
        _isInLight = false;
        _currentLightSource = null;
        _attackFrozenByLight = false;
        _timeInLight = 0f;
        _attackScareOffTimer = 0f;

        if (_currentTarget != null)
        {
            _currentState = GhostState.Chase;
        }
        else
        {
            _currentState = GhostState.Patrol;
        }
    }

    private Vector3 GetDirectionAwayFromLight()
    {
        Vector3 awayDirection;

        if (_currentLightSource != null)
        {
            awayDirection = transform.position - _currentLightSource.position;
        }
        else
        {
            awayDirection = -transform.forward;
        }

        awayDirection.y = 0f;

        return awayDirection.normalized;
    }

    #endregion

    #region Fleeing

    private void GhostStartFleeing()
    {
        // Do not allow fleeing once the endgame has started
        if (GhostSpawner.IsEndgame)
        {
            return;
        }
        _poof.Play();

        _currentTarget = null;
        _attackingTarget = null;

        _chaseTimer = 0f;
        _timeInLight = 0f;

        _isInLight = false;
        _currentLightSource = null;
        _outOfLightTimer = 0f;

        _attackFrozenByLight = false;
        _attackScareOffTimer = 0f;

        _currentVelocity = Vector3.zero;

        UpdateGhostAlpha(0f);
        UpdateGhostEmission(0f);

        _waitTimer = Random.Range(_waitDuration.x, _waitDuration.y);

        _currentState = GhostState.Fleeing;
        _previouslyFleeing = true;
    }

    private void DoFleeing()
    {
        if (PatrolPoints == null || PatrolPoints.Count == 0)
        {
            _currentState = GhostState.Patrol;
            _previouslyFleeing = false;
            return;
        }

        _isInLight = false;
        _currentLightSource = null;
        _currentTarget = null;

        UpdateGhostAlpha(0f);
        UpdateGhostEmission(0f);
        UpdateGhostScale(0.5f); 
        UpdateGhostLighting(_patrolLight, _patrolLightMaxIntensity * 0.5f);
        UpdateGhostLighting(_attackLight, 0f);
        UpdateSweatParticles(false, 0f);
        _face.sprite = _faceFleeing;

        Vector3 targetPos = PatrolPoints[_patrolIndex];

        MoveTowards(targetPos, _fleeingSpeed);

        if (Vector3.Distance(transform.position, targetPos) < 0.25f)
        {
            _currentVelocity = Vector3.zero;

            if (_waitTimer > 0)
            {
                MoveTowards(targetPos, 0);
                _waitTimer -= Time.fixedDeltaTime;
            }
            else
            {
                _appear.Play();
                _currentState = GhostState.Patrol;
                _previouslyFleeing = false;
            }
        }
    }

    #endregion

    #region Vacuum

    //Function called when ghost is within the vacuum's suction trigger
    public void ApplyVacuumInfluence(Transform suctionPoint, bool isPrimaryTarget, bool isSuperVacuum, float maxDistance)
    {
        if (suctionPoint == null) return;

        //if (_currentState == GhostState.Fleeing)
        //{
        //    return;
        //}

        Debug.Log("Ghost is under influence.");

        if (_currentState == GhostState.Attack)
        {
            ReleasePlayer();
        }

        if (_currentState != GhostState.Vacuumed)
        {
            _vacuumSuckTimer = 0f;
            _currentVelocity = Vector3.zero;
        }

        _currentVacuumPoint = suctionPoint;
        _vacuumMaxDistance = maxDistance;
        _lastVacuumRefreshTime = Time.time;

        _currentState = GhostState.Vacuumed;

        _isBeingSuperVacuumed = isSuperVacuum;
    }

    //Behavior when the ghost is being sucked towards the vacuum
    private void DoVacuum()
    {
        if (_currentVacuumPoint == null)
        {
            StopBeingVacuumed();
            return;
        }

        if (Time.time - _lastVacuumRefreshTime > _vacuumRefreshGrace)
        {
            StopBeingVacuumed();
            return;
        }

        UpdateGhostEmission(0f);
        UpdateGhostAlpha(_ghostVisibleAlpha);
        UpdateGhostLighting(_patrolLight, 1f);
        UpdateGhostLighting(_attackLight, 0f);
        UpdateSweatParticles(true, 5f);
        _face.sprite = _faceVacuumed;

        float suckDuration = _isBeingSuperVacuumed ? _superVacuumSuckDuration : _vacuumSuckDuration;

        if (_vacuumSuckTimer < suckDuration)
        {
            _vacuumSuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            _vacuumSuckTimer = suckDuration;
        }

        float vacuumPercent = Mathf.Clamp01(_vacuumSuckTimer / suckDuration);
        float easedPercent = EaseInQuad(vacuumPercent);

        float targetDistanceAlongBeam = Mathf.Lerp(
            _vacuumMaxDistance,
            0f,
            easedPercent
        );

        float distanceFromSuctionPoint = Vector3.Distance(
            transform.position,
            _currentVacuumPoint.position
        );

        float shrinkPercent = Mathf.Clamp01(
            distanceFromSuctionPoint / _vacuumMaxDistance
        );

        UpdateGhostScale(shrinkPercent);

        Vector3 vacuumForward = _currentVacuumPoint.forward;
        vacuumForward.y = 0f;
        vacuumForward.Normalize();

        Vector3 targetPointOnBeam = _currentVacuumPoint.position + vacuumForward * targetDistanceAlongBeam;

        Vector3 toTargetPoint = targetPointOnBeam - transform.position;
        toTargetPoint.y = 0f;

        float distanceToTargetPoint = toTargetPoint.magnitude;

        float normalizedDistanceToPoint = Mathf.Clamp01(
            distanceToTargetPoint / _vacuumMaxDistance
        );

        float moveSpeed;

        if (vacuumPercent >= 1f)
        {
            moveSpeed = _vacuumMaxMoveSpeed;
        }
        else
        {
            moveSpeed = Mathf.Clamp(
                distanceToTargetPoint * _vacuumDistanceSpeedMultiplier,
                0f,
                _vacuumMaxMoveSpeed
            );
        }

        Vector3 moveDirection = toTargetPoint.normalized;

        MoveInDirection(moveDirection, moveSpeed, true);

        if (vacuumPercent >= 1f && distanceToTargetPoint <= _vacuumDistanceDeadZone)
        {
            KillGhost();
        }
    }

    private void StopBeingVacuumed()
    {
        _currentVacuumPoint = null;
        _vacuumSuckTimer = 0f;
        _currentVelocity = Vector3.zero;

        UpdateSweatParticles(false, 0f);

        if (_ghostRendererComponent != null)
        {
            _ghostRendererComponent.transform.localScale = _ghostOriginalScale;
        }

        if (_previouslyFleeing)
        {
            GhostStartFleeing();
            return;
        }
        else
        {
            _currentState = GhostState.Patrol;
            return;
        }
    }

    private float EaseInQuad(float x)
    {
        return x * x;
    }

    private void KillGhost()
    {
        ForceReleasePlayer();

        if (GhostSpawner.IsLastGhost())
        {
            Vector3 launchDirection = Vector3.forward;

            if (_currentVacuumPoint != null)
            {
                launchDirection = _currentVacuumPoint.forward;
                launchDirection.y = 0f;
                launchDirection.Normalize();
            }

            SpawnSpecialKey(_currentVacuumPoint.position, launchDirection);
        }

        PlayDeathParticlesDetached();

        GhostKilled?.Invoke();

        Destroy(gameObject);
    }

    private void SpawnSpecialKey(Vector3 position, Vector3 direction)
    {
        if (_specialKeyPrefab == null)
        {
            Debug.LogWarning("Special key prefab not assigned!");
            return;
        }

        GameObject key = Instantiate(
            _specialKeyPrefab,
            position + Vector3.up,
            Quaternion.identity
        );

        Rigidbody rb = key.GetComponent<Rigidbody>();

        if (rb != null)
        {
            float launchForce = 10f;

            rb.AddForce(
                (direction + Vector3.up * 0.5f).normalized * launchForce,
                ForceMode.Impulse
            );
        }

        key.GetComponent<ItemScript>().MakeSpecial(true);
    }

    #endregion

    #region Attack

    private void StartAttack()
    {
        UpdateGhostAlpha(_ghostSneakyAlpha);
        UpdateGhostEmission(1f);

        _currentState = GhostState.Attack;
        _attackingTarget = _currentTarget;
        _attackedPlayerRumble = _attackingTarget.GetComponent<PlayerControllerRumble>();
        
        _attackTimer = _attackDuration;
        _currentVelocity = Vector3.zero;

        _chaseTimer = 0f;
        PlayerHealth hp = _attackingTarget.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            _ownsAttackSlider = hp.TryClaimAttackUI(this);
            _attackUiOwner = _ownsAttackSlider ? hp : null;

            if (_ownsAttackSlider)
            {
                ShowAttackSlider(hp);
            }

            hp.DisablePlayer();

            _disabledPlayer = hp;
            _playerDisabledByThisGhost = true;
        }
    }

    //Attacking behavior
    private void DoAttack()
    {
        if (_attackingTarget == null)
        {
            ReleasePlayer();
            GhostStartFleeing();
            return;
        }

        UpdateGhostAlpha(_ghostSneakyAlpha);
        UpdateGhostScale(1.25f);
        UpdateGhostLighting(_patrolLight, 0f);
        UpdateSweatParticles(false, 0);
        _face.sprite = _faceAttack;

        //Keep ghost locked on player position while attacking
        Vector3 playerPos = _attackingTarget.position;
        transform.position = new Vector3(playerPos.x, transform.position.y, playerPos.z);
        _currentVelocity = Vector3.zero;

        //If the attacking ghost is in a flashlight
        if (_attackFrozenByLight)
        {
            _attackScareOffTimer += Time.deltaTime;

            //Slowly decrease the transation back to normal
            float requiredTransition = 1f - Mathf.InverseLerp(0f, _attackScareOffDuration, _attackScareOffTimer);
            UpdateGhostEmission(requiredTransition);
            UpdateGhostLighting(_attackLight, _attackLightMaxIntensity * requiredTransition);

            //Controller rumble gets weaker over time
            if (_attackedPlayerRumble != null)
            {
                _attackedPlayerRumble.Rumble(requiredTransition);
            }

            //StayInLight() must be called again next frame to keep counting.
            _attackFrozenByLight = false;
            _currentLightSource = null;
            _isInLight = false;

            //Player is eventually released
            if (_attackScareOffTimer >= _attackScareOffDuration)
            {
                if (_attackedPlayerRumble != null)
                {
                    _attackedPlayerRumble.StopRumble();
                }

                ReleasePlayer();
                GhostStartFleeing();
            }

            return;
        }

        //If the flashlight stopped before the player was released,
        //first count the release timer back to the start before attacking again
        if (_attackScareOffTimer > 0f)
        {
            _attackScareOffTimer -= Time.deltaTime;

            if (_attackScareOffTimer < 0f)
            {
                _attackScareOffTimer = 0f;
            }

            float requiredTransition = 1f - Mathf.InverseLerp(0f, _attackScareOffDuration, _attackScareOffTimer);
            UpdateGhostEmission(requiredTransition);
            UpdateGhostLighting(_attackLight, _attackLightMaxIntensity * requiredTransition);

            if (_attackedPlayerRumble != null)
            {
                _attackedPlayerRumble.Rumble(requiredTransition);
            }

            return;
        }

        //No flashlighs = attack as normal
        _attackScareOffTimer = 0f;

        //Transition = max (evil)
        UpdateGhostEmission(1f);
        UpdateGhostLighting(_attackLight, _attackLightMaxIntensity);

        //Rumble the controller
        if (_attackedPlayerRumble != null)
        {
            _attackedPlayerRumble.Rumble(1f);
        }

        //Count down until death
        _attackTimer -= Time.deltaTime;
        UpdateAttackSlider();

        if (_attackTimer <= 0f)
        {
            PlayerHealth health = _attackingTarget.GetComponent<PlayerHealth>();

            if (health != null)
            {
                if (_attackedPlayerRumble != null)
                {
                    _attackedPlayerRumble.StopRumble();
                }

                health.KillPlayer();

                if (_disabledPlayer == health)
                {
                    _disabledPlayer = null;
                    _playerDisabledByThisGhost = false;
                }
            }

            PlayerPickUp pickUp = _attackingTarget.GetComponentInChildren<PlayerPickUp>();
            if (pickUp != null)
            {
                pickUp.ForceDropHeldItem();
            }

            HideAttackSlider();
            _attackingTarget = null;
            _currentTarget = null;
            GhostStartFleeing();
        }

        _attackFrozenByLight = false;
        _currentLightSource = null;
        _isInLight = false;
    }

    //Releasing the player (if ghost is caught in light while trying to kill one)
    private void ReleasePlayer()
    {
        _attackingTarget = null;
        _attackTimer = 0f;
        _attackScareOffTimer = 0f;
        _attackFrozenByLight = false;
        _currentVelocity = Vector3.zero;
        HideAttackSlider();

        if (_playerDisabledByThisGhost && _disabledPlayer != null)
        {
            _disabledPlayer.EnablePlayer();
            _disabledPlayer = null;
            _playerDisabledByThisGhost = false;
        }

        if (_attackedPlayerRumble != null)
        {
            _attackedPlayerRumble.StopRumble();
            _attackedPlayerRumble = null;
        }
    }

    public void ForceReleasePlayer()
    {
        HideAttackSlider();
        ReleasePlayer();
    }

    #endregion

    #region Ghost visual changes

    private void UpdateGhostEmission(float targetEmission)
    {
        targetEmission = Mathf.Clamp01(targetEmission);

        _emissionT = Mathf.Lerp(
            _emissionT,
            targetEmission,
            _shaderTransitionLerpSpeed * Time.deltaTime
        );

        SetGhostEmission(_emissionT);
    }

    private void SetGhostEmission(float value)
    {
        if (_ghostRenderer == null)
        {
            Debug.Log("Ghost renderer is null.");
            return;
        }

        _ghostRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(_emissionPropertyID, value);
        _ghostRenderer.SetPropertyBlock(_propertyBlock);
    }


    private void UpdateGhostAlpha(float targetAlpha)
    {
        targetAlpha = Mathf.Clamp01(targetAlpha);

        _alphaT = Mathf.Lerp(
            _alphaT,
            targetAlpha,
            _shaderTransitionLerpSpeed * Time.deltaTime
        );

        SetGhostAlpha(_alphaT);
    }

    private void SetGhostAlpha(float value)
    {
        if (_ghostRenderer == null)
        {
            Debug.Log("Ghost renderer is null.");
            return;
        }

        _ghostRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(_alphaPropertyID, value);
        _ghostRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void UpdateSweatParticles(bool shouldPlay, float emissionRate)
    {
        if (_sweatParticles == null)
            return;

        ParticleSystem.EmissionModule emission = _sweatParticles.emission;
        emission.rateOverTime = emissionRate;

        if (shouldPlay)
        {
            if (!_sweatParticles.isPlaying)
                _sweatParticles.Play();
        }
        else
        {
            if (_sweatParticles.isPlaying)
                _sweatParticles.Stop();
        }
    }

    private void UpdateGhostScale(float sizeModifier)
    {
        if (_ghostRendererComponent == null) return;

        Vector3 targetScale = _ghostOriginalScale * sizeModifier;

        _ghostRendererComponent.transform.localScale = Vector3.Lerp(
            _ghostRendererComponent.transform.localScale,
            targetScale,
            _ghostScaleLerpSpeed * Time.deltaTime
        );
    }

    private void SetGhostScale(float sizeModifier)
    {
        if (_ghostRendererComponent == null) return;

        _ghostRendererComponent.transform.localScale = _ghostOriginalScale * sizeModifier;
    }

    private void UpdateGhostLighting(Light light, float intensity)
    {
        if (light == null) return;

        light.intensity = Mathf.Lerp(light.intensity, intensity, _lightLerpSpeed * Time.deltaTime);
    }

    private void SetGhostLighting(Light light, float intensity)
    {
        if (light == null) return;

        light.intensity = intensity;
    }

    private void PlayDeathParticlesDetached()
    {
        if (_deathParticles == null) return;

        ParticleSystem particles = Instantiate(
            _deathParticles,
            transform.position,
            Quaternion.identity
        );

        particles.Play();

        float destroyDelay = particles.main.duration + particles.main.startLifetime.constantMax;

        Destroy(particles.gameObject, destroyDelay);
    }

    #endregion

    private void UpdateGhostCollisionLayer()
    {
        if (_solidGhostLayer == -1 || _phaseGhostLayer == -1)
            return;

        if (_currentState == GhostState.Chase || _currentState == GhostState.Patrol || _currentState == GhostState.Fleeing)
        {
            gameObject.layer = _phaseGhostLayer;
        }
        else
        {
            gameObject.layer = _solidGhostLayer;
        }
    }

    #region Movement Base

    private void MoveTowards(Vector3 destination, float speed)
    {
        Vector3 toTarget = destination - transform.position;
        toTarget.y = 0f;

        Vector3 desiredDirection = Vector3.zero;

        if (toTarget.sqrMagnitude > 0.0001f)
            desiredDirection = toTarget.normalized;

        MoveInDirection(desiredDirection, speed);
    }

    private void MoveInDirection(Vector3 direction, float speed, bool rotateOtherDirection = false)
    {
        direction.y = 0f;

        Vector3 desiredVelocity = direction * speed;

        float rate = _acceleration;

        _currentVelocity = Vector3.MoveTowards(
            _currentVelocity,
            desiredVelocity,
            rate * Time.deltaTime
        );

        Vector3 movement = _currentVelocity * Time.deltaTime;
        transform.position += movement;

        Vector3 lookDir = _currentVelocity;
        lookDir.y = 0f;

        if (rotateOtherDirection)
            lookDir = -lookDir;

        if (lookDir.magnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                _rotationSpeed * Time.deltaTime
            );
        }
    }

    private void LookTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime
        );
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
    }

    private void RandomSoundEffect()
    {
        float lowPitchRange = .85f;
        float highPitchRange = 1.15f;
        int randomIndex = Random.Range(0, _ghostAttacks.Length);
        float randomPitch = Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = Random.Range(0.8f, 1f);
        SoundLimiter.Instance.PlaySound(
            "Ghost",
            _effectsSource,
            _ghostAttacks[randomIndex],
            randomPitch
        );
    }

    private void PlaySFX(AudioClip clip)
    {
        float lowPitchRange = .85f;
        float highPitchRange = 1.15f;
        float randomPitch = Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = Random.Range(0.8f, 1f);
        Debug.Log("Hissing sound");
        SoundLimiter.Instance.PlaySound(
            "GhostHiss",
            _effectsSource,
            clip,
            randomPitch
        );
    }

    private void ShowAttackSlider(PlayerHealth player)
    {
        if (_slider == null || _attackSlider == null) return;

        _slider.SetActive(true);

        _attackSlider.maxValue = _attackDuration;
        _attackSlider.value = _attackDuration;

        PlayerColor playerColor = player.GetComponent<PlayerColor>();

        if (playerColor != null && _sliderFill != null)
        {
            _sliderFill.color = playerColor.GetColor();
        }
    }

    private void UpdateAttackSlider()
    {
        if (!_ownsAttackSlider || _attackSlider == null) return;

        _attackSlider.value = _attackTimer;
    }

    private void HideAttackSlider()
    {
        if (_ownsAttackSlider && _attackUiOwner != null)
            _attackUiOwner.ReleaseAttackUI(this);
        
        _attackUiOwner = null;
        _ownsAttackSlider = false;

        if (_slider == null || _attackSlider == null) return;

        _attackSlider.value = _attackDuration;

        if (_sliderFill != null)
            _sliderFill.color = Color.white;

        _slider.SetActive(false);
    }
    public void UpdatePlayerList()
    {
        // 1. Find all GameObjects with the tag "Player"
        // This returns an array: GameObject[]
        GameObject[] playersFound = GameObject.FindGameObjectsWithTag("Player");

        // 2. Clear the current list and add the newly found players
        _playerList.Clear();
        _playerList.AddRange(playersFound);

        Debug.Log($"Found {_playerList.Count} players in the scene.");
    }
}