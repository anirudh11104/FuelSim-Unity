using UnityEngine;
using UnityEngine.InputSystem;

public class RealisticMotorcyclePOV : MonoBehaviour
{
    public BikeEngineSimulator vehicle;
    public Transform cameraMountPoint;
    public Transform bikeTransform;

    private float surgeVelocity = 0f;
    private float pitchVelocity = 0f;
    private float bikeLeanVelocity = 0f;

    [Header("Manual Look (Right Stick)")]
    public float lookSensitivity = 2f;
    public float maxLookYaw = 40f;
    public float maxLookPitch = 20f;
    public float lookCenteringSpeed = 4f;

    [Header("Apex Look (Auto-Looking into turns)")]
    public float apexLookMultiplier = 0.6f;

    [Header("Body Surge (The Clutch/Brake Effect)")]
    public float maxSurgeZ = 0.25f;
    public float surgeSmoothness = 8f;

    [Header("Acceleration & Braking (G-Force Pitch)")]
    public float accelPitchMultiplier = 0.8f;
    public float maxAccelPitch = 12f;
    public float accelSmoothness = 5f;

    [Header("Inertia & Gear Shifts")]
    public float shiftPitchIntensity = 8f;
    public float pitchRecoverySpeed = 5f;

    [Header("Idle Camera Sway (Noticeable Breathing)")]
    public float maxMicroJitter = 0.6f;
    public float breathingSwayAmount = 4f;
    public float breathingSwaySpeed = 1.2f;

    // Internal trackers
    private float lastSpeed;
    private float currentShiftPitch = 0f;
    private float smoothedAccelPitch = 0f;
    private float smoothedSurgeZ = 0f;
    private float smoothedBikeLean = 0f;
    private float currentManualYaw = 0f;
    private float currentManualPitch = 0f;

    void Start()
    {
        if (vehicle != null)
        {
            lastSpeed = vehicle.speed;
        }
    }

