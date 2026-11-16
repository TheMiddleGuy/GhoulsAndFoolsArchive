using UnityEngine;

public class FlashlightVolumeController : MonoBehaviour
{
    [Header("References")]
    public Transform flashlightTransform;

    [Header("Sphere")]

    [SerializeField] public float sphereRadius = 2.0f;
    [SerializeField] public float sphereSoftness = 1.0f;

    [Header("Beam")]
    [SerializeField] public float beamLength = 8.0f;
    [SerializeField] public float beamStartHalfWidth = 0.5f;
    [SerializeField] public float beamStartHalfHeight = 0.5f;
    [SerializeField] public float beamEndHalfWidth = 3.5f;
    [SerializeField] public float beamEndHalfHeight = 2.5f;
    [SerializeField] public float beamSoftness = 0.5f;

    [Header("Look")]
    [SerializeField] public Color flashColor = new Color(1f, 0.95f, 0.8f);
    [SerializeField] public float flashIntensity = 2.0f;

    void OnEnable()
    {
        FlashlightManager.Instance.Register(this);
        Debug.Log("Registered");
    }

    void OnDisable()
    {
        FlashlightManager.Instance.Unregister(this);
        Debug.Log("Turned off");
    }
}