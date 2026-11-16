using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class DoorLock : MonoBehaviour
{
    private Rigidbody _rb;

    [Header("Functionality")]
    [SerializeField] private bool _locked = true;
    [SerializeField] private int _lockID;

    public int LockID => _lockID;
    public bool Locked => _locked;

    [Header("Style")]
    [SerializeField] private List<GameObject> _locks = new();
    [SerializeField] private List<Light> _lights = new();
    [SerializeField] private float _unlockingSwingTorque = 4f;

    [SerializeField] private LockColorDatabase _colorDB;

    [SerializeField] private Transform _hinge;
    [SerializeField] private float _shakeAngle = 3f;
    [SerializeField] private float _shakeSpeed = 0.1f;

    [SerializeField] private ParticleSystem _openParticles;

    [Header("Audio")]
    [SerializeField] private AudioSource _effectsSource;
    [SerializeField] private AudioClip _badUnlock;
    [SerializeField] private AudioClip _goodUnlock;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = _locked;
        }

        foreach (GameObject doorLock in _locks)
        {
            doorLock.GetComponent<Renderer>().material.color = _colorDB.GetColor(_lockID);
        }

        foreach (Light light in _lights)
        {
            light.color = _colorDB.GetColor(_lockID);
        }

        var main = _openParticles.main;
        main.startColor = _colorDB.GetColor(_lockID);
    }

    public bool TryUnlock(int keyID, Transform playerPosition)
    {
        if (!_locked) return false;

        if (keyID == _lockID)
        {
            PlaySFX(true);
            Unlock(playerPosition);
            return true;
        }
        else
        {
            PlaySFX(false);
            Debug.Log("Wrong key!");
            WrongKeyAnimation();
            return false;
        }
    }

    private void WrongKeyAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(WrongKeyShake());
    }

    private IEnumerator WrongKeyShake()
    {
        Quaternion originalRotation = _hinge.localRotation;

        for (int i = 0; i < 3; i++)
        {
            yield return RotateTo(originalRotation * Quaternion.Euler(0f, _shakeAngle, 0f));
            yield return RotateTo(originalRotation * Quaternion.Euler(0f, -_shakeAngle, 0f));
        }

        yield return RotateTo(originalRotation);
    }

    private IEnumerator RotateTo(Quaternion targetRotation)
    {
        Quaternion startRotation = _hinge.localRotation;
        float elapsed = 0f;

        while (elapsed < _shakeSpeed)
        {
            float t = elapsed / _shakeSpeed;
            _hinge.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _hinge.localRotation = targetRotation;
    }

    private void Unlock(Transform playerPosition)
    {
        _locked = false;
        if (_rb != null)
        {
            _rb.isKinematic = false;

            Vector3 directionFromPlayer = transform.position - playerPosition.position;
            float side = Vector3.Dot(transform.right, directionFromPlayer);
            float swingDirection = side >= 0 ? 1f : -1f;

            _openParticles.Play();

            _rb.AddTorque(Vector3.up * swingDirection * _unlockingSwingTorque, ForceMode.Impulse);
        }
        foreach (GameObject doorLock in _locks)
        {
            doorLock.SetActive(false);
        }

        Debug.Log("Door unlocked!");
    }

    public void SetLockID(int id)
    {
        _lockID = id;
    }

    private void PlaySFX(bool isSuccess)
    {
        float lowPitchRange = .85f;
        float highPitchRange = 1.15f;
        float randomPitch = UnityEngine.Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = UnityEngine.Random.Range(0.8f, 1f);
        SoundLimiter.Instance.PlaySound(
            "DoorLock",
            _effectsSource,
            (isSuccess) ? _goodUnlock : _badUnlock,
            randomPitch
        );
    }
}
