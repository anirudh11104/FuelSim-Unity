using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadHaptics : MonoBehaviour
{
    public BikeEngineSimulator bike;

    [Header("Gear Shift Clunk")]
    public float shiftPulseDuration = 0.25f;
    public float shiftLowFreq = 1.0f;
    public float shiftHighFreq = 1.0f;

    [Header("Engine Start Rumble")]
    public float startPulseDuration = 0.8f;
    public float startLowFreq = 1.0f;
    public float startHighFreq = 0.6f;

    [Header("Continuous Engine Purr")]
    public bool enableEnginePurr = true;
    [Range(0f, 0.15f)] public float maxPurrIntensity = 0.015f;

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

        // --- TRIGGERS ---
        if (bike.currentGear != lastGear)
        {
            shiftTimer = shiftPulseDuration;
            lastGear = bike.currentGear;
        }

        if (bike.engineRunning && !wasEngineRunning)
        {
            startTimer = startPulseDuration; // Trigger the start rumble
        }
        wasEngineRunning = bike.engineRunning;

        // --- TIMERS ---
        if (impactTimer > 0) impactTimer -= Time.deltaTime;
        if (scrapeTimer > 0) scrapeTimer -= Time.deltaTime;
        if (shiftTimer > 0) shiftTimer -= Time.deltaTime;
        if (startTimer > 0) startTimer -= Time.deltaTime; // Added this back!

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
            // Engine Start Rumble is now properly prioritized!
            Gamepad.current.SetMotorSpeeds(startLowFreq, startHighFreq);
        }
        else if (shiftTimer > 0)
        {
            Gamepad.current.SetMotorSpeeds(shiftLowFreq, shiftHighFreq);
        }
        else if (enableEnginePurr && bike.engineRunning)
        {
            float rpmPercent = bike.rpm / bike.maxRPM;
            float purr = rpmPercent * maxPurrIntensity;
            Gamepad.current.SetMotorSpeeds(0f, purr); // Only the right motor hums
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
        if (collision.GetContact(0).normal.y > 0.5f) return;

        float crashForce = collision.relativeVelocity.magnitude;
        if (crashForce < crashForceThreshold) return;

        Vector3 hitDirection = transform.InverseTransformPoint(collision.GetContact(0).point);
        float intensity = Mathf.Clamp01(crashForce / 20f);

        // STRICT ISOLATION: 0% power to the opposite motor to prevent the heavy weight from stealing the show
        if (hitDirection.x < 0) { impactLow = intensity; impactHigh = 0f; } // Left Hit
        else { impactLow = 0f; impactHigh = intensity; } // Right Hit

        impactTimer = impactPulseDuration;
    }

    // --- CONTINUOUS WALL SCRAPING ---
    void OnCollisionStay(Collision collision)
    {
        if (Gamepad.current == null || collision.contactCount == 0) return;
        if (collision.GetContact(0).normal.y > 0.5f) return;
        if (bike.speed < 5f) return;

        Vector3 hitDirection = transform.InverseTransformPoint(collision.GetContact(0).point);
        float intensity = Mathf.Clamp(bike.speed / 100f, 0.1f, 0.7f);

        // STRICT ISOLATION
        if (hitDirection.x < 0) { scrapeLow = intensity; scrapeHigh = 0f; } // Left Scrape
        else { scrapeLow = 0f; scrapeHigh = intensity; } // Right Scrape

        scrapeTimer = 0.1f;
    }

    void OnDisable() { if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f); }
    void OnApplicationQuit() { if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f); }
}