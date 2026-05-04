using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class DashboardController : MonoBehaviour
{
    [Header("Connections (Link BOTH here!)")]
    public RectTransform speedNeedle;
    public RectTransform rpmNeedle;

    // (Gear text removed so it doesn't fight your new GearDisplay.cs script)

    [Header("Speedometer Settings")]
    public float speedMinAngle = 140f;
    public float speedMaxAngle = -140f;
    public float maxSpeed = 200f;

    [Header("Tachometer Settings")]
    public float rpmMinAngle = 140f;
    public float rpmMaxAngle = -140f;
    public float maxRPM = 8000f;

    [Header("The Ultimate Flicker Fix")]
    [Range(0.01f, 1f)]
    public float needleResponsiveness = 0.15f;

    [Header("Startup Animation")]
    public float sweepDuration = 1.0f;

    private bool isStartingUp = false;
    private float smoothSpeed;
    private float smoothRPM;
    private float speedVelocity;
    private float rpmVelocity;
    private bool wasGamepadConnected = false;

    void Update()
    {
        // 1. Gamepad Sweep Animation Logic
        bool isGamepadConnected = Gamepad.current != null;
        if (isGamepadConnected && !wasGamepadConnected)
        {
            StopAllCoroutines();
            StartCoroutine(StartupSweep());
        }
        wasGamepadConnected = isGamepadConnected;

        // If the sweep animation is running, ignore the engine data for a second
        if (isStartingUp) return;

        // 2. THE FIX: Dynamically find whichever vehicle is active right now
        var car = FindFirstObjectByType<CarEngineSimulator>();
        var bike = FindFirstObjectByType<BikeEngineSimulator>();

        float speed = 0f;
        float rpm = 0f;

        // 3. Extract the data safely
        if (car != null && car.isActiveAndEnabled)
        {
            speed = car.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
            rpm = car.rpm;
        }
        else if (bike != null && bike.isActiveAndEnabled)
        {
            speed = bike.speed;
            rpm = bike.rpm;
        }

        // 4. Smooth the needle movement
        smoothSpeed = Mathf.SmoothDamp(smoothSpeed, speed, ref speedVelocity, needleResponsiveness);
        smoothRPM = Mathf.SmoothDamp(smoothRPM, rpm, ref rpmVelocity, needleResponsiveness);

        float speedPercent = Mathf.Clamp01(smoothSpeed / maxSpeed);
        float rpmPercent = Mathf.Clamp01(smoothRPM / maxRPM);

        // 5. Apply the rotation to the UI Images
        if (speedNeedle != null)
            speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMinAngle, speedMaxAngle, speedPercent));

        if (rpmNeedle != null)
            rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMinAngle, rpmMaxAngle, rpmPercent));
    }

    IEnumerator StartupSweep()
    {
        isStartingUp = true;
        float timer = 0f;

        if (speedNeedle == null || rpmNeedle == null) yield break;

        // Sweep Up
        while (timer < sweepDuration)
        {
            timer += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, timer / sweepDuration);
            speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMinAngle, speedMaxAngle, p));
            rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMinAngle, rpmMaxAngle, p));
            yield return null;
        }

        timer = 0f;
        // Sweep Down
        while (timer < sweepDuration)
        {
            timer += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, timer / sweepDuration);
            speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMaxAngle, speedMinAngle, p));
            rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMaxAngle, rpmMinAngle, p));
            yield return null;
        }

        smoothSpeed = 0f;
        smoothRPM = 0f;
        isStartingUp = false;
    }
}