using UnityEngine;
using UnityEngine.InputSystem;

public class EndScreenScript : MonoBehaviour
{
    // --- Two possible screens ---
    [Header("Outcome screens")]
    [SerializeField] private GameObject _winScreen;
    [SerializeField] private GameObject _lossScreen;

    // --- Buttons ---
    [Header("Buttons")]
    [SerializeField] private MyButtonScript[] _buttons;

    private int _selectedIndex = 0;

    // --- Input ---
    private InputActionMap _playerInputUI;

    private InputAction _goNext;
    private InputAction _goPrev;
    private InputAction _select;

    private void Awake()
    {
        // --- Setup buttons ---
        _buttons[_selectedIndex].SetSelect(true);

        // --- Setup input ---
        PlayerInput input = GetComponent<PlayerInput>();
        _playerInputUI = input.actions.FindActionMap("UI");

        _goNext = _playerInputUI.FindAction("GoNext");
        _goPrev = _playerInputUI.FindAction("GoPrev");
        _select = _playerInputUI.FindAction("Select");
    }

    private void OnEnable()
    {
        if (_goNext != null) _goNext.performed += OnGoNext;
        if (_goPrev != null) _goPrev.performed += OnGoPrev;
        if (_select != null) _select.performed += OnSelect;
    }

    private void OnDisable()
    {
        if (_goNext != null) _goNext.performed -= OnGoNext;
        if (_goPrev != null) _goPrev.performed -= OnGoPrev;
        if (_select != null) _select.performed -= OnSelect;
    }

    private void Start()
    {
        // --- Force both inactive ---
        if (_winScreen != null) _winScreen.SetActive(false);
        if (_lossScreen != null) _lossScreen.SetActive(false);

        // --- Pull GameState -> enable outcome ---
        switch (GameSession.State)
        {
            case GameSession.GameState.Ongoing:
                Debug.LogWarning("EndScreen loaded while game is still ongoing.");
                break;

            case GameSession.GameState.Win:
                if (_winScreen != null)
                    _winScreen.SetActive(true);

                // --- Play Win SFX ---
                break;

            case GameSession.GameState.Loss:
                if (_lossScreen != null)
                    _lossScreen.SetActive(true);

                // --- Play Loss SFX ---
                break;
        }
    }

    
    // --- Input Callbacks ---
    private void OnGoNext(InputAction.CallbackContext context)
    {
        SelectNext();
    }

    private void OnGoPrev(InputAction.CallbackContext context)
    {
        SelectPrevious();
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        ExecuteButton();
    }

    // --- Navigation ---
    private void SelectNext()
    {
        _buttons[_selectedIndex].SetSelect(false);

        if (_selectedIndex >= _buttons.Length - 1)
            _selectedIndex = 0;
        else
            _selectedIndex++;

        _buttons[_selectedIndex].SetSelect(true);
    }

    private void SelectPrevious()
    {
        _buttons[_selectedIndex].SetSelect(false);

        if (_selectedIndex <= 0)
            _selectedIndex = _buttons.Length - 1;
        else
            _selectedIndex--;

        _buttons[_selectedIndex].SetSelect(true);
    }

    private void ExecuteButton()
    {
        Debug.Log($"Executing button {_buttons[_selectedIndex].name}");

        _buttons[_selectedIndex].Execute();
    }

    // --- Button Functions ---
    public void LoadMainMenu()
    {
        GameFlowManager.Instance.LoadMainMenu();
    }

    public void QuitGame()
    {
        GameFlowManager.Instance.QuitGame();
    }

    public void RestartGame()
    {
        GameFlowManager.Instance.RestartGame();
    }
}
