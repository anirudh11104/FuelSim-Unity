using UnityEngine;
using System.Collections;

public class DashboardController : MonoBehaviour
{
    [Header("Connections")]
    public BikeEngineSimulator vehicle; // Drag your Vehicle here
    public RectTransform speedNeedle;
    public RectTransform rpmNeedle;

    [Header("Speedometer Settings")]
    public float speedMinAngle = 140f;
    public float speedMaxAngle = -140f;
    public float maxSpeed = 200f;

    [Header("Tachometer Settings")]
    public float rpmMinAngle = 140f;
    public float rpmMaxAngle = -140f;
    public float maxRPM = 8000f;

    [Header("Needle Weight (Fixes Flicker)")]
    [Range(0.01f, 0.5f)]
    public float needleSmoothing = 0.2f; // Higher = Heavier/Smoother needle

    [Header("Startup Animation")]
    public float sweepDuration = 1.0f;

    private bool isStartingUp = true;
    private float smoothSpeed;
    private float smoothRPM;

    void Start()
    {
        // Now happens as soon as you hit PLAY
        StartCoroutine(StartupSweep());
    }

    void Update()
    {
        if (isStartingUp || vehicle == null) return;

        // 1. FILTER THE JITTER: 
        // We use Lerp to create a "Heavy Needle" effect. 
        // This ignores the tiny physics vibrations in 1st gear.
        smoothSpeed = Mathf.Lerp(smoothSpeed, vehicle.speed, needleSmoothing);
        smoothRPM = Mathf.Lerp(smoothRPM, vehicle.rpm, needleSmoothing);

        // 2. CALCULATE PERCENTAGES
        float speedPercent = Mathf.Clamp01(smoothSpeed / maxSpeed);
        float rpmPercent = Mathf.Clamp01(smoothRPM / maxRPM);

        // 3. APPLY ROTATION
        speedNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(speedMinAngle, speedMaxAngle, speedPercent));
        rpmNeedle.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(rpmMinAngle, rpmMaxAngle, rpmPercent));
    }

    IEnumerator StartupSweep()
    {
        isStartingUp = true;
        float timer = 0f;

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

        isStartingUp = false;
    }
}