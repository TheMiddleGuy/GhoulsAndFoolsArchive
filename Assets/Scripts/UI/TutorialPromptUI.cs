using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialPromptUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;

    [SerializeField] private float _slideTime = 0.25f;
    [SerializeField] private float _stayTime = 2f;
    [SerializeField] private float _hiddenOffsetX = 450f;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void Initialize(string message)
    {
        _label.text = message;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return null; // <-- Wait one frame

        Vector2 shown = _rect.anchoredPosition;

        Vector2 hidden = shown + Vector2.right * _hiddenOffsetX;

        _rect.anchoredPosition = hidden;

        yield return Move(hidden, shown);

        yield return new WaitForSeconds(_stayTime);

        yield return Move(shown, hidden);

        Destroy(gameObject);
    }

    private IEnumerator Move(Vector2 from, Vector2 to)
    {
        float time = 0f;

        while (time < _slideTime)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / _slideTime);

            _rect.anchoredPosition = Vector2.Lerp(from, to, t);

            yield return null;
        }

        _rect.anchoredPosition = to;
    }
}