using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingDoor : MonoBehaviour
{
    private Rigidbody _rb;
    private Quaternion _startingRotation;

    // Tracks which side of the starting angle the door was on last frame
    // 0 = Exactly at start, 1 = Swung Positive, -1 = Swung Negative
    private int _lastSide = 0;

    [Header("Audio Setup")]
    [Tooltip("Drag the SPECIFIC AudioSource dedicated to moving/creaking here")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Audio Settings")]
    [Tooltip("The audio clip that plays ONCE as the door crosses the center angle")]
    [SerializeField] private AudioClip _doorCrossingClip;

    [Tooltip("How fast the door needs to be moving to trigger the crossing sound")]
    [SerializeField] private float _movementThreshold = 0.05f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Fallback to grabbing the first AudioSource if none was assigned in the inspector
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (_audioSource != null)
        {
            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
        }

        _startingRotation = transform.rotation;
    }

    void Update()
    {
        if (_audioSource == null || _doorCrossingClip == null) return;

        // Check if the door is moving fast enough to warrant a sound
        float currentSpeed = _rb.angularVelocity.magnitude + _rb.linearVelocity.magnitude;
        if (currentSpeed < _movementThreshold)
        {
            // Reset side tracker if the door completely stops at the center origin
            if (Quaternion.Angle(_startingRotation, transform.rotation) < 0.1f)
            {
                _lastSide = 0;
            }
            return;
        }

        // Determine which side of the starting rotation the door is on
        int currentSide = GetDoorSide();

        // If the door was at 0 and moves to a side, OR switches sides entirely (crosses center)
        if (currentSide != _lastSide)
        {
            // Play the sound using whatever volume is currently set in the inspector
            _audioSource.PlayOneShot(_doorCrossingClip);

            // Save the side we are currently on
            _lastSide = currentSide;
        }
    }

    private int GetDoorSide()
    {
        // Calculate the relative rotation from the starting angle
        Quaternion relativeRotation = Quaternion.Inverse(_startingRotation) * transform.rotation;

        // Convert to directional angles (-180 to 180) around the Y axis
        float angle = relativeRotation.eulerAngles.y;
        if (angle > 180) angle -= 360;

        // Give a tiny buffer zone right at 0 to avoid physics jitter
        if (Mathf.Abs(angle) < 0.2f) return 0;

        return angle > 0 ? 1 : -1;
    }
}