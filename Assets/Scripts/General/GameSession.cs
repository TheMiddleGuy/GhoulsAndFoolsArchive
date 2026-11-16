using System.Collections.Generic;
using UnityEngine;

public class GameSession : MonoBehaviour
{
    // --- Player Data ---
    public class PlayerSelectionData
    {
        public int PlayerIndex;
        public Color PlayerColor;
        public int GamepadDeviceId;
    }
    
    // --- Game State Data ---
    public enum GameState
    {
        Ongoing = 0,
        Win = 1,
        Loss = 2
    }
    private static GameState _state;
    public static GameState State
        { 
            get { return _state; } 
            set { _state = value; }
        }

    // --- Death and such ---
    // maybe here

    public static GameSession Instance { get; private set; }

    public List<PlayerSelectionData> playerSelections = new List<PlayerSelectionData>();

    private void Awake()
    {
        // --- Prevent duplicates ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _state = GameState.Ongoing;
    }

    public void ClearSelections()
    {
        playerSelections.Clear();
    }

    public void SavePlayerSelection(PlayerSelectionData selection)
    {
        int existingIndex = playerSelections.FindIndex(p => p.PlayerIndex == selection.PlayerIndex);

        if (existingIndex >= 0)
        {
            playerSelections[existingIndex] = selection;
        }
        else
        {
            playerSelections.Add(selection);
        }
    }

    public PlayerSelectionData GetPlayerSelection(int playerIndex)
    {
        return playerSelections.Find(p => p.PlayerIndex == playerIndex);
    }
}
