using UnityEngine;

public class CameraMotion : MonoBehaviour
{
    public BikeEngineSimulator vehicle;

    [Header("FOV Settings")]
    public float baseFOV = 60f;
    public float maxFOV = 72f;
    public float speedForMaxFOV = 120f; // speed at which camera reaches max FOV

    [Header("Shift Effect")]
    public float shiftPunch = 6f; // How much the FOV spikes when shifting

    [Header("Smoothness")]
    public float smoothSpeed = 4f;

    private Camera cam;
    private int lastGear;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (vehicle != null)
        {
            lastGear = vehicle.currentGear;
        }
    }

    void Update()
    {
        if (vehicle == null) return;

        // --- 1. THE SHIFT PUNCH ---
        if (vehicle.currentGear > lastGear)
        {
            // Shifting UP: The torque hits, simulating the rider's head jerking back
            cam.fieldOfView += shiftPunch;
        }
        else if (vehicle.currentGear < lastGear && vehicle.currentGear != 0)
        {
            // Shifting DOWN: Engine braking hits, simulating lurching forward
            cam.fieldOfView -= shiftPunch;
        }
        lastGear = vehicle.currentGear;


        // --- 2. THE SPEED WARP ---
        // normalize speed safely
        float speedFactor = Mathf.Clamp01(vehicle.speed / speedForMaxFOV);
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedFactor);


        // --- 3. THE SMOOTHER ---
        // This will effortlessly smooth out both the gradual speed changes AND the sudden shift punches
        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            Time.deltaTime * smoothSpeed
        );
    }
}