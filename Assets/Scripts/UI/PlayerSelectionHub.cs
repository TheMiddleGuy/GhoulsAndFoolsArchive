using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameSession;

public class PlayerSelectionHub : MonoBehaviour
{

    [SerializeField] private List<Color> _playerColors = new();

    // --- Available colors ---
    private readonly Dictionary<int, Color> _playerColorsDictionary = new Dictionary<int, Color>
    {
        //[0] = Color.red,
        //[1] = Color.green,
        //[2] = Color.blue,
        //[3] = Color.yellow,
        //[4] = Color.pink,
        //[5] = Color.orange,
        //[6] = Color.navyBlue,
        //[7] = Color.white,
        //[8] = Color.coral,
        //[9] = Color.magenta,
        //[10] = Color.cyan
    };
    // Safe way to get value would be:
    // _playerColors.TryGetValue(0, out Color Player.Color whatever

    [System.Serializable]
    struct PlayerEntry
    {
        public Image image;
        public Image eyes;
    }

    // --- Player Displays ---
    [Header("Player Color displays")]
    [SerializeField] private PlayerEntry[] _playerDisplays;
    private int[] _selectedColorIndices = new int[4];

    // --- Possible Gamepads and Ready states ---
    private Gamepad[] _assignedPads = new Gamepad[4];

    // --- Slots that say "Press A to join", duh ---
    [Header("Press A to join slots")]    
    [SerializeField] private GameObject[] _joinTexts;

    // --- Ready texts ---
    [Header("Readiness indicators")]
    [SerializeField] private TextMeshProUGUI[] _playerReadyTexts;
    [SerializeField] private Image[] _frames;
    private bool[] _readyStates = new bool[4];


    // --- Countdown when everybody is ready ---
    [Header("Game start countdown")]
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private int _startCountdownDuration = 3;
    private bool _countdownActive = false;
    private float _countdownTimer = 0f;
    private event System.Action _countdownComplete;
    private bool _isStarting = false;

    private void OnEnable()
    {
        _countdownComplete += LoadGame;
    }

    private void OnDisable()
    {
        _countdownComplete -= LoadGame;
    }

