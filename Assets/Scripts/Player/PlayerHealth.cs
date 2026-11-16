using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    private InputActionMap _playerActionMap;
    public GameObject FlashLightCone;
    public GameObject ReviveGhostPrefab;

    [Header("Grab Settings")]
    public float GrabRange = 3.0f;
    public Transform GrabHoldPoint; // Optional: Create an empty child object where the ghost floats

    public bool IsAlive = true;
    public bool IsMobile = true;
    private bool _isFlashlightOn = false;

    [SerializeField] private PlayerPickUp _pickUpScript;

    [Header("Indicator Plane")]
    [SerializeField] private GameObject _indicator;

    [Header("Sound")]
    [SerializeField] private AudioClip[] _deathSFX;
    [SerializeField] private AudioSource _effectsSource;
    [SerializeField] private AudioClip _flashlightOn;
    [SerializeField] private AudioClip _flashlightOff;
    [SerializeField] private AudioClip _reviveSFX; // Added for the revive sound

    // --- Dan's addition ---
    public static event System.Action<PlayerHealth> PlayerStateChanged;
    private int _playerIndex;
    public GameObject CurrentReviveGhost;
    public int PlayerIndex
    {
        get { return _playerIndex; }
        set { _playerIndex = value; }
    }

    private BasicGhostBehavior _attackingGhost;

    void Awake()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        _playerActionMap = input.actions.FindActionMap("Player");

        //_playerActionMap.FindAction("UseItem").started += context => UseItem();
        //_playerActionMap.FindAction("PickUp").started += context => GrabGhost();
        FlashLightCone.SetActive(_isFlashlightOn);
    }

    // --- Existing Methods ---

    public void KillPlayer()
    {
        if (!IsAlive) return;
        //if (_heldGhost != null) _heldGhost.ReleaseReviveGhost();
        GetComponent<PlayerColor>().SetColorDead();
        IsAlive = false;
        DisablePlayer();

        RandomSoundEffect();
        _indicator.SetActive(false);
        PlayerStateChanged?.Invoke(this);

        GameObject reviveGhost = Instantiate(ReviveGhostPrefab);
        CurrentReviveGhost = reviveGhost;
        // 1. Get the Agent and the Ghost script
        NavMeshAgent ghostAgent = reviveGhost.GetComponent<NavMeshAgent>();
        ReviveGhostBehavior reviveScript = reviveGhost.GetComponent<ReviveGhostBehavior>();

        // 2. Use Warp to move the agent correctly
        if (ghostAgent != null)
        {
            ghostAgent.Warp(transform.position);
        }
        else
        {
            // Fallback if there is no agent
            reviveGhost.transform.position = transform.position;
        }

        // 3. Set up the rest of the data
        reviveScript.PlayerId = GetComponent<PlayerColor>().PlayerId;
        reviveScript.PlayerColor = GetComponent<PlayerColor>().GetColor();
        reviveScript.SetPlayerColor(reviveScript.PlayerColor);
        reviveScript.PlayersBody = gameObject;
    }

    public void RevivePlayer()
    {
        IsAlive = true;
        EnablePlayer();
        GetComponent<PlayerColor>().SetColorAlive();

        // --- Play Revive Sound ---
        PlayReviveSFX();

        // The ghost destroys itself in its own script via Revive()
        _indicator.SetActive(true);
        PlayerStateChanged?.Invoke(this);
    }

    public void DisablePlayer()
    {
        IsMobile = false;
        GetComponent<BasicMovement>().Dead = true;
        _pickUpScript.IsDead = true;
    }

    public void EnablePlayer()
    {
        IsMobile = true;
        GetComponent<BasicMovement>().Dead = false;
        _pickUpScript.IsDead = false;
    }

    public void TurnOnFlashLight()
    {
        PlayFlashlightSFX(true);
        FlashLightCone.SetActive(true);
    }
    public void TurnOffFlashLight()
    {
        PlayFlashlightSFX(false);
        FlashLightCone.SetActive(false);
    }

    public void SetFlashlightState(bool state)
    {
        _isFlashlightOn = state;
        if (_isFlashlightOn) TurnOnFlashLight();
        else TurnOffFlashLight();
    }

    public void ToggleFlashlight()
    {
        SetFlashlightState(!_isFlashlightOn);
    }

    // Helpful for debugging the grab range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GrabRange);
    }

    private void RandomSoundEffect()
    {
        float lowPitchRange = .85f;
        float highPitchRange = 1.15f;
        int randomIndex = Random.Range(0, _deathSFX.Length);
        float randomPitch = Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = Random.Range(0.8f, 1f);
        SoundLimiter.Instance.PlaySound(
            "Death",
            _effectsSource,
            _deathSFX[randomIndex],
            randomPitch
        );
    }

    private void PlayFlashlightSFX(bool isOn)
    {
        float lowPitchRange = .85f;
        float highPitchRange = 1.15f;
        float randomPitch = UnityEngine.Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = UnityEngine.Random.Range(0.8f, 1f);
        SoundLimiter.Instance.PlaySound(
            "Flashlight",
            _effectsSource,
            (isOn) ? _flashlightOn : _flashlightOff,
            randomPitch
        );
    }

    private void PlayReviveSFX()
    {
        if (_reviveSFX == null) return;

        float lowPitchRange = .95f;  // Keep the pitch changes tighter so a musical jingle stays clean
        float highPitchRange = 1.05f;
        float randomPitch = Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = Random.Range(0.85f, 1f);

        SoundLimiter.Instance.PlaySound(
            "Revive",
            _effectsSource,
            _reviveSFX,
            randomPitch
        );
    }

    public bool TryClaimAttackUI(BasicGhostBehavior ghost)
    {
        if (_attackingGhost != null && _attackingGhost != ghost)
            return false;

        _attackingGhost = ghost;
        return true;
    }

    public void ReleaseAttackUI(BasicGhostBehavior ghost)
    {
        if (_attackingGhost == ghost)
            _attackingGhost = null;
    }
}