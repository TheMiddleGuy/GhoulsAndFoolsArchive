using UnityEngine;

public class LossChecker : MonoBehaviour
{
    private PlayerHealth[] _players;

    private bool _gameEnded = false;

    private void Start()
    {
        _players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
    }

    private void OnEnable()
    {
        PlayerHealth.PlayerStateChanged += CheckLossCondition;
    }

    private void OnDisable()
    {
        PlayerHealth.PlayerStateChanged -= CheckLossCondition;
    }

    private void CheckLossCondition(PlayerHealth _)
    {
        if (_gameEnded) return;

        foreach (var player in _players)
        {
            if (player.IsAlive)
            {
                // --- Someone still alive ---
                return;
            }
        }

        // --- Nobody alive ---
        _gameEnded = true;

        Debug.Log("All players are dead. Game lost.");

        GameFlowManager.Instance.FinishGame(false);
    }
}
