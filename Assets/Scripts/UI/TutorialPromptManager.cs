using System.Collections.Generic;
using UnityEngine;

public class TutorialPromptManager : MonoBehaviour
{
    public static TutorialPromptManager Instance { get; private set; }

    [SerializeField] private TutorialPromptUI _promptPrefab;
    [SerializeField] private Transform _container;

    private readonly HashSet<string> _shownPrompts = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ShowPrompt("OnSpawn", "Find the vacuum!");
    }

    public void ShowPrompt(string id, string message, bool showOnlyOnce = true)
    {
        // --- No double showing... why would it even happen :D ? ---
        if (showOnlyOnce && _shownPrompts.Contains(id)) return;

        // --- Store for later checking, if needed ---
        if (showOnlyOnce) _shownPrompts.Add(id);

        // --- Create and parent ---
        TutorialPromptUI prompt = Instantiate(_promptPrefab);

        prompt.transform.SetParent(_container, false);

        // --- Set Message ---
        prompt.Initialize(message);
    }
}