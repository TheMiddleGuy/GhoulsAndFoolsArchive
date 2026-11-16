using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MyButtonScript : MonoBehaviour
{
    // --- The whole selection shebang ---
    [SerializeField]
    private Image _buttonBackground;

    [SerializeField]
    private GameObject _indicator;

    [Header("Find the function to execute")]
    [SerializeField]
    private UnityEvent _onClick;

    [Header("Sounds")]
    [SerializeField] private AudioSource _effectsSource;
    [SerializeField] private AudioClip _click;
    [SerializeField] private AudioClip _skip;

    private bool _isSelected = false;
    private event System.Action _selectionChanged;
    public void SetSelect(bool selection)
    {
        if (!selection) PlaySFX(false);

        _isSelected = selection;
        _indicator.SetActive(selection);
        _selectionChanged?.Invoke();
    }

    private void SetButtonColor()
    {
        //_buttonBackground.color = _isSelected ? Color.yellow : Color.white;
    }

    private void OnEnable()
    {
        _selectionChanged += SetButtonColor;
        SetButtonColor();
    }

    private void OnDisable()
    {
        _selectionChanged -= SetButtonColor;
    }

    public void Execute()
    {
        PlaySFX(true);
        _onClick?.Invoke();
    }

    private void PlaySFX(bool isClick)
    {
        float lowPitchRange = .85f;
        float highPitchRange = 1.15f;
        float randomPitch = UnityEngine.Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = UnityEngine.Random.Range(0.8f, 1f);
        SoundLimiter.Instance.PlaySound(
            "Button",
            _effectsSource,
            (isClick) ? _click : _skip,
            randomPitch
        );
    }
}
