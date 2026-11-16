using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform _hinge;
    [SerializeField] private Collider _collider;
    [SerializeField] private bool _colliderOffAfterOpen;
    [SerializeField] private Vector2 _rotationLimits;
    [SerializeField] private float _openSpeed = 5f;

    private Quaternion _targetRotation;
    private bool _open = false;

    private void Start()
    {
        _targetRotation = _hinge.localRotation;
    }

    private void Update()
    {
        _hinge.localRotation = Quaternion.Lerp(
            _hinge.localRotation,
            _targetRotation,
            Time.deltaTime * _openSpeed
        );

        if (_open && _colliderOffAfterOpen)
            _collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_open) return;
        if (!other.CompareTag("Player")) return;

        Vector3 toPlayer = other.transform.position - transform.position;

        float side = Vector3.Dot(transform.right, toPlayer);

        float targetAngle = side >= 0f ? _rotationLimits.y : _rotationLimits.x;

        _targetRotation = Quaternion.Euler(
            _hinge.localEulerAngles.x,
            targetAngle,
            _hinge.localEulerAngles.z
        );

        _open = true;
    }
}