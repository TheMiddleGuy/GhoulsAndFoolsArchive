using UnityEngine;

// UI follows a target, faces camera, optionally scales with distance,
// and can maintain a consistent screen-space height offset.
public class UIFollowWorldIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _target;
    [SerializeField] private Camera _camera;

    [Header("Positioning")]
    [SerializeField] private Vector3 _worldAnchorOffset = Vector3.up * 2f;

    [Tooltip("Vertical offset in screen space")]
    [SerializeField] private float _screenHeightOffset = 10f;

    [SerializeField] private bool _useScreenSpaceOffset = true;

    [Header("Scaling")]
    [SerializeField] private bool _scaleWithDistance = true;

    [SerializeField] private float _referenceDistance = 10f;

    [SerializeField] private float _minScaleMultiplier = 0.5f;
    [SerializeField] private float _maxScaleMultiplier = 2f;

    private Vector3 _originalScale;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        _originalScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (_camera == null) return;

        UpdatePosition();
        FaceCamera();

        if (_scaleWithDistance)
            UpdateScale();
        else
            transform.localScale = _originalScale;
    }

    private void UpdatePosition()
    {
        Vector3 anchorWorldPos = transform.position;

        if (_target != null)
            anchorWorldPos = _target.position + _worldAnchorOffset;

        if (_useScreenSpaceOffset)
        {
            Vector3 screenPos = _camera.WorldToScreenPoint(anchorWorldPos);

            screenPos.y += _screenHeightOffset;

            Vector3 worldPos = _camera.ScreenToWorldPoint(screenPos);
            transform.position = worldPos;
        }
        else
        {
            transform.position = anchorWorldPos;
        }
    }

    private void FaceCamera()
    {
        transform.LookAt(
            transform.position + _camera.transform.rotation * Vector3.back,
            _camera.transform.rotation * Vector3.up
        );
    }

    private void UpdateScale()
    {
        float distance = Vector3.Distance(transform.position, _camera.transform.position);

        float distanceRatio = distance / Mathf.Max(_referenceDistance, 0.001f);

        float multiplier = distanceRatio;
        multiplier = Mathf.Clamp(multiplier, _minScaleMultiplier, _maxScaleMultiplier);

        transform.localScale = _originalScale * multiplier;
    }
}