    private void Start()
    {

        for (int i = 0; i < _playerColors.Count; i++)
        {
            _playerColorsDictionary.Add(i, _playerColors[i]);
        }

        if (GameSession.Instance != null)
        {
            Debug.Log("Game Session Exists! Clearing...");
            GameSession.Instance.ClearSelections();
        }

        RefreshAllSlots();

        if (_countdownText != null)
        {
            // --- Disable countdown for now ---
            _countdownText.gameObject.SetActive(false);
        }

        for (int slotIndex = 0; slotIndex < _playerDisplays.Length; slotIndex++)
        {
            // --- Make colors transparent ---
            UpdateSlotVisual(slotIndex);

            // --- Empty ready text ---
            _playerReadyTexts[slotIndex].SetText("");
            _frames[slotIndex].gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // TLDR: Dan here, All this is temporary because it needs to be done.
        // In polishing I will get rid of update loop and fully use messages,
        // for now it works (hopefully)

        if (_isStarting) return; 

        RemoveDisconnectedPads();

        // --- Update Gamepads + input ---
        foreach (var pad in Gamepad.all)
        {
            if (pad == null) continue;

            int thisSlotIndex = GetSlotIndexForPad(pad);

            // --- A -> join if not assigned, otherwise toggle ready ---
            if (pad.buttonSouth.wasPressedThisFrame)
            {
                if (thisSlotIndex == -1) AssignGPadToFreeSlot(pad);
                else ToggleReady(thisSlotIndex);
            }

            // D-pad editing only for already joined players who are not ready
            if (thisSlotIndex != -1 && !_readyStates[thisSlotIndex])
            {
                if (pad.dpad.left.wasPressedThisFrame)
                {
                    PickPreviousColor(thisSlotIndex);
                }
                else if (pad.dpad.right.wasPressedThisFrame)
                {
                    PickNextColor(thisSlotIndex);
                }
            }
        }

        // --- Handle countdown ---
        HandleCountdown();
    }

    void PickNextColor(int slotIndex)
    {
        // --- Increment color index ---
        int potentialColorIdx = _selectedColorIndices[slotIndex] + 1;

        // --- Handle turnaround ---
        if (potentialColorIdx >= _playerColors.Count) potentialColorIdx = 0;

        // --- Handle "In use" case ---
        if (IsColorInUSe(potentialColorIdx, slotIndex))
        {
            Debug.Log("Color in use, picking next one");
            _selectedColorIndices[slotIndex] = potentialColorIdx; // Avoid Stack overflow
            PickNextColor(slotIndex);
            return;
        }

        // --- [0 : 10] and not in use? -> save ---
        _selectedColorIndices[slotIndex] = potentialColorIdx;

        // --- Update visuals ---
        UpdateSlotVisual(slotIndex);
    }

    void PickPreviousColor(int slotIndex)
    {
        // --- Decrement color index ---
        int potentialColorIdx = _selectedColorIndices[slotIndex] - 1;

        // --- Handle turnaround ---
        if (potentialColorIdx < 0) potentialColorIdx = _playerColors.Count - 1;

        // --- Handle "In use case" ---
        if (IsColorInUSe(potentialColorIdx, slotIndex))
        {
            _selectedColorIndices[slotIndex] = potentialColorIdx; // Avoid Stack overflow
            PickPreviousColor(slotIndex);
            return;
        }

        // --- [0 : 10] and not in use? -> save ---
        _selectedColorIndices[slotIndex] = potentialColorIdx;

        // --- Update visuals ---
        UpdateSlotVisual(slotIndex);
    }

    bool IsColorInUSe(int potentialColorIndex, int indexToIgnore)
    {
        Debug.Log("Checking if color is in use");
        // for all connected players
        for (int playerIndex = 0; playerIndex < _assignedPads.Length; playerIndex++)
        {
            // --- Skip the caller ---
            if (playerIndex == indexToIgnore) continue;
            if (_assignedPads[playerIndex] == null) continue;

            if (_selectedColorIndices[playerIndex] == potentialColorIndex) return true;
        }

        return false;
    }

    private int GetSlotIndexForPad(Gamepad pad)
    {
        // --- Raw loop to preserve "index", sorry Alex ---
        for (int playerIndex = 0; playerIndex < _assignedPads.Length; playerIndex++)
        {
            if (_assignedPads[playerIndex] == pad) return playerIndex;
        }
        return -1;
    }

    private void AssignGPadToFreeSlot(Gamepad pad)
    {
        for (int playerIndex = 0; playerIndex < _assignedPads.Length; playerIndex++)
        {
            if (_assignedPads[playerIndex] == null)
            {
                _assignedPads[playerIndex] = pad;
                _readyStates[playerIndex] = false;
                _joinTexts[playerIndex].SetActive(false);
                _frames[playerIndex].gameObject.SetActive(true);

                Debug.Log($"Assigned {pad.displayName} to slot {playerIndex}");

                //Look for the first color not in use, starting from the first color
                for (int colorIndex = 0; colorIndex < _playerColors.Count; colorIndex++)
                {
                    if (!IsColorInUSe(colorIndex, playerIndex))
                    {
                        _selectedColorIndices[playerIndex] = colorIndex;
                        break;
                    }
                }
                UpdateSlotVisual(playerIndex);
                //UpdateSlotVisual(playerIndex);
                return;
            }
        }

        Debug.Log("No free player slots left.");
    }

    private void ToggleReady(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _readyStates.Length) return;
        if (_assignedPads[slotIndex] == null) return;

        _readyStates[slotIndex] = !_readyStates[slotIndex];
        UpdateSlotVisual(slotIndex);

        CancelCountdown();

        Debug.Log($"Player {slotIndex} ready = {_readyStates[slotIndex]}");
    }

    private void RemoveDisconnectedPads()
    {
        for (int savedPadIndex = 0; savedPadIndex < _assignedPads.Length; savedPadIndex++)
        {
            if (_assignedPads[savedPadIndex] == null) continue;

            bool stillConnected = false;

            for (int activePadIndex = 0; activePadIndex < Gamepad.all.Count; activePadIndex++)
            {
                if (Gamepad.all[activePadIndex] == _assignedPads[savedPadIndex])
                {
                    stillConnected = true;
                    break;
                }
            }

            if (!stillConnected)
            {
                Debug.Log($"Player {savedPadIndex} controller disconnected. Clearing slot.");

                _assignedPads[savedPadIndex] = null;
                _readyStates[savedPadIndex] = false;
                _playerReadyTexts[savedPadIndex].text = "";
                _frames[savedPadIndex].gameObject.SetActive(false);

                UpdateSlotVisual(savedPadIndex);
                CancelCountdown();
            }
        }
    }

