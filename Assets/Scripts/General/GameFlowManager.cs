using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.75f;

    private bool _transitioning = false;

    public enum Scenes
    {
        MainMenu = 1,
        PlayerSelectionHub = 2,
        Game = 3,
        End = 4
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- Public API ---

    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneRoutine(Scenes.MainMenu));
        Debug.Log("Opening main menu");
        AudioManager.Instance.StopAllAudio();
        AudioManager.Instance.PlaySceneAudio(Scenes.MainMenu);
    }

    public void LoadPlayerSelection()
    {
        StartCoroutine(LoadSceneRoutine(Scenes.PlayerSelectionHub));
    }

    public void LoadGame()
    {
        StartCoroutine(LoadSceneRoutine(Scenes.Game));
        AudioManager.Instance.StopAllAudio();
        AudioManager.Instance.PlaySceneAudio(Scenes.Game);
    }

    public void FinishGame(bool isWin)
    {
        GameSession.State = isWin
            ? GameSession.GameState.Win
            : GameSession.GameState.Loss;

        StartCoroutine(LoadSceneRoutine(Scenes.End));
        AudioManager.Instance.StopAllAudio();
        AudioManager.Instance.PlaySceneAudio(Scenes.End);
    }

    public void RestartGame()
    {
        GameSession.State = GameSession.GameState.Ongoing;

        StartCoroutine(LoadSceneRoutine(Scenes.Game));
        AudioManager.Instance.StopAllAudio();
        AudioManager.Instance.PlaySceneAudio(Scenes.Game);
    }

    public void QuitGame()
    {
        StartCoroutine(QuitRoutine());
    }

    // --- Rountines ---
    private IEnumerator LoadSceneRoutine(Scenes scene)
    {
        if (_transitioning) yield break;

        _transitioning = true;

        yield return FadeToBlack();

        SceneManager.LoadScene((int)scene);

        yield return null;

        yield return FadeFromBlack();

        _transitioning = false;
    }

    private IEnumerator QuitRoutine()
    {
        if (_transitioning) yield break;

        _transitioning = true;

        yield return FadeToBlack();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeToBlack()
    {
        float time = 0f;
        Color color = fadeImage.color;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            color.a = Mathf.Lerp(0f, 1f, time / fadeDuration);
            fadeImage.color = color;

            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    private IEnumerator FadeFromBlack()
    {
        float time = 0f;
        Color color = fadeImage.color;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            color.a = Mathf.Lerp(1f, 0f, time / fadeDuration);
            fadeImage.color = color;

            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
    }
}