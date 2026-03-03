using UnityEngine;

public class CameraMotion : MonoBehaviour
{
    public BikeEngineSimulator vehicle;

    [Header("FOV Settings")]
    public float baseFOV = 60f;
    public float maxFOV = 72f;
    public float speedForMaxFOV = 120f; // speed at which camera reaches max FOV

    [Header("Smoothness")]
    public float smoothSpeed = 4f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (vehicle == null) return;

        // normalize speed safely
        float speedFactor = Mathf.Clamp01(vehicle.speed / speedForMaxFOV);

        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedFactor);

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            Time.deltaTime * smoothSpeed
        );
    }
}
