using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class ItemScript : MonoBehaviour
{
    private bool _carried;
    private Rigidbody _rb;
    private Collider _col;

    private bool _isSpecial = false;

    [Header("Particles")]
    [SerializeField] private ParticleSystem _pickUpParticles;
    [SerializeField] private ParticleSystem _particlesInLight;
    [SerializeField] private ParticleSystem _particlesInDark;
    [SerializeField] private GameObject _specialParticles;
    private bool _isPlaying;

    [Header("Item Shader")]
    [SerializeField] private List<Renderer> _renders;
    [SerializeField] private float _shaderTransitionLerpSpeed = 5f;
    [SerializeField] private string _shaderEmissionProperty = "_emissionT";

    [SerializeField] private float _darkEmissionValue = 0f;
    [SerializeField] private float _holdingEmissionValue = 0.5f;
    [SerializeField] private float _lightEmissionValue = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource _effectsSource;
    [SerializeField] private AudioClip _pickUp;
    [SerializeField] private AudioClip _drop;


    private MaterialPropertyBlock _propertyBlock;
    private int _emissionPropertyID;
    private float _emissionT;

    private float _targetEmmision;

    private event System.Action _keyPickedUp;

    public static event System.Action FlashlightPickedUp;
    public static event System.Action VacuumPickedUp;
    public static event System.Action RepairPickedUp;
    private Vector3 _originalScale;
    public enum ItemTypes
    {
        decor,
        key,
        cross,
        book,
        ductTape,
        repairParts,
        vacuum,
        flashlight
    }


    [Tooltip("The type of item.")]
    [SerializeField] private ItemTypes _type;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        // Store the scale the item had in the editor
        _originalScale = transform.localScale;

        _propertyBlock = new MaterialPropertyBlock();
        _emissionPropertyID = Shader.PropertyToID(_shaderEmissionProperty);

        SetItemEmission(_darkEmissionValue);
        
        _particlesInDark.Play();
    }

    private void Update()
    {
        UpdateItemEmission(_targetEmmision);
    }

    public ItemTypes GetItemType()
    {
        return _type;
    }

    private void PlayPickupParticlesDetached()
    {
        if (_pickUpParticles == null) return;

        ParticleSystem particles = Instantiate(
            _pickUpParticles,
            _pickUpParticles.transform.position,
            _pickUpParticles.transform.rotation
        );

        particles.transform.SetParent(null);
        particles.Play();

        float destroyDelay =
            particles.main.duration +
            particles.main.startLifetime.constantMax;

        Destroy(particles.gameObject, destroyDelay);
    }

    public void Carry()
    {
        PlayPickupParticlesDetached();
        PlaySFX(_pickUp);
        Debug.Log($"{_type.ToString()} picked up!");

        _carried = true;
        _rb.isKinematic = true;
        _col.enabled = false;
        // --- NEW SCALING LOGIC ---
        if (_type == ItemTypes.book || _type == ItemTypes.cross)
        {
            transform.localScale = _originalScale * 0.5f;
        }
        // -------------------------
        if (_type == ItemTypes.vacuum)
        {
            Debug.Log("Vacuum picked up!");
            VacuumPickedUp?.Invoke();
        }

        if( _type == ItemTypes.flashlight) 
        {
            Debug.Log("Flashlight picked up!");
            FlashlightPickedUp?.Invoke(); 
        }

        if (_type == ItemTypes.repairParts)
        {
            Debug.Log("Repair parts picked up!");
            RepairPickedUp?.Invoke();
        }

        if (_isSpecial)
        {
            Debug.Log("Special key picked up... GG!");
            GameFlowManager.Instance.FinishGame(true);
        }

        if (_particlesInLight != null)
            _particlesInLight.Stop();
        if (_particlesInDark != null)
            _particlesInDark.Stop();

        _targetEmmision = _holdingEmissionValue;
        SetItemEmission(_holdingEmissionValue);
    }

    public void UnCarry(Vector3 throwStrenght)
    {
        _carried = false;
        _rb.isKinematic = false;
        _col.enabled = true;
        // --- NEW SCALING LOGIC ---
        // Restore original size when dropped
        transform.localScale = _originalScale;
        // -------------------------
        PlaySFX(_drop);
        _rb.AddForce(throwStrenght, ForceMode.Force);

        _targetEmmision = _darkEmissionValue;
    }

    public bool CheckIfCarried()
    {
        return _carried;
    }

    public void Sparkle()
    {
        if (_carried) return;

        if (_isPlaying) return;

        _isPlaying = true;

        if (_particlesInLight != null)
        {
            _particlesInLight.Play();
        }
        if (_particlesInDark != null)
        {
            _particlesInDark.Stop();
        }

        _targetEmmision = _lightEmissionValue;
    }

    public void StopSparkle()
    {
        if (_carried) return;

        _isPlaying = false;

        if (_particlesInLight != null)
        {
            _particlesInLight.Stop();
        }
        if (_particlesInDark != null)
        {
            _particlesInDark.Play();
        }

        _targetEmmision = _darkEmissionValue;
    }

    private void UpdateItemEmission(float targetEmission)
    {
        targetEmission = Mathf.Clamp01(targetEmission);

        _emissionT = Mathf.Lerp(
            _emissionT,
            targetEmission,
            _shaderTransitionLerpSpeed * Time.deltaTime
        );

        SetItemEmission(_emissionT);
    }

    private void SetItemEmission(float value)
    {
        if (_renders.Count == 0)
        {
            Debug.Log("No renderer assigned to itemScript.");
            return;
        }

        foreach (Renderer renderer in _renders)
        {
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(_emissionPropertyID, value);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }

    public void MakeSpecial(bool isSpecial)
    {
        Debug.Log("This thing got made special");
        _isSpecial = isSpecial;

        // turn on particles
        if(_specialParticles != null)
            _specialParticles.SetActive(true);
    }

    private void PlaySFX(AudioClip clip)
    {
        float lowPitchRange = .85f;
        float highPitchRange = 1.15f;
        float randomPitch = Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = Random.Range(0.8f, 1f);
        SoundLimiter.Instance.PlaySound(
            "Item",
            _effectsSource,
            clip,
            randomPitch
        );
    }
}
