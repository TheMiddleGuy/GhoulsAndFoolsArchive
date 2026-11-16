using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DeadPlayerUIScaler : MonoBehaviour
{
    private Image _targetImage;

    [Header("Player Setup")]
    [Tooltip("Match this to the PlayerId on the Player's BasicMovement script")]
    public int PlayerId; // Assign 0, 1, 2, or 3 to match the specific player

    [Header("Progress Settings")]
    [SerializeField] private Color _lowProgressColor = new Color(0.8f, 0.8f, 0.1f, 0.5f);
    [SerializeField] private Color _highProgressColor = new Color(0.1f, 0.8f, 0.1f, 0.7f);

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // Listen to the new updated event signature
        BasicMovement.OnProximityStatusChanged += HandleProximityChanged;
        UpdateProgressBar(0f);
    }

    private void OnDisable()
    {
        BasicMovement.OnProximityStatusChanged -= HandleProximityChanged;
    }

    // This method acts as a filter
    private void HandleProximityChanged(int id, float score)
    {
        // If the ID sent by the player doesn't match this UI icon's ID, do nothing!
        if (id != PlayerId) return;

        UpdateProgressBar(score);
    }

    private void UpdateProgressBar(float proximityScore)
    {
        if (_targetImage == null) return;

        float normalizedFill = Mathf.Clamp01(proximityScore / 5f);
        _targetImage.fillAmount = normalizedFill;

        _targetImage.color = Color.Lerp(_lowProgressColor, _highProgressColor, normalizedFill);
    }
}