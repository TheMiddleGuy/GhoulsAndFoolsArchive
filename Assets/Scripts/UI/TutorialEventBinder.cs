using UnityEngine;

public class TutorialEventBinder : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerHealth.PlayerStateChanged += OnPlayerStateChanged;
        VacuumScript.VacuumFullyUpgraded += OnVacuumFullyUpgraded;
        VacuumScript.VacuumFixed += OnVacuumFixed;
        ItemScript.VacuumPickedUp += OnVacuumPickedUp;
        ItemScript.FlashlightPickedUp += OnFlashlightPickedUp;
        ItemScript.RepairPickedUp += OnRepairPickedUp;
        VacuumGhostKillTrigger.LastGhostKilled += OnLastGhostDead;
        VacuumGhostKillTrigger.GhostKilled += OnGhostKilled;
        Debug.Log("Bindings created");
    }

    private void OnDisable()
    {
        PlayerHealth.PlayerStateChanged -= OnPlayerStateChanged;
        VacuumScript.VacuumFullyUpgraded -= OnVacuumFullyUpgraded;
        VacuumScript.VacuumFixed -= OnVacuumFixed;
        ItemScript.VacuumPickedUp -= OnVacuumPickedUp;
        ItemScript.FlashlightPickedUp -= OnFlashlightPickedUp;
        ItemScript.RepairPickedUp -= OnRepairPickedUp;
        VacuumGhostKillTrigger.LastGhostKilled -= OnLastGhostDead;
        VacuumGhostKillTrigger.GhostKilled -= OnGhostKilled;
    }

    private void OnPlayerStateChanged(PlayerHealth player)
    {
        if (player.IsAlive) return;

        TutorialPromptManager.Instance.ShowPrompt(
            "RevivePlayer", "Catch the revive ghost!", false
            );
    }

    private void OnVacuumPickedUp()
    {
        TutorialPromptManager.Instance.ShowPrompt(
            "VacuumPickedUp", "Repair the vacuum!"
            );
    }

    private void OnVacuumFixed()
    {
        TutorialPromptManager.Instance.ShowPrompt(
            "VacuumRepaired", "Find items to upgrade it!"
            );

        TutorialPromptManager.Instance.ShowPrompt(
            "VacuumRepaired2", "Try killing ghosts! (RT)"
            );
    }

    private void OnVacuumFullyUpgraded()
    {
        TutorialPromptManager.Instance.ShowPrompt(
            "VacuumUpgraded", "Catch them all!"
            );
    }

    private void OnLastGhostDead()
    {
        TutorialPromptManager.Instance.ShowPrompt(
           "LastGhostDead", "Pick up the key! Escape!"
           );
    }

    private void OnFlashlightPickedUp()
    {
        TutorialPromptManager.Instance.ShowPrompt(
           "Flashlight", "Ghosts fear light!"
           );
    }

    private void OnGhostKilled()
    {
        TutorialPromptManager.Instance.ShowPrompt(
           "FirstGhostDead", "Easy right?"
           );
    }

    private void OnRepairPickedUp()
    {
        TutorialPromptManager.Instance.ShowPrompt(
           "RepairParts", "Use this on Vacuum (X)"
           );
    }
}
