using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameSession;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private PlayerInput playerPrefab;

    [Header("Player Spawn positions (in order of spawning)")]
    [SerializeField] private List<Transform> playerSpawnPoints = new List<Transform>();

    [Header("Player colors (in order of spawning) - DEBUG PUROPSES")]
    [SerializeField] private Color[] playerColors =
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow
    };

    public static event System.Action PlayersSpawned;

    private void Start()
    {
        
        Debug.Log("GameSession exists? " + (GameSession.Instance != null));

        if (GameSession.Instance != null)
        {
            Debug.Log("Saved selections count: " + GameSession.Instance.playerSelections.Count);
            HandleStandardStartup();
        }
        else
        {
            Debug.LogError("GameSession not found. Loading Currently connected controllers");
            HandleDebugStartup();
        }
        
        PlayersSpawned?.Invoke();
    }

    // TLDR: This one is the modern version
    // It pulls data from GameSession
    // Data that previously got saved
    // Good job no? (Gotta glaze myself)
    private void HandleStandardStartup()
    {
        List<PlayerSelectionData> selections = GameSession.Instance.playerSelections;

        for (int selectionIndex = 0; selectionIndex < selections.Count; selectionIndex++)
        {
            PlayerSelectionData selection = selections[selectionIndex];

            Gamepad matchingPad = FindGamepadByDeviceId(selection.GamepadDeviceId);

            if (matchingPad == null)
            {
                Debug.LogWarning($"No matching gamepad found for player {selection.PlayerIndex}");
                continue;
            }

            PlayerInput player = PlayerInput.Instantiate(
                playerPrefab.gameObject,
                playerIndex: selection.PlayerIndex,
                pairWithDevice: matchingPad
            );

            // --- Set controller to PlayerControllerRumble
            PlayerControllerRumble rumble = player.GetComponent<PlayerControllerRumble>();

            if (rumble != null)
            {
                rumble.SetGamepad(matchingPad);
            }

            // --- Set correct color ---
            PlayerColor playerColor = player.GetComponent<PlayerColor>();
            playerColor.SetColor(selection.PlayerColor);

            // --- Set correct ID ---
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            playerHealth.PlayerIndex = selection.PlayerIndex;
            BasicMovement movement = player.GetComponent<BasicMovement>();
            movement.PlayerId = selection.PlayerIndex;

            // --- Move player to spawn point ---
            if (selection.PlayerIndex < playerSpawnPoints.Count)
            {
                player.GetComponent<Rigidbody>().position = playerSpawnPoints[selection.PlayerIndex].position;
                player.GetComponent<Rigidbody>().rotation = playerSpawnPoints[selection.PlayerIndex].rotation;
            }
        }
    }

    // TLDR: This one is the old version,
    // where it just detects currently connected controllers
    // and assigns each a color
    private void HandleDebugStartup()
    {
        var gamepads = Gamepad.all;

        for (int playerIndex = 0; playerIndex < Mathf.Min(4, gamepads.Count); playerIndex++)
        {
            PlayerInput player = PlayerInput.Instantiate(
                playerPrefab.gameObject,
                playerIndex: playerIndex,
                pairWithDevice: gamepads[playerIndex]
            );

            // --- Set controller to PlayerControllerRumble
            PlayerControllerRumble rumble = player.GetComponent<PlayerControllerRumble>();

            if (rumble != null)
            {
                rumble.SetGamepad(gamepads[playerIndex]);
            }

            // --- Set correct color
            PlayerColor playerColor = player.GetComponent<PlayerColor>();
            playerColor.PlayerId = playerIndex;
            Debug.Log(playerIndex);
            playerColor.SetColor(playerColors[playerIndex]);

            // --- Set correct ID ---
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            playerHealth.PlayerIndex = playerIndex;
            BasicMovement movement = player.GetComponent<BasicMovement>();
            movement.PlayerId = playerIndex;

            // --- Move player to spawn point ---
            player.GetComponent<Rigidbody>().position = playerSpawnPoints[playerIndex].position;
            player.GetComponent<Rigidbody>().rotation = playerSpawnPoints[playerIndex].rotation;
        }
    }

    private Gamepad FindGamepadByDeviceId(int deviceId)
    {
        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            if (Gamepad.all[i].deviceId == deviceId)
                return Gamepad.all[i];
        }

        return null;
    }
}
