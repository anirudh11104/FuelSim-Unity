using UnityEngine;
using UnityEngine.InputSystem;

public class RealisticCarPOV : MonoBehaviour
{
    public CarEngineSimulator vehicle;

    [Tooltip("An empty GameObject placed exactly where the driver's head should be")]
    public Transform cameraMountPoint;

    private float surgeVelocity = 0f;
    private float pitchVelocity = 0f;
    private float lookYawVelocity = 0f;

    [Header("Manual Look (Right Stick)")]
    public float lookSensitivity = 2f;
    public float maxLookYawLeft = 90f;  // Let the driver look out the side window
    public float maxLookYawRight = 90f;
    public float maxLookPitch = 20f;
    public float lookCenteringSpeed = 4f;

    [Header("Apex Look (Looking into turns)")]
    public float apexLookMultiplier = 15f; // How many degrees to turn the head based on steering

    [Header("Seatbelt Surge (Braking/Acceleration)")]
    public float maxSurgeZ = 0.15f; // Thrown forward into the wheel or pushed back into the seat
    public float surgeSmoothness = 8f;

    [Header("G-Force Pitch (Nose Dive/Squat)")]
    public float accelPitchMultiplier = 0.6f;
    public float maxAccelPitch = 8f;
    public float accelSmoothness = 5f;

    [Header("Idle Camera Sway (Breathing)")]
    public float breathingSwayAmount = 1.5f;
    public float breathingSwaySpeed = 1f;

    [Header("Sense of Speed (Dynamic FOV)")]
    public Camera cam;
    public float baseFOV = 60f;
    public float maxFOV = 75f;
    public float fovSpeedThreshold = 120f; // Speed required to hit max FOV

    // Internal trackers
    private float smoothedAccelPitch = 0f;
    private float smoothedSurgeZ = 0f;
    private float currentManualYaw = 0f;
    private float currentManualPitch = 0f;
    private float smoothedApexYaw = 0f;

    void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (vehicle == null || cameraMountPoint == null) return;

        // --- 1. BASE POSITION ---
        Vector3 basePosition = cameraMountPoint.position;
        Vector3 baseAngles = cameraMountPoint.rotation.eulerAngles;

        // We keep all 3 axes of rotation here so you feel the car's body roll and suspension
        Quaternion baseRotation = Quaternion.Euler(baseAngles.x, baseAngles.y, baseAngles.z);

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

        currentManualYaw = Mathf.Clamp(currentManualYaw, -maxLookYawLeft, maxLookYawRight);
        currentManualPitch = Mathf.Clamp(currentManualPitch, -maxLookPitch, maxLookPitch);

        // --- 3. G-FORCE PITCH & SURGE ---
        float[] gearGForce = { 0f, 15f, 10f, 7f, 4f, 2f };
        int safeGear = Mathf.Clamp(vehicle.currentGear, 0, 5);

        float rpmFactor = Mathf.Clamp01(1f - (vehicle.rpm / vehicle.maxRPM));
        float clutchEngagement = 1f - vehicle.clutch;

        // Acceleration pushes head back and tilts nose up
        float targetAccelPitch = -vehicle.throttle * gearGForce[safeGear] * rpmFactor * clutchEngagement * accelPitchMultiplier;

        // Calculate visual speed approx
        float currentSpeedKmH = (vehicle.wheelSpeed * 0.33f * 3.6f);

        // Braking throws head forward and pitches down
        if (vehicle.brakeInput > 0.1f && currentSpeedKmH > 5f)
        {
            float speedFactor = Mathf.Clamp01(currentSpeedKmH / 60f);
            targetAccelPitch = 8f * speedFactor * vehicle.brakeInput;
        }

        targetAccelPitch = Mathf.Clamp(targetAccelPitch, -maxAccelPitch, maxAccelPitch);

        // Z-Surge: Moving closer or further from the steering wheel
        float targetSurgeZ = 0f;
        bool braking = vehicle.brakeInput > 0.1f;
        bool clutching = vehicle.clutchInputRaw > 0.1f;
        bool accelerating = vehicle.throttle > 0.1f;
        bool isMoving = currentSpeedKmH > 2f;

        if (isMoving)
        {
            if (braking && !clutching) targetSurgeZ = maxSurgeZ;          // Hard Braking (Nose dive)
            else if (braking && clutching) targetSurgeZ = 0.8f * maxSurgeZ; // Braking + Clutch
            else if (!accelerating && !clutching) targetSurgeZ = 0.6f * maxSurgeZ; // Engine braking
            else if (clutching && !braking) targetSurgeZ = 0.2f * maxSurgeZ; // Coasting/Neutral
            else if (accelerating) targetSurgeZ = -maxSurgeZ * 0.5f * vehicle.throttle; // Sunk into seat
        }

        smoothedSurgeZ = Mathf.SmoothDamp(smoothedSurgeZ, targetSurgeZ, ref surgeVelocity, 0.12f);
        smoothedAccelPitch = Mathf.SmoothDamp(smoothedAccelPitch, targetAccelPitch, ref pitchVelocity, 0.1f);

        // --- 4. APEX LOOK (Looking into corners) ---
        // As you steer and drive faster, the camera naturally looks into the turn
        float targetApexYaw = vehicle.steerInput * apexLookMultiplier * Mathf.Clamp01(currentSpeedKmH / 30f);
        smoothedApexYaw = Mathf.SmoothDamp(smoothedApexYaw, targetApexYaw, ref lookYawVelocity, 0.15f);

        // --- 5. ORGANIC IDLE NOISE ---
        float randomX = (Mathf.PerlinNoise(Time.time * breathingSwaySpeed, 100f) * 2f) - 1f;
        float randomY = (Mathf.PerlinNoise(200f, Time.time * breathingSwaySpeed) * 2f) - 1f;

        // Fade out the breathing sway when driving fast so it isn't nauseating
        float swayFade = Mathf.Clamp01(1f - (currentSpeedKmH / 100f));
        float swayX = randomX * breathingSwayAmount * swayFade * 0.6f;
        float swayY = randomY * breathingSwayAmount * swayFade * 0.4f;

        // --- 6. DYNAMIC FOV ---
        if (cam != null)
        {
            float targetFOV = Mathf.Lerp(baseFOV, maxFOV, currentSpeedKmH / fovSpeedThreshold);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
        }

        // --- 7. APPLY ALL OFFSETS ---
        float finalPitch = smoothedAccelPitch + swayX + currentManualPitch;
        float finalYaw = swayY + currentManualYaw + smoothedApexYaw;

        // Local position offset (Z is forward/backward along the mount)
        Vector3 finalPosition = basePosition + (baseRotation * new Vector3(0f, 0f, smoothedSurgeZ));

        // Final Rotation
        Quaternion finalRotation = baseRotation * Quaternion.Euler(finalPitch, finalYaw, 0f);

        transform.position = finalPosition;
        transform.rotation = finalRotation;
    }
}