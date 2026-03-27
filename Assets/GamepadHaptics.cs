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

    [Header("Continuous Engine Purr (RT Throttle)")]
    public bool enableEnginePurr = true;
    [Range(0f, 0.5f)] public float maxPurrIntensity = 0.05f; // Might need to bump this slightly now that it's tied to RT

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
            startTimer = startPulseDuration;
        }
        wasEngineRunning = bike.engineRunning;

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
        else if (enableEnginePurr && bike.engineRunning && bike.throttle > 0.01f)
        {
            // FIX: Tied directly to RT (throttle).
            // If you hold RT 50%, it vibrates at 50% of the max purr.
            // The millisecond you let go, it drops to 0.
            float purr = bike.throttle * maxPurrIntensity;
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

        // FIX: Increased from 0.5f to 0.75f to catch slanted walls!
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

        // FIX: Increased from 0.5f to 0.75f to catch slanted walls!
        if (collision.GetContact(0).normal.y > 0.75f) return;

        if (bike.speed < 2f) return;

        Vector3 hitDirection = transform.InverseTransformPoint(collision.GetContact(0).point);

        // Ensure scrape is always at least 50% intensity so it overrides the RT purr cleanly
        float intensity = Mathf.Clamp(bike.speed / 30f, 0.5f, 1.0f);

        if (hitDirection.x < 0) { scrapeLow = intensity; scrapeHigh = 0f; }
        else { scrapeLow = 0f; scrapeHigh = intensity; }

        scrapeTimer = 0.1f;
    }

    void OnDisable() { if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f); }
    void OnApplicationQuit() { if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f); }
}