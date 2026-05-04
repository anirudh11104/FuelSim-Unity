using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro;

public class DashboardController : MonoBehaviour
{
    [Header("Connections (Link BOTH here!)")]
    public BikeEngineSimulator vehicle;
    public CarEngineSimulator car;
    public RectTransform speedNeedle;
    public RectTransform rpmNeedle;

    [Header("Digital Displays")]
    public TextMeshProUGUI gearText;

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

    // --- THE SMART SWITCH ---
    private bool UseCar
    {
        get
        {
            if (car != null && car.engineRunning) return true;
            if (vehicle != null && vehicle.engineRunning) return false;
            if (car != null && car.gameObject.activeInHierarchy && (vehicle == null || !vehicle.gameObject.activeInHierarchy)) return true;
            return false;
        }
    }

    private float Speed => UseCar ? car.GetComponent<Rigidbody>().velocity.magnitude * 3.6f : (vehicle != null ? vehicle.speed : 0f);
    private float Rpm => UseCar ? car.rpm : (vehicle != null ? vehicle.rpm : 0f);
    private int CurrentGear => UseCar ? car.currentGear : (vehicle != null ? vehicle.currentGear : 0);

    void Update()
    {
        bool isGamepadConnected = Gamepad.current != null;
        if (isGamepadConnected && !wasGamepadConnected)
        {
            StopAllCoroutines();
            StartCoroutine(StartupSweep());
        }
        wasGamepadConnected = isGamepadConnected;

        if (isStartingUp || (vehicle == null && car == null)) return;

        smoothSpeed = Mathf.SmoothDamp(smoothSpeed, Speed, ref speedVelocity, needleResponsiveness);
        smoothRPM = Mathf.SmoothDamp(smoothRPM, Rpm, ref rpmVelocity, needleResponsiveness);

        float speedPercent = Mathf.Clamp01(smoothSpeed / maxSpeed);
        float rpmPercent = Mathf.Clamp01(smoothRPM / maxRPM);

        speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMinAngle, speedMaxAngle, speedPercent));
        rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMinAngle, rpmMaxAngle, rpmPercent));

        if (gearText != null)
        {
            string currentGearStr = CurrentGear == 0 ? "N" : CurrentGear.ToString();
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

        smoothSpeed = Speed;
        smoothRPM = Rpm;
        isStartingUp = false;
    }
}