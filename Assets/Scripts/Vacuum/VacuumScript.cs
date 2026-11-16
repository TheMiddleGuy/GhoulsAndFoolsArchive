using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class VacuumScript : MonoBehaviour
{
    private readonly List<BasicGhostBehavior> _ghostsInSuctionRange = new();
    [SerializeField] private float _ghostMaxPlayerPullForce = 1f;

    [Header("Components")]
    [SerializeField] private GameObject _nozzle;
    [SerializeField] private GameObject _suctionComponent;
    [SerializeField] private GameObject _superSuctionComponent;
    [SerializeField] private ParticleSystem _suctionParticles;
    [SerializeField] private ParticleSystem _superSuctionParticles;
    [SerializeField] private Transform _suctionPoint;
    [SerializeField] private LayerMask _suctionBlockers;

    [Header("Variables")]
    [SerializeField] private float _maxDistance = 10f;
    [SerializeField] private float _maxForce = 20f;
    [SerializeField] private float _minForce = 2f;
    private Rigidbody _holderRb;    //used so you can't suck yourself
    private bool _vacuumIsOn = false;

    [Header("Levels")]
    [SerializeField] private bool _hasRepairParts = false;
    [SerializeField] private bool _hasDuctTape = false;
    [SerializeField] private bool _hasCross = false;
    [SerializeField] private bool _hasBook = false;

    [SerializeField] private ParticleSystem _upgradeParticles;  //Upgrade particles
    [SerializeField] private ParticleSystem _brokenParticles;

    public static event System.Action VacuumFullyUpgraded;

    //UI feedback images
    [Header("Feedback Image Components")]
    [SerializeField] private Image _wrongUpgradeIndicator;
    [SerializeField] private Image _requestSpeachBubble;
    [SerializeField] private Image _requiredItemIndicator;
    [Header("Feedback Image Sprites")]
    [SerializeField] private Sprite _ductTapeImage;
    [SerializeField] private Sprite _repairPartsImage;
    [SerializeField] private Sprite _bookOrCrossImage;
    [SerializeField] private Sprite _bookImage;
    [SerializeField] private Sprite _crossImage;
    [Header("Feedback Variables")]
    [SerializeField] private float _resultDisplayDuration = 1f;
    [SerializeField] private float _feedbackDuration = 1.5f;
    [SerializeField] private float _easeDuration = 0.8f;
    [SerializeField] private float _brokenParticlesDuration = 0.5f;

    private Coroutine _indicatorRoutine;
    private bool _feedbackActive = false;

    [Header("HUD")]
    [SerializeField] private VacuumUpgradeOverlayTransition _hUD;

    [Header("Sound")]
    [SerializeField] private AudioSource _effectsSource;
    [SerializeField] private AudioClip _vacuum;
    [SerializeField] private AudioClip _malfunction;
    [SerializeField] private AudioClip _repair;
    [SerializeField] private AudioClip _tape;
    [SerializeField] private AudioClip _holyRepair;
    

    public static event System.Action VacuumFixed;

    private void Awake()
    {
        TurnNozzleOn(false);
        TurnVacuumOn(false, null);

        HideAllIndicators();

        _hUD.DisplayContents(_hasRepairParts, _hasDuctTape, _hasCross, _hasBook);
    }

    private void FixedUpdate()
    {
        if (!_vacuumIsOn) return;

        HandleGhostVacuuming();
    }

    //Displays nozzle when vacuum is grabbed by a player
    public void TurnNozzleOn(bool On)
    {
        _nozzle.SetActive(On);
    }

    //Turns on or off the vacuum
    public void TurnVacuumOn(bool On, Rigidbody holderRb)
    {
        if (On && !_hasRepairParts)
        {
            ShowBrokenVacuumFeedback();
            return;
        }

        _vacuumIsOn = On;
        _holderRb = holderRb;
        if (!On)
            _holderRb = null;

        // --- HANDLE VACUUM LOOP SOUND ---
        if (_effectsSource != null && _vacuum != null)
        {
            if (On)
            {
                // Only start playing if it's not already playing to avoid "phasing"
                if (!_effectsSource.isPlaying || _effectsSource.clip != _vacuum)
                {
                    _effectsSource.clip = _vacuum;
                    _effectsSource.volume = 4f;
                    _effectsSource.loop = true; // Ensure it loops
                    _effectsSource.Play();
                }
            }
            else
            {
                // Stop the loop when vacuum turns off
                if (_effectsSource.clip == _vacuum)
                {
                    _effectsSource.Stop();
                    _effectsSource.loop = false; // Reset loop for other SFX
                }
            }
        }
        // --------------------------------

        if (!IsFullyUpgraded())
        {
            if (On) { _suctionParticles.Play(); }
            else { _suctionParticles.Stop(); }
            _suctionComponent.SetActive(On);
        }
        else
        {
            if (On) { _superSuctionParticles.Play(); }
            else
            {
                _superSuctionParticles.Stop();
                _suctionParticles.Stop();
            }
            _superSuctionComponent.SetActive(On);
        }
    }

    private void HandleGhostVacuuming()
    {
        if (_ghostsInSuctionRange.Count == 0) return;

        List<BasicGhostBehavior> validGhosts = new List<BasicGhostBehavior>();
        Dictionary<BasicGhostBehavior, float> distances = new Dictionary<BasicGhostBehavior, float>();

        for (int i = _ghostsInSuctionRange.Count - 1; i >= 0; i--)
        {
            BasicGhostBehavior ghost = _ghostsInSuctionRange[i];

            if (ghost == null)
            {
                _ghostsInSuctionRange.RemoveAt(i);
                continue;
            }

            Vector3 origin = _suctionPoint.position;
            Vector3 targetPoint = ghost.transform.position;
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;

            if (distance <= 0.001f || distance > _maxDistance)
                continue;

            Vector3 direction = toTarget.normalized;

            if (IsBlocked(origin, direction, distance, ghost.transform))
                continue;

            validGhosts.Add(ghost);
            distances[ghost] = distance;
        }

        if (validGhosts.Count == 0) return;

        bool superVacuum = IsFullyUpgraded();

        BasicGhostBehavior closestGhost = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < validGhosts.Count; i++)
        {
            float dist = distances[validGhosts[i]];
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestGhost = validGhosts[i];
            }
        }

        if (_holderRb != null && closestGhost != null)
        {
            Vector3 pullDirection = closestGhost.transform.position - _holderRb.position;
            pullDirection.y = 0f;

            if (pullDirection.sqrMagnitude > 0.001f)
            {
                BasicMovement movement = _holderRb.GetComponent<BasicMovement>();

                if (movement != null)
                {
                    Vector3 pullVelocity = pullDirection.normalized * _ghostMaxPlayerPullForce * Mathf.Clamp01(pullDirection.magnitude / _maxDistance);
                    movement.AddExternalVelocity(pullVelocity);
                }
            }
        }

        for (int i = 0; i < validGhosts.Count; i++)
        {
            BasicGhostBehavior ghost = validGhosts[i];
            bool isPrimary = superVacuum || ghost == closestGhost;

            ghost.ApplyVacuumInfluence(
                _suctionPoint,
                isPrimary,
                superVacuum,
                _maxDistance
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ghost")) return;

        BasicGhostBehavior ghost = other.GetComponentInParent<BasicGhostBehavior>();
        if (ghost == null) return;

        if (!_ghostsInSuctionRange.Contains(ghost))
            _ghostsInSuctionRange.Add(ghost);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Ghost")) return;

        BasicGhostBehavior ghost = other.GetComponentInParent<BasicGhostBehavior>();
        if (ghost == null) return;

        _ghostsInSuctionRange.Remove(ghost);
    }

    //Suction for non-ghost rigidbodies
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ghost")) return;

        Rigidbody targetRb = other.attachedRigidbody;

        if (targetRb == null) return;
        if (targetRb == _holderRb) return;

        Vector3 origin = _suctionPoint.position;
        Vector3 targetPoint = targetRb.worldCenterOfMass;
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;

        if (distance <= 0.001f || distance > _maxDistance) return;

        Vector3 direction = toTarget.normalized;

        if (IsBlocked(origin, direction, distance, targetRb.transform)) return;

        float t = 1f - Mathf.Clamp01(distance / _maxDistance);
        float force = Mathf.Lerp(_minForce, _maxForce, t * t);

        Vector3 pullDirection = (origin - targetPoint).normalized;
        targetRb.AddForce(pullDirection * force, ForceMode.Force);
    }

    //Checks if a player or wall is in the way of the suction
    private bool IsBlocked(Vector3 origin, Vector3 direction, float distance, Transform target)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            _suctionBlockers,
            QueryTriggerInteraction.Ignore
        );

        //No hits = is not blocked
        if (hits.Length == 0)
        {
            return false;
        }

        float closestDistance = float.MaxValue;
        RaycastHit closestHit = default;
        bool foundHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].distance < closestDistance)
            {
                closestDistance = hits[i].distance;
                closestHit = hits[i];
                foundHit = true;
            }
        }

        //If no player or wall is found = is not blocked
        if (!foundHit)
        {
            return false;
        }

        Transform hitTransform = closestHit.collider.transform;

        //If it hits the target or a child of the target = is not blocked
        if (hitTransform == target || hitTransform.IsChildOf(target) || target.IsChildOf(hitTransform))
        {
            return false;
        }

        //Found a reason to be blocked
        return true;
    }

    private bool IsFullyUpgraded()
    {
        return _hasRepairParts && _hasDuctTape && _hasCross && _hasBook;
    }

    public void SetUpgradeHudVisible()
    {
        if (_hUD == null) return;

        _hUD.DisplayContents(_hasRepairParts, _hasDuctTape, _hasCross, _hasBook);

        _hUD.SetVisible();
    }

    //Upgrading
    internal bool TryUpgradingWith(ItemScript.ItemTypes type)
    {
        //Checks if current upgrade item is the required item
        bool isValidUpgrade = IsValidNextUpgrade(type);

        if (!isValidUpgrade)
        {
            UpgradeFeedback(GetNextRequiredSprite(), false);
            _hUD.DisplayContents(_hasRepairParts, _hasDuctTape, _hasCross, _hasBook);
            return false;
        }

        //Apply the upgrade
        ApplyUpgrade(type);

        //Show feedback for the next required item after applying this one
        Sprite nextSprite = GetNextRequiredSprite();

        if (nextSprite != null)
        {
            UpgradeFeedback(nextSprite, true);
        }
        else
        {
            //Fully upgraded
            if (_upgradeParticles != null)
                _upgradeParticles.Play();
        }

        _hUD.SetVisible();
        _hUD.DisplayContents(_hasRepairParts, _hasDuctTape, _hasCross, _hasBook);
        _hUD.PlayUpgradeAnimation(type);

        //Send message if fully upgraded
        if (_hasRepairParts && _hasDuctTape && _hasCross && _hasBook)
        {
            Debug.Log("Vacuum fully upgraded");
            VacuumFullyUpgraded?.Invoke();
        }
        return true;
    }

    private bool IsValidNextUpgrade(ItemScript.ItemTypes type)
    {
        if (!_hasRepairParts)
            return type == ItemScript.ItemTypes.repairParts;

        if (!_hasDuctTape)
            return type == ItemScript.ItemTypes.ductTape;

        if (!_hasBook && !_hasCross)
            return type == ItemScript.ItemTypes.book || type == ItemScript.ItemTypes.cross;

        if (!_hasBook)
            return type == ItemScript.ItemTypes.book;

        if (!_hasCross)
            return type == ItemScript.ItemTypes.cross;

        return false;
    }

    private void ApplyUpgrade(ItemScript.ItemTypes type)
    {
        switch (type)
        {
            case ItemScript.ItemTypes.repairParts:
                PlaySFX(_repair);
                VacuumFixed?.Invoke();
                _hasRepairParts = true;
                break;

            case ItemScript.ItemTypes.ductTape:
                PlaySFX(_tape);
                _hasDuctTape = true;
                break;

            case ItemScript.ItemTypes.book:
                PlaySFX(_holyRepair);
                _hasBook = true;
                break;

            case ItemScript.ItemTypes.cross:
                PlaySFX(_holyRepair);
                _hasCross = true;
                break;
        }
    }

    private Sprite GetNextRequiredSprite()
    {
        if (!_hasRepairParts)
            return _repairPartsImage;

        if (!_hasDuctTape)
            return _ductTapeImage;

        if (!_hasBook && !_hasCross)
            return _bookOrCrossImage;

        if (!_hasBook)
            return _bookImage;

        if (!_hasCross)
            return _crossImage;

        return null;
    }

    private void ShowBrokenVacuumFeedback()
    {
        if (_feedbackActive) return;

        if (_brokenParticles != null)
            _brokenParticles.Play();

        PlaySFX(_malfunction);
        UpgradeFeedback(_repairPartsImage, false);
    }

    //Displays selected ui image as feedback mechanism
    private void UpgradeFeedback(Sprite image, bool isCorrectUpgrade)
    {
        if (image == null) return;
        if (_feedbackActive) return;

        _requiredItemIndicator.sprite = image;

        if (_indicatorRoutine != null)
            StopCoroutine(_indicatorRoutine);

        _indicatorRoutine = StartCoroutine(ShowFeedbackRoutine(isCorrectUpgrade));
    }

    //Coroutine for showing the feedback
    private IEnumerator ShowFeedbackRoutine(bool isCorrectUpgrade)
    {
        _feedbackActive = true;

        HideAllIndicators();

        if (_hasRepairParts)
        {
            if (isCorrectUpgrade)
            {
                if (_upgradeParticles != null)
                    _upgradeParticles.Play();
            }
            else
            {
                yield return AnimateWithEase(_wrongUpgradeIndicator);
            }

            yield return new WaitForSeconds(_resultDisplayDuration);
        }
        else
        {
            yield return new WaitForSeconds(_brokenParticlesDuration);
        }

        if (_wrongUpgradeIndicator != null)
            _wrongUpgradeIndicator.gameObject.SetActive(false);

        yield return AnimateWithEase(_requiredItemIndicator, _requestSpeachBubble);
        yield return new WaitForSeconds(_feedbackDuration);

        _requiredItemIndicator.gameObject.SetActive(false);
        _requestSpeachBubble.gameObject.SetActive(false);

        _feedbackActive = false;
        _indicatorRoutine = null;
    }

    // Animates one or more UI images using the easing function
    private IEnumerator AnimateWithEase(params Image[] images)
    {
        List<RectTransform> rects = new List<RectTransform>();

        foreach (Image image in images)
        {
            image.gameObject.SetActive(true);
            RectTransform rect = image.rectTransform;
            rect.localScale = Vector3.zero;
            rects.Add(rect);
        }

        float time = 0f;
        while (time < _easeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _easeDuration);
            float scale = EaseOutElastic(t);

            foreach (RectTransform rect in rects)
            {
                rect.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        foreach (RectTransform rect in rects)
        {
            rect.localScale = Vector3.one;
        }
    }

    //Hides all indicators
    private void HideAllIndicators()
    {
        if (_wrongUpgradeIndicator == null || _requestSpeachBubble == null || _requiredItemIndicator == null)
        {
            Debug.LogWarning("Check the vacuum image inputs");
            return;
        }

        _wrongUpgradeIndicator.gameObject.SetActive(false);
        _requestSpeachBubble.gameObject.SetActive(false);
        _requiredItemIndicator.gameObject.SetActive(false);
    }

    //Easing function, source: https://easings.net/
    private float EaseOutElastic(float x)
    {
        float c4 = (2 * Mathf.PI) / 3f;

        if (x == 0f) return 0f;
        if (x == 1f) return 1f;

        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(transform.position + transform.forward * _maxDistance, Vector3.one - Vector3.forward);
    }

    private void PlaySFX(AudioClip clip)
    {
        float lowPitchRange = .85f;
        float highPitchRange = 1.15f;
        float randomPitch = UnityEngine.Random.Range(lowPitchRange, highPitchRange);
        _effectsSource.volume = UnityEngine.Random.Range(1.5f, 2f);
        SoundLimiter.Instance.PlaySound(
            "Vacuum",
            _effectsSource,
            clip,
            randomPitch
        );
    }
}