    private void RefreshAllSlots()
    {
        for (int slotIndex = 0; slotIndex < _playerDisplays.Length; slotIndex++)
        {
            UpdateSlotVisual(slotIndex);
        }
    }

    private void UpdateSlotVisual(int slotIndex)
    {
        Debug.Log($"Updating visuals for slot {slotIndex}");

        if (slotIndex < 0 || slotIndex >= _playerDisplays.Length) return;

        // --- Handle emptiness ---
        if (_assignedPads[slotIndex] == null)
        {
            Debug.Log($"Handling emptiness for slot {slotIndex}");
            // --- Set Alpha to zero ---
            _playerDisplays[slotIndex].image.gameObject.SetActive(false);

            // --- Set "Press A to join" text visible
            _joinTexts[slotIndex].SetActive(true);

            return;
        }

        // --- Handle being connected ---
        // --- Image Color ---
        Color potentialColor;
        _playerColorsDictionary.TryGetValue(_selectedColorIndices[slotIndex], out potentialColor);
        // --- Imagine not finding color lol ---
        if (potentialColor == null) potentialColor = Color.red;

        // --- Connectivity Text ---
        if (_readyStates[slotIndex])
        {
            _playerReadyTexts[slotIndex].text = "READY";
            _playerReadyTexts[slotIndex].color = potentialColor; // Sets text to Green
        }
        else
        {
            _playerReadyTexts[slotIndex].text = "NOT READY";
            Color redish = new Color(0.6f, 0.224f, 0.165f, 1.0f);
            _playerReadyTexts[slotIndex].color = redish;   // Sets text to Red
        }

        _playerDisplays[slotIndex].image.gameObject.SetActive(true);
        _playerDisplays[slotIndex].eyes.color = potentialColor;
    }

    public bool AreAllJoinedPlayersReady()
    {
        int joined = 0;
        int ready = 0;

        for (int playerIndex = 0; playerIndex < _assignedPads.Length; playerIndex++)
        {
            if (_assignedPads[playerIndex] != null)
            {
                joined++;

                if (_readyStates[playerIndex]) ready++;
            }
        }

        return joined > 0 && joined == ready;
    }

    private void HandleCountdown()
    {
        bool shouldCountDown = AreAllJoinedPlayersReady();

        if (!shouldCountDown)
        {
            CancelCountdown();
            return;
        }

        // --- Start countdown ---
        if (!_countdownActive)
        {
            _countdownActive = true;
            _countdownTimer = _startCountdownDuration;

            if (_countdownText != null) _countdownText.gameObject.SetActive(true);
        }

        _countdownTimer -= Time.deltaTime;

        // --- Display Text ---
        if (_countdownText != null)
        {
            float displayTime = Mathf.Max(0f, _countdownTimer);
            _countdownText.text = "Starting in " + displayTime.ToString("0");
        }

        // --- Countdown over ---
        if (_countdownTimer <= 0f)
        {
            _countdownActive = false;

            if (_countdownText != null) _countdownText.gameObject.SetActive(false);

            _isStarting = true;
            SavePlayerData();
            _countdownComplete?.Invoke();
        }
    }

    private void CancelCountdown()
    {
        _countdownActive = false;
        _countdownTimer = _startCountdownDuration;

        if (_countdownText != null)
            _countdownText.gameObject.SetActive(false);
    }

    private void LoadGame()
    {
        GameFlowManager.Instance.LoadGame();
    }

    private void SavePlayerData()
    {
        for (int playerIndex = 0; playerIndex < _assignedPads.Length; playerIndex++)
        {
            SavePlayerSelection(playerIndex);
        }
    }

    private void SavePlayerSelection(int slotIndex)
    {
        if (GameSession.Instance == null) return;
        if (_assignedPads[slotIndex] == null) return;

        // --- Fetch color from dictionnary ---
        Color color;
        _playerColorsDictionary.TryGetValue(_selectedColorIndices[slotIndex], out color);

        PlayerSelectionData selection = new PlayerSelectionData
        {
            PlayerIndex = slotIndex,
            PlayerColor = color,
            GamepadDeviceId = _assignedPads[slotIndex].deviceId
        };

        GameSession.Instance.SavePlayerSelection(selection);
        Debug.Log($"Saved Player {slotIndex} | Color = ({color})");
    }
}