    void LateUpdate()
    {
        if (vehicle == null || cameraMountPoint == null) return;

        // --- 1. THE JITTER KILLER (Absolute Tracking) ---
        Vector3 basePosition = cameraMountPoint.position;

        // THE FIX: Grab the mount's angles, but force the Z-axis (roll) to 0.
        // This keeps the horizon perfectly level while still tracking hills and turns!
        Vector3 baseAngles = cameraMountPoint.rotation.eulerAngles;
        Quaternion baseRotation = Quaternion.Euler(baseAngles.x, baseAngles.y, 0f);

        // --- 2. MANUAL LOOK ---
        float inputX = 0f;
        float inputY = 0f;

        if (Gamepad.current != null)
        {
            inputX = Gamepad.current.rightStick.x.ReadValue();
            inputY = -Gamepad.current.rightStick.y.ReadValue();
        }

        if (Mathf.Abs(inputX) < 0.15f) inputX = 0f;
        if (Mathf.Abs(inputY) < 0.15f) inputY = 0f;

        if (inputX != 0f || inputY != 0f)
        {
            currentManualYaw += inputX * lookSensitivity * 100f * Time.deltaTime;
            currentManualPitch += inputY * lookSensitivity * 100f * Time.deltaTime;
        }
        else
        {
            currentManualYaw = Mathf.Lerp(currentManualYaw, 0f, Time.deltaTime * lookCenteringSpeed);
            currentManualPitch = Mathf.Lerp(currentManualPitch, 0f, Time.deltaTime * lookCenteringSpeed);
        }

        currentManualYaw = Mathf.Clamp(currentManualYaw, -maxLookYaw, maxLookYaw);
        currentManualPitch = Mathf.Clamp(currentManualPitch, -maxLookPitch, maxLookPitch);

        // --- 3. SURGE & G-FORCE PITCH ---
        float[] gearGForce = { 0f, 15f, 10f, 7f, 4f, 2f };
        int safeGear = Mathf.Clamp(vehicle.currentGear, 0, 5);

        float rpmFactor = Mathf.Clamp01(1f - (vehicle.rpm / vehicle.maxRPM));
        float clutchEngagement = 1f - vehicle.clutch;

        // 1. Un-square the brake math to get a perfect 0.0 to 1.0 linear trigger ratio
        float linearBrakeRatio = Mathf.Sqrt(Mathf.Clamp01(vehicle.currentBrakeForce / vehicle.brakePower));

        // 2. Acceleration Pitch
        float targetAccelPitch = -vehicle.throttle * gearGForce[safeGear] * rpmFactor * clutchEngagement * accelPitchMultiplier;

        // 3. Braking Dive (Scales proportionally with the trigger!)
        if (linearBrakeRatio > 0.01f)
        {
            float speedFactor = Mathf.Clamp01(vehicle.speed / 10f);
            targetAccelPitch = 10f * speedFactor * linearBrakeRatio;
        }

        targetAccelPitch = Mathf.Clamp(targetAccelPitch, -maxAccelPitch, maxAccelPitch);

        // 4. Z-Surge Update (Moving forward/back proportionally)
        float targetSurgeZ = 0f;
        bool clutching = vehicle.clutchInputRaw > 0.1f;
        bool accelerating = vehicle.throttle > 0.1f;
        bool isMoving = vehicle.speed > 2f;

        if (isMoving)
        {
            if (linearBrakeRatio > 0.05f && !clutching) targetSurgeZ = maxSurgeZ * linearBrakeRatio; // Smooth forward surge!
            else if (linearBrakeRatio > 0.05f && clutching) targetSurgeZ = 0.8f * maxSurgeZ * linearBrakeRatio; // Smooth forward surge!
            else if (!accelerating && !clutching) targetSurgeZ = 0.6f * maxSurgeZ; // Engine braking
            else if (clutching && linearBrakeRatio <= 0.05f) targetSurgeZ = 0.2f * maxSurgeZ; // Coasting
            else if (accelerating) targetSurgeZ = -vehicle.throttle * maxSurgeZ * 0.6f; // Sink into seat
        }

        targetSurgeZ = Mathf.Clamp(targetSurgeZ, -maxSurgeZ, maxSurgeZ);

        // Smooth the INTERNAL numbers, not the camera transform
        smoothedSurgeZ = Mathf.SmoothDamp(smoothedSurgeZ, targetSurgeZ, ref surgeVelocity, 0.12f);
        smoothedAccelPitch = Mathf.SmoothDamp(smoothedAccelPitch, targetAccelPitch, ref pitchVelocity, 0.1f);
        currentShiftPitch = Mathf.Lerp(currentShiftPitch, 0f, Time.deltaTime * pitchRecoverySpeed);

        // --- 4. ORGANIC NOISE & APEX LOOK ---
        float swayFade = 1f;
        float randomX = (Mathf.PerlinNoise(Time.time * breathingSwaySpeed, 100f) * 2f) - 1f;
        float randomY = (Mathf.PerlinNoise(200f, Time.time * breathingSwaySpeed) * 2f) - 1f;

        float swayX = randomX * breathingSwayAmount * swayFade * 0.6f;
        float swayY = randomY * breathingSwayAmount * swayFade * 0.4f;

        float currentBikeLean = 0f;
        if (bikeTransform != null)
        {
            float rawLean = bikeTransform.localEulerAngles.z;
            if (rawLean > 180f) rawLean -= 360f;
            currentBikeLean = rawLean;
        }

        smoothedBikeLean = Mathf.SmoothDamp(smoothedBikeLean, currentBikeLean, ref bikeLeanVelocity, 0.08f);
        float apexYaw = smoothedBikeLean * apexLookMultiplier;

        // --- 5. APPLY EVERYTHING AS LOCAL OFFSETS ---
        float finalPitch = smoothedAccelPitch + swayX + currentManualPitch;
        float finalYaw = swayY + currentManualYaw + apexYaw;

        // Calculate final position by moving locally forward/backward along the mount's Z axis
        Vector3 finalPosition = basePosition + (baseRotation * new Vector3(0f, 0f, smoothedSurgeZ));

        // Calculate final rotation by applying our effects ON TOP of the mount's rigid rotation
        Quaternion finalRotation = baseRotation * Quaternion.Euler(finalPitch, finalYaw, 0f);

        transform.position = finalPosition;
        transform.rotation = finalRotation;
    }
}