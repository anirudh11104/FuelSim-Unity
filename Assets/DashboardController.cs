using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro;

public class DashboardController : MonoBehaviour
{
    [Header("Connections (Link BOTH here!)")]
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

    private BikeEngineSimulator bike;
    private CarEngineSimulator car;

    void Update()
    {
        bool isGamepadConnected = Gamepad.current != null;
        if (isGamepadConnected && !wasGamepadConnected)
        {
            StopAllCoroutines();
            StartCoroutine(StartupSweep());
        }
        wasGamepadConnected = isGamepadConnected;

        if (isStartingUp || ActiveVehicle.Current == null) return;

        bike = ActiveVehicle.Current.GetComponent<BikeEngineSimulator>();
        car = ActiveVehicle.Current.GetComponent<CarEngineSimulator>();

        float speed = 0f;
        float rpm = 0f;
        int gear = 0;

        if (bike != null)
        {
            speed = bike.speed;
            rpm = bike.rpm;
            gear = bike.currentGear;
        }
        else if (car != null)
        {
            speed = car.GetComponent<Rigidbody>().velocity.magnitude * 3.6f;
            rpm = car.rpm;
            gear = car.currentGear;
        }

        smoothSpeed = Mathf.SmoothDamp(smoothSpeed, speed, ref speedVelocity, needleResponsiveness);
        smoothRPM = Mathf.SmoothDamp(smoothRPM, rpm, ref rpmVelocity, needleResponsiveness);

        float speedPercent = Mathf.Clamp01(smoothSpeed / maxSpeed);
        float rpmPercent = Mathf.Clamp01(smoothRPM / maxRPM);

        speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMinAngle, speedMaxAngle, speedPercent));
        rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMinAngle, rpmMaxAngle, rpmPercent));

        if (gearText != null)
        {
            string currentGearStr = gear == 0 ? "N" : gear.ToString();
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

        smoothSpeed = 0f;
        smoothRPM = 0f;
        isStartingUp = false;
    }
}