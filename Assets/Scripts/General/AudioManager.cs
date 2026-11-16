using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Scene Audio")]
    [SerializeField] private AudioClip _gameAmbient;
    [SerializeField] private AudioClip _menuAmbient;

    [Header("Settings")]
    [SerializeField] private float _fadeDuration = 1f;

    private AudioSource _source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();

        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;

        Debug.Log("AudioManager created!");
    }

    public void PlaySceneAudio(GameFlowManager.Scenes scene)
    {

        Debug.Log("Trying to play audio");
        AudioClip clip = null;

        switch (scene)
        {
            case GameFlowManager.Scenes.MainMenu:
                clip = _menuAmbient;
                break;

            case GameFlowManager.Scenes.PlayerSelectionHub:
                clip = _menuAmbient;
                break;

            case GameFlowManager.Scenes.Game:
                clip = _gameAmbient;
                break;

            case GameFlowManager.Scenes.End:
                clip = _menuAmbient;
                break;
        }

        if (_source.clip == clip) return;

        Debug.Log($"Playing clip{clip.name}");

        StopAllCoroutines();
        StartCoroutine(FadeToClip(clip));
    }

    public void StopAllAudio()
    {
        StopAllCoroutines();
        _source.Stop();
        _source.clip = null;
    }

    private IEnumerator FadeToClip(AudioClip clip)
    {
        float startVolume = _source.volume;

        float time = 0f;

        // Fade out
        while (time < _fadeDuration)
        {
            time += Time.deltaTime;

            _source.volume = Mathf.Lerp(
                startVolume,
                0f,
                time / _fadeDuration
            );

            yield return null;
        }

        _source.Stop();

        _source.clip = clip;

        if (clip != null)
        {
            _source.Play();
        }

        // Fade in
        time = 0f;

        while (time < _fadeDuration)
        {
            time += Time.deltaTime;

            _source.volume = Mathf.Lerp(
                0f,
                1f,
                time / _fadeDuration
            );

            yield return null;
        }

        _source.volume = 1f;
    }
}