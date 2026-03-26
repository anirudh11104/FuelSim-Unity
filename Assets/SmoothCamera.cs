using UnityEngine;

public class SmoothCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("0 = Head stays perfectly flat. 1 = Head leans completely with the bike.")]
    [Range(0f, 1f)]
    public float headTiltMultiplier = 0.15f;

    [Header("Dynamic Gear Shift Zoom")]
    public float maxZoomAmount = 12f;
    public float zoomSpeed = 8f;
    public float zoomDuration = 0.2f;
    public float maxEffectSpeed = 100f;

    private Camera cam;
    private float baseFOV;
    private float targetFOV;
    private float zoomTimer = 0f;
    private float currentDynamicZoom = 0f;

    private BikeEngineSimulator bikeScript;
    private int lastGear;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            baseFOV = cam.fieldOfView;
            targetFOV = baseFOV;
        }

        // Look for the bike script in the parent object (Vehicle)
        bikeScript = GetComponentInParent<BikeEngineSimulator>();
        if (bikeScript != null) lastGear = bikeScript.currentGear;
    }

    void LateUpdate()
    {
        // --- 1. DYNAMIC GEAR SHIFT ZOOM ---
        if (bikeScript != null && bikeScript.currentGear != lastGear)
        {
            // Only zoom if actually moving
            if (bikeScript.speed > 5f)
            {
                zoomTimer = zoomDuration;
                float speedFactor = Mathf.Clamp01(bikeScript.speed / maxEffectSpeed);
                currentDynamicZoom = maxZoomAmount * speedFactor;
            }
            lastGear = bikeScript.currentGear;
        }

        if (cam != null)
        {
            if (zoomTimer > 0)
            {
                targetFOV = baseFOV - currentDynamicZoom;
                zoomTimer -= Time.deltaTime;
            }
            else
            {
                targetFOV = baseFOV;
            }
            // Smoothly animate the lens
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }

        // --- 2. NATIVE HORIZON LOCK ---
        // Because the camera is a child, position and steering are handled flawlessly by Unity natively.
        // We only need to counter-rotate the Z-axis (roll) so you don't break your neck leaning.
        if (transform.parent != null)
        {
            float bikeLean = transform.parent.rotation.eulerAngles.z;
            if (bikeLean > 180f) bikeLean -= 360f;

            // Counter-act the lean
            float counterLean = -bikeLean * (1f - headTiltMultiplier);

            // Keep X and Y perfectly glued to 0 (staring at the dashboard), just tilt the Z axis!
            transform.localRotation = Quaternion.Euler(0, 0, counterLean);
        }
    }
}