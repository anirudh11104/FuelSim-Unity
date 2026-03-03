using UnityEngine;

public class EngineSound : MonoBehaviour
{
    public BikeEngineSimulator vehicle;

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

    void Awake()
    {
        // Force silence on boot
        if (startupSource != null) { startupSource.playOnAwake = false; startupSource.Stop(); }
        if (engineLoopSource != null) { engineLoopSource.playOnAwake = false; engineLoopSource.Stop(); }
    }

    void Start()
    {
        // Safety check to find vehicle if the slot is empty
        if (vehicle == null) vehicle = GetComponentInParent<BikeEngineSimulator>();
    }

    void Update()
    {
        // THE SAFETY SHIELD: If everything is null, stop everything and exit
        if (vehicle == null || startupSource == null || engineLoopSource == null)
        {
            if (engineLoopSource != null && engineLoopSource.isPlaying) engineLoopSource.Stop();
            return;
        }

        // --- 1. STARTUP LOGIC ---
        if (vehicle.engineRunning && !wasEngineRunning)
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
        if (vehicle.engineRunning && !isStarting)
        {
            float rpmPercent = Mathf.Clamp01(vehicle.rpm / vehicle.maxRPM);
            engineLoopSource.pitch = Mathf.Lerp(minPitch, maxPitch, rpmPercent);
            engineLoopSource.volume = Mathf.Lerp(0.6f, 1.0f, rpmPercent);

            // Double check: if it somehow stopped, play it again
            if (!engineLoopSource.isPlaying) engineLoopSource.Play();
        }

        // --- 3. SHUTDOWN LOGIC ---
        if (!vehicle.engineRunning)
        {
            isStarting = false;
            if (startupSource.isPlaying) startupSource.Stop();
            if (engineLoopSource.isPlaying) engineLoopSource.Stop();
        }

        wasEngineRunning = vehicle.engineRunning;
    }
}