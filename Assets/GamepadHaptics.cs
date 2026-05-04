using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadHaptics : MonoBehaviour
{
    [Header("Vehicle References (Auto-Detects)")]
    public BikeEngineSimulator bike;
    public CarEngineSimulator car;
    private Rigidbody rb;

    [Header("Gear Shift Clunk")]
    public float shiftPulseDuration = 0.25f;
    public float shiftLowFreq = 1.0f;
    public float shiftHighFreq = 1.0f;

    [Header("Engine Start Rumble")]
    public float startPulseDuration = 0.8f;
    public float startLowFreq = 1.0f;
    public float startHighFreq = 0.6f;

    [Header("Continuous Engine Purr (RT Throttle)")]
    public bool enableEnginePurr = true;
    [Range(0f, 0.5f)] public float maxPurrIntensity = 0.05f;

    [Header("Collision & Scraping")]
    public float impactPulseDuration = 0.35f;
    public float crashForceThreshold = 3.0f;

    private int lastGear;
    private float shiftTimer = 0f;
    private float startTimer = 0f;
    private float impactTimer = 0f;
    private float scrapeTimer = 0f;

    private float impactLow = 0f, impactHigh = 0f;
    private float scrapeLow = 0f, scrapeHigh = 0f;
    private bool wasEngineRunning = false;

    void Start()
    {
        // Auto-detect the vehicle and physics body
        if (bike == null) bike = GetComponentInParent<BikeEngineSimulator>();
        if (car == null) car = GetComponentInParent<CarEngineSimulator>();
        rb = GetComponentInParent<Rigidbody>();

        lastGear = GetCurrentGear();
        wasEngineRunning = IsEngineRunning();
    }

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
            return;
        }
        if (Gamepad.current == null || (bike == null && car == null)) return;

        // --- TRIGGERS ---
        int currentGear = GetCurrentGear();
        if (currentGear != lastGear)
        {
            shiftTimer = shiftPulseDuration;
            lastGear = currentGear;
        }

        bool engineRunning = IsEngineRunning();
        if (engineRunning && !wasEngineRunning)
        {
            startTimer = startPulseDuration;
        }
        wasEngineRunning = engineRunning;

        // --- TIMERS ---
        if (impactTimer > 0) impactTimer -= Time.deltaTime;
        if (scrapeTimer > 0) scrapeTimer -= Time.deltaTime;
        if (shiftTimer > 0) shiftTimer -= Time.deltaTime;
        if (startTimer > 0) startTimer -= Time.deltaTime;

        // --- VIBRATION MIXER (Priority System) ---
        if (impactTimer > 0)
        {
            Gamepad.current.SetMotorSpeeds(impactLow, impactHigh);
        }
        else if (scrapeTimer > 0)
        {
            Gamepad.current.SetMotorSpeeds(scrapeLow, scrapeHigh);
        }
        else if (startTimer > 0)
        {
            Gamepad.current.SetMotorSpeeds(startLowFreq, startHighFreq);
        }
        else if (shiftTimer > 0)
        {
            Gamepad.current.SetMotorSpeeds(shiftLowFreq, shiftHighFreq);
        }
        else if (enableEnginePurr && engineRunning && GetThrottle() > 0.01f)
        {
            float purr = GetThrottle() * maxPurrIntensity;
            Gamepad.current.SetMotorSpeeds(0f, purr);
        }
        else
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    // --- HARD IMPACTS ---
    void OnCollisionEnter(Collision collision)
    {
        if (Gamepad.current == null || collision.contactCount == 0) return;

        if (collision.GetContact(0).normal.y > 0.75f) return;

        float crashForce = collision.relativeVelocity.magnitude;
        if (crashForce < crashForceThreshold) return;

        Vector3 hitDirection = transform.InverseTransformPoint(collision.GetContact(0).point);
        float intensity = Mathf.Clamp01(crashForce / 10f);

        if (hitDirection.x < 0) { impactLow = intensity; impactHigh = 0f; }
        else { impactLow = 0f; impactHigh = intensity; }

        impactTimer = impactPulseDuration;
    }

    // --- CONTINUOUS WALL SCRAPING ---
    void OnCollisionStay(Collision collision)
    {
        if (Gamepad.current == null || collision.contactCount == 0) return;

        if (collision.GetContact(0).normal.y > 0.75f) return;

        float currentSpeed = GetVehicleSpeed();
        if (currentSpeed < 2f) return;

        Vector3 hitDirection = transform.InverseTransformPoint(collision.GetContact(0).point);

        float intensity = Mathf.Clamp(currentSpeed / 30f, 0.5f, 1.0f);

        if (hitDirection.x < 0) { scrapeLow = intensity; scrapeHigh = 0f; }
        else { scrapeLow = 0f; scrapeHigh = intensity; }

        scrapeTimer = 0.1f;
    }

    void OnDisable() { if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f); }
    void OnApplicationQuit() { if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f); }

    // =========================================================
    // UNIVERSAL HELPERS (These make the script work for both!)
    // =========================================================
    private int GetCurrentGear()
    {
        if (bike != null) return bike.currentGear;
        if (car != null) return car.currentGear;
        return 0;
    }

    private bool IsEngineRunning()
    {
        if (bike != null) return bike.engineRunning;
        if (car != null) return car.engineRunning;
        return false;
    }

    private float GetThrottle()
    {
        if (bike != null) return bike.throttle;
        if (car != null) return car.throttle;
        return 0f;
    }

    private float GetVehicleSpeed()
    {
        if (bike != null) return bike.speed;
        if (car != null && rb != null) return rb.velocity.magnitude * 3.6f;
        return 0f;
    }
}