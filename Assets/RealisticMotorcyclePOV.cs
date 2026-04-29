using UnityEngine;



public class RealisticMotorcyclePOV : MonoBehaviour

{

    public BikeEngineSimulator vehicle;



    [Header("Camera Weight & Look")]

    public float neckStiffness = 3f;



    [Header("Manual Look (Right Stick)")]

    public float lookSensitivity = 2f;

    public float maxLookYaw = 40f;

    public float maxLookPitch = 20f;

    public Transform bikeTransform;

    public Transform cameraMountPoint;

    public float lookCenteringSpeed = 4f;



    [Header("Apex Look (Auto-Looking into turns)")]

    public float apexLookMultiplier = 0.6f;



    [Header("Body Surge (The Clutch/Brake Effect)")]



    public float maxSurgeZ = 0.25f; // Increased so you can really see it move

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

    public float microJitterSpeed = 25f;

    public float breathingSwayAmount = 4f; // Cranked up for noticeable idle movement

    public float breathingSwaySpeed = 1.2f;  // Breathes faster



    [Header("Horizon Lock")]

    [Range(0f, 1f)]

    public float headTiltMultiplier = 0.15f;



    // Internal trackers

    private int lastGear;

    private float lastSpeed;

    public Vector3 cameraOffset = new Vector3(0f, 1.6f, 0.2f);

    private float currentShiftPitch = 0f;



    private float smoothedAccelPitch = 0f;

    private float smoothedSurgeZ = 0f;

    private float smoothedBikeLean = 0f;

    private float bikeLeanVelocity = 0f;

    private float currentManualYaw = 0f;

    private float currentManualPitch = 0f;

    Quaternion yawOnly;

    float smoothedYaw = 0f;

    private Vector3 baseLocalPosition;



    void Start()

    {

        if (vehicle != null)

        {

            lastGear = vehicle.currentGear;

            lastSpeed = vehicle.speed;

        }

        baseLocalPosition = transform.localPosition;

        smoothedYaw = Mathf.Atan2(bikeTransform.forward.x, bikeTransform.forward.z) * Mathf.Rad2Deg;

    }



    void LateUpdate()

    {

        yawOnly = Quaternion.Euler(0f, bikeTransform.eulerAngles.y, 0f);

        if (vehicle == null) return;



        // --- 1. MANUAL LOOK ---

        float inputX = Input.GetAxis("RightStick X");

        float inputY = Input.GetAxis("RightStick Y");



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





        // --- 2. G-FORCE PITCH & MASSIVE BODY SURGE (Z-Shift) ---
        float speedChange = (vehicle.speed - lastSpeed) / Time.deltaTime;
        lastSpeed = vehicle.speed;

        float targetAccelPitch = -speedChange * accelPitchMultiplier;
        targetAccelPitch = Mathf.Clamp(targetAccelPitch, -maxAccelPitch, maxAccelPitch);
        smoothedAccelPitch = Mathf.Lerp(smoothedAccelPitch, targetAccelPitch, Time.deltaTime * accelSmoothness);

        // NEW SURGE LOGIC: Explicitly mapping deceleration vs acceleration AND SPEED
        float targetSurgeZ = 0f;

        bool braking = vehicle.currentBrakeForce > 10f;
        bool clutching = vehicle.clutch > 0.1f;
        bool accelerating = vehicle.throttle > 0.1f;
        bool isMoving = vehicle.speed > 2f; // NEW: Only apply momentum effects if moving

        if (isMoving)
        {
            if (braking && !clutching) targetSurgeZ = maxSurgeZ;
            else if (braking && clutching) targetSurgeZ = 0.8f * maxSurgeZ;
            else if (!accelerating && !clutching) targetSurgeZ = 0.6f * maxSurgeZ; // Engine braking
            else if (clutching && !braking) targetSurgeZ = 0.2f * maxSurgeZ; // Reduced from 0.3
            else if (accelerating) targetSurgeZ = -vehicle.throttle * maxSurgeZ * 0.6f;
        }
        else
        {
            // FIX: Zero movement at idle. Revving the throttle while parked no longer pushes the camera backward.
            targetSurgeZ = 0f;
        }

        targetSurgeZ = Mathf.Clamp(targetSurgeZ, -maxSurgeZ, maxSurgeZ);

        // 🔥 FIX: DYNAMIC RECOVERY (Kills the delay)
        float dynamicSmooth;
        if (vehicle.speed < 5f)
        {
            dynamicSmooth = surgeSmoothness * 6f; // Instant snap at idle
        }
        else if (Mathf.Abs(targetSurgeZ) < 0.05f || targetSurgeZ < smoothedSurgeZ)
        {
            dynamicSmooth = surgeSmoothness * 3.5f; // Faster recovery when releasing levers
        }
        else
        {
            dynamicSmooth = surgeSmoothness; // Normal smooth entry into surge
        }

        smoothedSurgeZ = Mathf.Lerp(smoothedSurgeZ, targetSurgeZ, Time.deltaTime * dynamicSmooth);



        // --- 3. INERTIA PUNCH (Gear Shifts) ---



        currentShiftPitch = Mathf.Lerp(currentShiftPitch, 0f, Time.deltaTime * pitchRecoverySpeed);





        // --- 4. LAYERED NOISE (Noticeable Idle Sway) ---

        // FIX: Fade out the camera breathing wobble as speed increases so you don't feel disconnected from the world.
        float swayFade = Mathf.Clamp01(1f - (vehicle.speed / 15f));
        float idleBreath = Mathf.Sin(Time.time * breathingSwaySpeed) * breathingSwayAmount * swayFade;

        float swayX = idleBreath * 0.6f;
        float swayY = idleBreath * 0.4f;

        Vector3 basePos = cameraMountPoint.position;

        // FIX: Removed 'idleOffset' (the floating drone effect). 
        // Camera stays locked to the mount, only surging forward/back with the clutch.
        Vector3 surge = yawOnly * new Vector3(0f, 0f, smoothedSurgeZ);

        transform.position = basePos + surge;



        float speedFactor = Mathf.Clamp01(vehicle.speed / 120f);

        float currentJitter = Mathf.Lerp(0.01f, maxMicroJitter * 0.5f, speedFactor);



        float jitterX = 0f;

        float jitterY = 0f;





        // --- 5. HORIZON LOCK & APEX LOOK ---

        float currentBikeLean = 0f;

        if (bikeTransform != null)

        {

            Vector3 localEuler = bikeTransform.localEulerAngles;

            float rawLean = localEuler.z;

            if (rawLean > 180f) rawLean -= 360f;

            currentBikeLean = rawLean;

        }







        smoothedBikeLean = Mathf.SmoothDamp(

        smoothedBikeLean,

        currentBikeLean,

        ref bikeLeanVelocity,

        0.08f

    );




        float apexYaw = smoothedBikeLean * apexLookMultiplier;





        // --- 6. APPLY FINAL ROTATION ---
        float finalPitch = smoothedAccelPitch + swayX + jitterX + currentManualPitch;
        float finalYaw = swayY + jitterY + currentManualYaw + apexYaw;

        // FIX: Removed the double-yaw math entirely. 
        // cameraMountPoint ALREADY knows which way the bike is facing.
        Quaternion baseRotation = cameraMountPoint.rotation;

        // Apply our custom pitch and sway on top of the mount's world rotation
        Quaternion targetRotation = baseRotation * Quaternion.Euler(finalPitch, finalYaw, 0f);

        // The neckStiffness Slerp handles all the smoothing we need naturally
        Quaternion desiredRotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * neckStiffness * 1.5f
        );

        transform.rotation = desiredRotation;

    }

}

