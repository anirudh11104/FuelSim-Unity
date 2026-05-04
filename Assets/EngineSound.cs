using UnityEngine;

public class EngineSound : MonoBehaviour
{
    [Header("Vehicle Links (Assign One)")]
    public BikeEngineSimulator vehicle;
    public CarEngineSimulator car;

    [Header("Audio Sources")]
    public AudioSource startupSource;
    public AudioSource engineLoopSource;

    [Header("Startup Settings")]
    public float startupCutoffTime = 0.5f;

    [Header("Engine Loop Settings")]
    public float minPitch = 0.8f;
    public float maxPitch = 2.5f;

    private bool isStarting = false;
    private float startTimer = 0f;
    private bool wasEngineRunning = false;

    private bool EngineRunning => vehicle != null ? vehicle.engineRunning : (car != null ? car.engineRunning : false);
    private float Rpm => vehicle != null ? vehicle.rpm : (car != null ? car.rpm : 0f);
    private float MaxRpm => vehicle != null ? vehicle.maxRPM : (car != null ? car.maxRPM : 7000f);

    void Awake()
    {
        if (startupSource != null) { startupSource.playOnAwake = false; startupSource.Stop(); }
        if (engineLoopSource != null) { engineLoopSource.playOnAwake = false; engineLoopSource.Stop(); }
    }

    void Update()
    {
        if ((vehicle == null && car == null) || startupSource == null || engineLoopSource == null)
        {
            if (engineLoopSource != null && engineLoopSource.isPlaying) engineLoopSource.Stop();
            return;
        }

        // --- 1. STARTUP LOGIC ---
        if (EngineRunning && !wasEngineRunning)
        {
            isStarting = true;
            startTimer = 0f;
            startupSource.Play();
            engineLoopSource.Stop();
        }

        if (isStarting)
        {
            startTimer += Time.deltaTime;
            if (startTimer >= startupCutoffTime)
            {
                isStarting = false;
                startupSource.Stop();
                engineLoopSource.Play();
                engineLoopSource.loop = true;
            }
        }

        // --- 2. RUNNING LOGIC ---
        if (EngineRunning && !isStarting)
        {
            float rpmPercent = Mathf.Clamp01(Rpm / MaxRpm);
            engineLoopSource.pitch = Mathf.Lerp(minPitch, maxPitch, rpmPercent);
            engineLoopSource.volume = Mathf.Lerp(0.6f, 1.0f, rpmPercent);

            if (!engineLoopSource.isPlaying) engineLoopSource.Play();
        }

        // --- 3. SHUTDOWN LOGIC ---
        if (!EngineRunning)
        {
            isStarting = false;
            if (startupSource.isPlaying) startupSource.Stop();
            if (engineLoopSource.isPlaying) engineLoopSource.Stop();
        }

        wasEngineRunning = EngineRunning;
    }
}