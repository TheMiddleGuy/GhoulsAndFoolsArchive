using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock; // Required for PS-specific controller features

public class PlayerControllerRumble : MonoBehaviour
{
    private Gamepad _gamepad;
    private Coroutine _rumbleRoutine;

    public void SetGamepad(Gamepad gamepad)
    {
        _gamepad = gamepad;
    }

    public void Rumble(float strength)
    {
        if (_gamepad == null || !_gamepad.added) return;

        // Stop any ongoing stop-routine so we don't fight the rumble command
        if (_rumbleRoutine != null)
        {
            StopCoroutine(_rumbleRoutine);
            _rumbleRoutine = null;
        }

        strength = Mathf.Clamp01(strength);

        // General gamepad rumble
        _gamepad.SetMotorSpeeds(strength, strength);

        // DualShock / DualSense specific override to ensure it triggers immediately
        if (_gamepad is DualShockGamepad dsGamepad)
        {
            dsGamepad.SetMotorSpeeds(strength, strength);
        }
    }
    public void StopRumbleOnDisable()
    {
        if (_gamepad == null) return;

        // Direct hardware commands (No Coroutines needed)
        _gamepad.SetMotorSpeeds(0f, 0f);
        _gamepad.ResetHaptics();

        // This forces the USB packet out immediately before the object is gone
        InputSystem.Update();
    }
    public void StopRumble()
    {
        if (_gamepad == null) return;

        // Start the flushing routine to clear the buffer
        if (_rumbleRoutine == null)
        {
                _rumbleRoutine = StartCoroutine(StopRumbleRoutine());
        }
    }

    private IEnumerator StopRumbleRoutine()
    {
        // Spam the "stop" signal for 5 frames (approx. 0.08 seconds) to override the hardware cache
        for (int i = 0; i < 5; i++)
        {
            if (_gamepad != null)
            {
                _gamepad.SetMotorSpeeds(0f, 0f);

                if (_gamepad is DualShockGamepad dsGamepad)
                {
                    dsGamepad.SetMotorSpeeds(0f, 0f);
                }
            }
            yield return null; // Wait for the next frame
        }

        // Final hardware reset
        if (_gamepad != null)
        {
            _gamepad.ResetHaptics();
        }

        _rumbleRoutine = null;
    }

    private void OnDisable()
    {
        StopRumbleOnDisable();
        InputSystem.PauseHaptics();
    }

    private void OnDestroy()
    {
        StopRumbleOnDisable();
    }
    public void Pulse(float lowFreq, float highFreq)
    {
        if (_gamepad == null || !_gamepad.added) return;

        if (_rumbleRoutine != null)
        {
            StopCoroutine(_rumbleRoutine);
            _rumbleRoutine = null;
        }

        // This specifically targets the two different types of motors
        _gamepad.SetMotorSpeeds(Mathf.Clamp01(lowFreq), Mathf.Clamp01(highFreq));

        if (_gamepad is DualShockGamepad dsGamepad)
        {
            dsGamepad.SetMotorSpeeds(lowFreq, highFreq);
        }
    }
}