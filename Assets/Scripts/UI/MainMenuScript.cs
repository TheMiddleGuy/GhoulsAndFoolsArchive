using NUnit.Framework;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private MyButtonScript[] _buttons;
    private int _selectedIndex = 1;

    [Header("Auxiliary windows")]
    [SerializeField] private GameObject _controllsWindow;
    [SerializeField] private GameObject _creditsWindow;
    private bool _isAuxWindowOpen = false;

    // --- Player Input ---
    private InputActionMap _playerInputUI;
    private InputAction _goNext;
    private InputAction _goPrev;
    private InputAction _select;
    private InputAction _back;

    private void OnEnable()
    {
        if (_goNext != null) _goNext.performed += OnGoNext;
        if (_goPrev != null) _goPrev.performed += OnGoPrev;
        if (_select != null) _select.performed += OnSelect;
        if (_back != null) _back.performed += OnBack;
    }

    private void OnDisable()
    {
        if (_goNext != null) _goNext.performed -= OnGoNext;
        if (_goPrev != null) _goPrev.performed -= OnGoPrev;
        if (_select != null) _select.performed -= OnSelect;
        if (_back != null) _back.performed -= OnBack;
    }

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

    private void OnBack(InputAction.CallbackContext context)
    {
        CloseAll();
    }


    private void Awake()
    {
        // --- Select Play Button by default ---
        _buttons[_selectedIndex].SetSelect(true);

        PlayerInput input = GetComponent<PlayerInput>();
        _playerInputUI = input.actions.FindActionMap("UI");

        _goNext = _playerInputUI.FindAction("GoNext");
        _goPrev = _playerInputUI.FindAction("GoPrev");
        _select = _playerInputUI.FindAction("Select");
        _back = _playerInputUI.FindAction("Back");
    }

    private void SelectNext()
    {
        // --- Disallow if aux window open ---
        if (_isAuxWindowOpen) return;

        // --- Deselect ---
        _buttons[_selectedIndex].SetSelect(false);

        // --- If Current Index == Max -> circle
        if (_selectedIndex == _buttons.Length - 1) _selectedIndex = 0;
        else _selectedIndex++;

        _buttons[_selectedIndex].SetSelect(true);
    }

    private void SelectPrevious()
    {
        // --- Disallow if aux window open ---
        if (_isAuxWindowOpen) return;

        // --- Deselect ---
        _buttons[_selectedIndex].SetSelect(false);

        // --- If Current Index == 0 -> circle ---
        if (_selectedIndex == 0) _selectedIndex = _buttons.Length - 1;
        else _selectedIndex--;

        _buttons[_selectedIndex].SetSelect(true);
    }

    private void ExecuteButton()
    {
        // --- Disallow if aux window open ---
        if (_isAuxWindowOpen) return;

        Debug.Log($"Executing button {_buttons[_selectedIndex].name}");
        _buttons[_selectedIndex].Execute();
    }

    private void CloseAll()
    {
        _isAuxWindowOpen = false;

        Debug.Log("Closing all windows");

        if (_controllsWindow != null) _controllsWindow.SetActive(false);
        if (_creditsWindow != null) _creditsWindow.SetActive(false);
    }

    public void QuitGame()
    {
        GameFlowManager.Instance.QuitGame();
    }

    public void LoadPlayerSelectionHub()
    {
        GameFlowManager.Instance.LoadPlayerSelection();
    }

    public void OpenCredits()
    {
        _isAuxWindowOpen = true;
        Debug.Log("Opening Credits Window");
        if (_creditsWindow != null) _creditsWindow.SetActive(true);
    }

    public void OpenControlls()
    {
        _isAuxWindowOpen = true;
        Debug.Log("Opening Controls Window");
        if (_controllsWindow != null) _controllsWindow.SetActive(true);
    }
}
