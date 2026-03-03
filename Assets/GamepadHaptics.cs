using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadHaptics : MonoBehaviour
{
    public BikeEngineSimulator bike;

    [Header("Gear Shift Clunk")]
    public float shiftPulseDuration = 0.2f;
    public float shiftLowFreq = 1.0f;
    public float shiftHighFreq = 0.2f;

    [Header("Engine Start Rumble")]
    public float startPulseDuration = 0.8f;
    public float startLowFreq = 1.0f;
    public float startHighFreq = 0.6f;

    private int lastGear;
    private float shiftTimer = 0f;
    private float startTimer = 0f;
    private bool wasEngineRunning = false;

    void Start()
    {
        if (bike == null) bike = GetComponentInParent<BikeEngineSimulator>();
        if (bike != null)
        {
            lastGear = bike.currentGear;
            wasEngineRunning = bike.engineRunning;
        }
    }

    void Update()
    {
        if (Gamepad.current == null || bike == null) return;

        if (bike.currentGear != lastGear)
        {
            shiftTimer = shiftPulseDuration;
            lastGear = bike.currentGear;
        }

        if (bike.engineRunning && !wasEngineRunning)
        {
            startTimer = startPulseDuration;
        }
        wasEngineRunning = bike.engineRunning;

        if (startTimer > 0)
        {
            startTimer -= Time.deltaTime;
            Gamepad.current.SetMotorSpeeds(startLowFreq, startHighFreq);
        }
        else if (shiftTimer > 0)
        {
            shiftTimer -= Time.deltaTime;
            Gamepad.current.SetMotorSpeeds(shiftLowFreq, shiftHighFreq);
        }
        else
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    void OnDisable()
    {
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    void OnApplicationQuit()
    {
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}