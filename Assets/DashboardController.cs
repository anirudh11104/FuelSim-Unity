using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro;

public class DashboardController : MonoBehaviour
{
    [Header("Connections")]
    public BikeEngineSimulator vehicle;
    public RectTransform speedNeedle;
    public RectTransform rpmNeedle;

    [Header("Digital Displays")]
    public TextMeshProUGUI gearText; // Kept this for the Gear Display!

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

    void Start()
    {
        if (vehicle == null)
            vehicle = FindFirstObjectByType<BikeEngineSimulator>();
    }

    void Update()
    {
        bool isGamepadConnected = Gamepad.current != null;
        if (isGamepadConnected && !wasGamepadConnected)
        {
            StopAllCoroutines();
            StartCoroutine(StartupSweep());
        }
        wasGamepadConnected = isGamepadConnected;

        if (isStartingUp || vehicle == null) return;

        smoothSpeed = Mathf.SmoothDamp(smoothSpeed, vehicle.speed, ref speedVelocity, needleResponsiveness);
        smoothRPM = Mathf.SmoothDamp(smoothRPM, vehicle.rpm, ref rpmVelocity, needleResponsiveness);

        float speedPercent = Mathf.Clamp01(smoothSpeed / maxSpeed);
        float rpmPercent = Mathf.Clamp01(smoothRPM / maxRPM);

        speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMinAngle, speedMaxAngle, speedPercent));
        rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMinAngle, rpmMaxAngle, rpmPercent));

        // --- GEAR DISPLAY ONLY ---
        if (gearText != null)
        {
            string currentGearStr = vehicle.currentGear == 0 ? "N" : vehicle.currentGear.ToString();
            gearText.text = "Gear: " + currentGearStr;
        }
    }

    IEnumerator StartupSweep()
    {
        isStartingUp = true;
        float timer = 0f;

        while (timer < sweepDuration)
        {
            timer += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, timer / sweepDuration);
            speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMinAngle, speedMaxAngle, p));
            rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMinAngle, rpmMaxAngle, p));
            yield return null;
        }

        timer = 0f;
        while (timer < sweepDuration)
        {
            timer += Time.deltaTime;
            float p = Mathf.SmoothStep(0, 1, timer / sweepDuration);
            speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMaxAngle, speedMinAngle, p));
            rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMaxAngle, rpmMinAngle, p));
            yield return null;
        }

        smoothSpeed = vehicle != null ? vehicle.speed : 0;
        smoothRPM = vehicle != null ? vehicle.rpm : 0;

        isStartingUp = false;
    }
}