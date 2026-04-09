using UnityEngine;

public class RealisticMotorcyclePOV : MonoBehaviour
{
    public BikeEngineSimulator vehicle;

    [Header("Camera Weight & Look")]
    public float neckStiffness = 10f;

    [Header("Manual Look (Right Stick / Mouse)")]
    [Tooltip("How fast you look around.")]
    public float lookSensitivity = 2f;
    public float maxLookYaw = 70f;
    public float maxLookPitch = 40f;
    [Tooltip("How fast the head snaps back to center when you let go of the stick.")]
    public float lookCenteringSpeed = 4f;

    [Header("Apex Look (Auto-Looking into turns)")]
    [Tooltip("How much the rider automatically turns their head left/right when the bike leans.")]
    public float apexLookMultiplier = 0.6f;

    [Header("Acceleration & Braking (G-Force)")]
    public float accelPitchMultiplier = 0.8f;
    public float maxAccelPitch = 12f;
    public float accelSmoothness = 5f;

    [Header("Inertia & Gear Shifts")]
    public float shiftPitchIntensity = 8f;
    public float pitchRecoverySpeed = 5f;

    [Header("Photorealistic Helmet Jitter")]
    public float maxMicroJitter = 0.6f;
    public float microJitterSpeed = 25f;
    public float breathingSwayAmount = 0.5f;
    public float breathingSwaySpeed = 0.5f;

    [Header("Horizon Lock")]
    [Range(0f, 1f)]
    public float headTiltMultiplier = 0.15f;

    private int lastGear;
    private float lastSpeed;
    private float currentShiftPitch = 0f;
    private float smoothedAccelPitch = 0f;

    // Tracks where the player is manually looking
    private float currentManualYaw = 0f;
    private float currentManualPitch = 0f;

    void Start()
    {
        if (vehicle != null)
        {
            lastGear = vehicle.currentGear;
            lastSpeed = vehicle.speed;
        }
    }

    void LateUpdate()
    {
        if (vehicle == null) return;

        // --- 1. MANUAL LOOK (Right Stick ONLY) ---
        float inputX = Input.GetAxis("RightStick X");
        float inputY = Input.GetAxis("RightStick Y");

        if (Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f)
        {
            // Player is pushing the stick, move the head
            currentManualYaw += inputX * lookSensitivity * 100f * Time.deltaTime;
            currentManualPitch += inputY * lookSensitivity * 100f * Time.deltaTime;
        }
        else
        {
            // Player let go of the stick, smoothly auto-center the head!
            currentManualYaw = Mathf.Lerp(currentManualYaw, 0f, Time.deltaTime * lookCenteringSpeed);
            currentManualPitch = Mathf.Lerp(currentManualPitch, 0f, Time.deltaTime * lookCenteringSpeed);
        }

        // Stop the player from spinning their head 360 degrees
        currentManualYaw = Mathf.Clamp(currentManualYaw, -maxLookYaw, maxLookYaw);
        currentManualPitch = Mathf.Clamp(currentManualPitch, -maxLookPitch, maxLookPitch);


        // --- 2. ACCELERATION & BRAKING (G-Force) ---
        float speedChange = (vehicle.speed - lastSpeed) / Time.deltaTime;
        lastSpeed = vehicle.speed;

        float targetAccelPitch = -speedChange * accelPitchMultiplier;
        targetAccelPitch = Mathf.Clamp(targetAccelPitch, -maxAccelPitch, maxAccelPitch);
        smoothedAccelPitch = Mathf.Lerp(smoothedAccelPitch, targetAccelPitch, Time.deltaTime * accelSmoothness);


        // --- 3. INERTIA PUNCH (Gear Shifts) ---
        if (vehicle.currentGear != lastGear)
        {
            if (vehicle.speed > 2f)
            {
                if (vehicle.currentGear > lastGear) currentShiftPitch -= shiftPitchIntensity;
                else if (vehicle.currentGear < lastGear && vehicle.currentGear != 0) currentShiftPitch += shiftPitchIntensity;
            }
            lastGear = vehicle.currentGear;
        }
        currentShiftPitch = Mathf.Lerp(currentShiftPitch, 0f, Time.deltaTime * pitchRecoverySpeed);


        // --- 4. LAYERED NOISE ---
        float swayX = (Mathf.PerlinNoise(Time.time * breathingSwaySpeed, 0f) - 0.5f) * breathingSwayAmount;
        float swayY = (Mathf.PerlinNoise(0f, Time.time * breathingSwaySpeed) - 0.5f) * breathingSwayAmount;

        float speedFactor = Mathf.Clamp01(vehicle.speed / 120f);
        float currentJitter = Mathf.Lerp(0.05f, maxMicroJitter, speedFactor);

        float jitterX = (Mathf.PerlinNoise(Time.time * microJitterSpeed, 100f) - 0.5f) * currentJitter;
        float jitterY = (Mathf.PerlinNoise(100f, Time.time * microJitterSpeed) - 0.5f) * currentJitter;


        // --- 5. HORIZON LOCK & APEX LOOK ---
        float counterLean = 0f;
        float apexYaw = 0f;

        if (transform.parent != null)
        {
            float bikeLean = transform.parent.rotation.eulerAngles.z;
            if (bikeLean > 180f) bikeLean -= 360f;

            counterLean = -bikeLean * (1f - headTiltMultiplier);

            // Look into the turn! If the bike leans left, the head automatically looks left.
            apexYaw = bikeLean * apexLookMultiplier;
        }

        // --- 6. APPLY FINAL ROTATION ---
        // Combine everything: physics, noise, manual look, and auto apex look
        float finalPitch = currentShiftPitch + smoothedAccelPitch + swayX + jitterX + currentManualPitch;
        float finalYaw = swayY + jitterY + currentManualYaw + apexYaw;

        Quaternion desiredRotation = Quaternion.Euler(finalPitch, finalYaw, counterLean);

        // Slerp gives it that weighty "lag" you feel in the video
        transform.localRotation = Quaternion.Slerp(transform.localRotation, desiredRotation, Time.deltaTime * neckStiffness);
    }
}