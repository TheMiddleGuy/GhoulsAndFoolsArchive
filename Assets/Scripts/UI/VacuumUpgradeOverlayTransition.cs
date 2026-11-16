using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VacuumUpgradeOverlayTransition : MonoBehaviour
{
    [Header("Main HUD Root")]
    [SerializeField] private RectTransform _mainHudRoot;

    [Header("Move Positions")]
    [SerializeField] private Vector2 _hiddenAnchoredPosition;
    [SerializeField] private Vector2 _visibleAnchoredPosition;
    [SerializeField] private float _moveDuration = 0.35f;

    [Header("Auto Hide")]
    [SerializeField] private float _hideAfterNoCallDuration = 1f;

    [Header("Components")]
    [SerializeField] private Image _slot1;
    [SerializeField] private Image _repairParts;
    [SerializeField] private Image _slot2;
    [SerializeField] private Image _cross;
    [SerializeField] private Image _slot3;
    [SerializeField] private Image _book;

    [SerializeField] private Image _broken;
    [SerializeField] private Image _fixed;

    [Header("Variables")]
    [SerializeField, Range(0f, 1f)] private float _transparentAlpha = 0.35f;

    private Coroutine _moveRoutine;
    private bool _isVisible;
    private float _hideTimer;

    [Header("Drop Animation")]
    [SerializeField] private float _dropDuration = 0.5f;
    [SerializeField] private float _dropStartScale = 3f;
    [SerializeField] private float _dropDelay = 0.08f;

    private Coroutine _upgradeAnimationRoutine;

    private void Awake()
    {
        if (_mainHudRoot != null)
            _mainHudRoot.anchoredPosition = _hiddenAnchoredPosition;

        DisplayContents(false, false, false, false);
        SetRootActive(false);
        _isVisible = false;
    }

    private void Update()
    {
        if (!_isVisible) return;

        if (_hideTimer >= _hideAfterNoCallDuration)
        {
            Hide();
        }
        else
        {
            _hideTimer += Time.deltaTime;
        }
    }

    //Call this repeatedly while the HUD should stay visible
    public void SetVisible()
    {
        _hideTimer = 0f;
        Show();
    }

    public void DisplayContents(bool hasRepairParts, bool hasDuctTape, bool hasCross, bool hasBook)
    {
        HideAll();

        //No upgrades
        _slot1.enabled = true;
        _repairParts.enabled = true;
        SetImageAlpha(_repairParts, hasRepairParts ? 1f : _transparentAlpha);

        _broken.enabled = !hasRepairParts;
        _fixed.enabled = hasRepairParts;

        //Repair parts applied
        if (hasRepairParts)
        {
            _slot2.enabled = true;
            _slot3.enabled = true;

            SetImageAlpha(_slot2, hasDuctTape ? 1f : _transparentAlpha);
            SetImageAlpha(_slot3, hasDuctTape ? 1f : _transparentAlpha);
        }

        //Duct tape applied
        if (hasDuctTape)
        {
            _cross.enabled = true;
            _book.enabled = true;

            SetImageAlpha(_cross, hasCross ? 1f : _transparentAlpha);
            SetImageAlpha(_book, hasBook ? 1f : _transparentAlpha);
        }
    }

    private void Show()
    {
        _isVisible = true;

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(AnimateVisibilityRoutine(true));
    }

    private void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        if (_moveRoutine != null)
            StopCoroutine(_moveRoutine);

        _moveRoutine = StartCoroutine(AnimateVisibilityRoutine(false));
    }

    private IEnumerator AnimateVisibilityRoutine(bool show)
    {
        if (show)
            SetRootActive(true);

        Vector2 start = _mainHudRoot.anchoredPosition;
        Vector2 end = show ? _visibleAnchoredPosition : _hiddenAnchoredPosition;

        float time = 0f;

        while (time < _moveDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _moveDuration);

            float easedT = show ? EaseOutQuart(t) : EaseInQuart(t);
            _mainHudRoot.anchoredPosition = Vector2.LerpUnclamped(start, end, easedT);

            yield return null;
        }

        _mainHudRoot.anchoredPosition = end;

        if (!show)
            SetRootActive(false);

        _moveRoutine = null;
    }

    private void HideAll()
    {
        _slot1.enabled = false;
        _repairParts.enabled = false;
        _slot2.enabled = false;
        _cross.enabled = false;
        _slot3.enabled = false;
        _book.enabled = false;

        _broken.enabled = false;
        _fixed.enabled = false;
    }

    private void SetRootActive(bool active)
    {
        if (_mainHudRoot != null)
            _mainHudRoot.gameObject.SetActive(active);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private float EaseOutQuart(float x)
    {
        return 1f - Mathf.Pow(1f - x, 4f);
    }

    private float EaseInQuart(float x)
    {
        return x * x * x * x;
    }

    public void PlayUpgradeAnimation(ItemScript.ItemTypes type)
    {
        if (_upgradeAnimationRoutine != null)
            StopCoroutine(_upgradeAnimationRoutine);

        _upgradeAnimationRoutine = StartCoroutine(PlayUpgradeAnimationRoutine(type));
    }

    private IEnumerator PlayUpgradeAnimationRoutine(ItemScript.ItemTypes type)
    {
        switch (type)
        {
            case ItemScript.ItemTypes.repairParts:
                _fixed.enabled = false;

                yield return DropImage(_repairParts);

                yield return new WaitForSeconds(_dropDelay);

                yield return DropImage(_fixed);
                break;

            case ItemScript.ItemTypes.ductTape:
                SetImageAlpha(_slot2, _transparentAlpha);
                SetImageAlpha(_slot3, _transparentAlpha);

                SetImageAlpha(_slot2, 1f);
                yield return DropImage(_slot2);

                yield return new WaitForSeconds(_dropDelay);

                SetImageAlpha(_slot3, 1f);
                yield return DropImage(_slot3);
                break;

            case ItemScript.ItemTypes.cross:
                yield return DropImage(_cross);
                break;

            case ItemScript.ItemTypes.book:
                yield return DropImage(_book);
                break;
        }

        _upgradeAnimationRoutine = null;
    }

    private IEnumerator DropImage(Image image)
    {
        if (image == null)
            yield break;

        image.enabled = true;
        SetImageAlpha(image, 1f);

        RectTransform rect = image.rectTransform;
        rect.localScale = Vector3.one * _dropStartScale;

        float time = 0f;

        while (time < _dropDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / _dropDuration);

            float easedT = EaseInCubic(t);
            float scale = Mathf.LerpUnclamped(_dropStartScale, 1f, easedT);

            rect.localScale = Vector3.one * scale;

            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    private float EaseInCubic(float x)
    {
        return x * x * x;
    }
}