using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class ReviveGhostBehavior : MonoBehaviour
{
    public int PlayerId;
    public Color PlayerColor;
    public GameObject PlayersBody;
    public GameObject ReviveGhostModel;

    [Header("Flee Settings")]
    public float FleeRadius = 10f;
    public int SearchSamples = 12;
    public float DetectionRadius = 15f;
    public LayerMask FriendlyLayer;

    [Header("Tether Settings")]
    public float MaxTetherDistance = 12f;
    public float TetherStiffness = 2.0f;

    [Header("Priority Settings")]
    public float PlayerFearWeight = 6.0f;

    [Header("Minigame Visuals/Speed")]
    [Tooltip("The 'Light' parent object in your hierarchy")]
    public GameObject LightContainer;
    [Tooltip("The 'Point Light' child object")]
    public Light GhostPointLight;

    [SerializeField] private float _minSpeedMultiplier = 0.25f; // How much it slows down at Status 5
    [SerializeField] private float _maxLightRange = 10f;       // Point Light range at Status 5
    [SerializeField] private float _maxContainerScale = 3f;    // Light parent scale at Status 5

    private NavMeshAgent _agent;
    private float _baseSpeed;
    private float _miniGameStatus = 0f;
    public bool IsGrabbed = false;
    private Transform _currentGrabSource;
    public MeshRenderer ghostRenderer;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null) _baseSpeed = _agent.speed;

        // Initialize state
        SetPlayerMiniGameStatus(0f);

        if (PlayerColor != null) SetPlayerColor(PlayerColor);
    }

    void Update()
    {
        if (IsGrabbed && _currentGrabSource != null) return;
        DoAnimation();
        FindBestEscapePoint();
    }

    private void DoAnimation()
    {
        float newY = Mathf.Sin(Time.time * 3) * 0.1f;
        ReviveGhostModel.transform.localPosition = new Vector3(0, newY, 0);
    }

    private void FindBestEscapePoint()
    {
        if (PlayersBody == null || !_agent.enabled) return;

        List<Vector3> playerThreats = GetNearbyPlayersOnly();
        Vector3 bodyPos = PlayersBody.transform.position;

        Vector3 bestPoint = transform.position;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < SearchSamples; i++)
        {
            float angle = i * (2 * Mathf.PI / SearchSamples);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 samplePos = transform.position + direction * FleeRadius;

            if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, FleeRadius, NavMesh.AllAreas))
            {
                float minDistanceToPlayer = float.MaxValue;
                foreach (Vector3 threatPos in playerThreats)
                {
                    float d = Vector3.Distance(hit.position, threatPos);
                    if (d < minDistanceToPlayer) minDistanceToPlayer = d;
                }
                if (playerThreats.Count == 0) minDistanceToPlayer = DetectionRadius;

                float distanceFromBody = Vector3.Distance(hit.position, bodyPos);
                float tetherPenalty = 0f;
                if (distanceFromBody > MaxTetherDistance)
                {
                    tetherPenalty = (distanceFromBody - MaxTetherDistance) * TetherStiffness;
                }

                float currentScore = (minDistanceToPlayer * PlayerFearWeight) + distanceFromBody - tetherPenalty;

                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    bestPoint = hit.position;
                }
            }
        }
        _agent.SetDestination(bestPoint);
    }

    public void SetPlayerMiniGameStatus(float status)
    {
        _miniGameStatus = Mathf.Clamp(status, 0f, 5f);

        // 1. CALCULATE INTENSITY (Normalize 1-5 range to 0-1)
        float intensity = 0f;
        if (_miniGameStatus > 1f)
        {
            intensity = (_miniGameStatus - 1f) / 4f; // 0 at status 1, 1 at status 5
        }

        // 2. HANDLE SPEED
        // The ghost slows down as the dead player gets closer to their target
        if (_agent != null)
        {
            _agent.speed = Mathf.Lerp(_baseSpeed, _baseSpeed * _minSpeedMultiplier, intensity);
        }

        // 3. HANDLE VISUALS & HITBOX
        if (intensity > 0.05f) // Small threshold to avoid flicker
        {
            if (!LightContainer.activeSelf) LightContainer.SetActive(true);

            // Scale the parent 'Light' object 
            // This scales the 'LightRadiusBox' hitbox automatically!
            LightContainer.transform.localScale = Vector3.one * (intensity * _maxContainerScale);

            // Adjust the actual Point Light range
            if (GhostPointLight != null)
            {
                GhostPointLight.range = intensity * _maxLightRange;
                // Optional: boost intensity so it looks brighter as it grows
                GhostPointLight.intensity = intensity * 2f;
            }
        }
        else
        {
            if (LightContainer.activeSelf) LightContainer.SetActive(false);
        }
    }

    public float GetPlayerMiniGameStatus() => _miniGameStatus;

    public void SetPlayerColor(Color color)
    {
        // Get renderer from child if not assigned manually
        if (ghostRenderer == null) ghostRenderer = GetComponentInChildren<MeshRenderer>();
        if (ghostRenderer == null) return;

        Material[] mats = ghostRenderer.materials;

        if (mats.Length > 0)
        {
            // Update Base Color
            if (mats[0].HasProperty("_BaseColor")) mats[0].SetColor("_BaseColor", color);
            else if (mats[0].HasProperty("_Color")) mats[0].SetColor("_Color", color);

            // Update Emission
            if (mats[0].HasProperty("_EmissionColor"))
            {
                mats[0].SetColor("_EmissionColor", color);
                // Ensure emission is actually enabled on the material
                mats[0].EnableKeyword("_EMISSION");
            }

            ghostRenderer.materials = mats;
        }
    }

    private List<Vector3> GetNearbyPlayersOnly()
    {
        List<Vector3> positions = new List<Vector3>();
        Collider[] hits = Physics.OverlapSphere(transform.position, DetectionRadius, FriendlyLayer);

        foreach (var hit in hits)
        {
            if (hit.gameObject == PlayersBody || hit.gameObject == this.gameObject) continue;
            positions.Add(hit.transform.position);
        }
        return positions;
    }

    public void GrabReviveGhost(Transform grabSource)
    {
        IsGrabbed = true;
        _currentGrabSource = grabSource;
        _agent.enabled = false;
    }

    public void ReleaseReviveGhost()
    {
        IsGrabbed = false;
        _currentGrabSource = null;
        _agent.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsGrabbed && other.gameObject == PlayersBody)
        {
            Revive();
        }
    }

    private void Revive()
    {
        PlayerHealth health = PlayersBody.GetComponent<PlayerHealth>();
        if (health != null) health.RevivePlayer();
        Destroy(gameObject);
    }
    
}