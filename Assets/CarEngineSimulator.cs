using UnityEngine;
using UnityEngine.InputSystem;

public class CarEngineSimulator : MonoBehaviour
{
    // ================= WHEELS =================
    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Visual Meshes")]
    public Transform meshFL;
    public Transform meshFR;
    public Transform meshRL;
    public Transform meshRR;

    // ================= ENGINE =================
    [Header("Engine")]
    public bool engineRunning = false;
    public float rpm;
    public float idleRPM = 900f;
    public float maxRPM = 7000f;
    public float stallRPM = 600f;
    public float engineMaxTorque = 300f;

    // ================= SPEED & INPUT =================
    public float wheelSpeed;
    public float throttle;
    public float clutch; // 0 = released, 1 = pulled
    public float clutchInputRaw;

    public float steerInput;
    private float smoothedSteer; // Kills the bouncing!
    public float maxSteerAngle = 35f;

    public float brakeInput;
    public float brakePower = 3000f;

    // ================= TRANSMISSION =================
    public int currentGear = 0; // Starts in Neutral
    public int maxGear = 5;
    float[] gearRatios = { 0f, 3.16f, 1.88f, 1.29f, 0.97f, 0.73f };
    public float reverseGearRatio = -3.2f;
    public float finalDriveRatio = 3.4f;

    [Header("Clutch Physics")]
    public float bitePoint = 0.65f;
    public float maxClutchTorque = 500f;

    private bool starterPressed;
    private Rigidbody rb;

    // --- TELEPORT VARIABLES ---
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Take a snapshot of the exact starting position when the scene loads
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    void Start()
    {
        rb.centerOfMass = new Vector3(0, -0.1f, 0);
    }

    void OnEnable()
    {
        Time.timeScale = 1f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;

            frontLeft.enabled = false;
            frontRight.enabled = false;
            rearLeft.enabled = false;
            rearRight.enabled = false;

            transform.position = spawnPosition;
            transform.rotation = spawnRotation;

            Physics.SyncTransforms();

            frontLeft.enabled = true;
            frontRight.enabled = true;
            rearLeft.enabled = true;
            rearRight.enabled = true;

            // THE FIX: Turn physics back on BEFORE clearing momentum!
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        engineRunning = false;
        currentGear = 0;
        rpm = 0f;
        throttle = 0f;
        clutch = 0f;
        brakeInput = 0f;
        wheelSpeed = 0f;
        smoothedSteer = 0f;
    }

    void OnDisable()
    {
        // Kill controller rumble
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    void Update()
    {
        // Safety freeze for when the game is paused. 
        // No haptic spam allowed here!
        if (Time.timeScale == 0f) return;

        HandleInput();
        TryStarterMotor();
        SyncVisualWheels();
    }

    void FixedUpdate()
    {
        SimulateEngineAndDrivetrain();
        CheckStall();
    }

    void HandleInput()
    {
        throttle = 0f;
        brakeInput = 0f;
        steerInput = 0f;
        starterPressed = false;

        // --- GAMEPAD INPUT ---
        if (Gamepad.current != null)
        {
            float rt = Gamepad.current.rightTrigger.ReadValue();
            float lt = Gamepad.current.leftTrigger.ReadValue();
            bool rbKey = Gamepad.current.rightShoulder.isPressed;

            clutchInputRaw = lt; // Left Trigger is strictly Clutch

            // Right Trigger is Throttle, UNLESS Right Shoulder is held, then it's Brake
            if (rbKey) brakeInput = rt;
            else throttle = rt;

            steerInput = Gamepad.current.leftStick.x.ReadValue();

            if (Gamepad.current.buttonSouth.wasPressedThisFrame) starterPressed = true;

            // SHIFT ONLY IF CLUTCH LEVER PULLED
            if (clutchInputRaw > 0.8f)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame) ShiftUp();
                if (Gamepad.current.buttonWest.wasPressedThisFrame) ShiftDown();
            }
        }
        else
        {
            // --- KEYBOARD FALLBACK ---
            if (Input.GetKey(KeyCode.W)) throttle = 1f;
            if (Input.GetKey(KeyCode.S)) brakeInput = 1f;
            if (Input.GetKey(KeyCode.LeftShift)) clutchInputRaw = 1f; else clutchInputRaw = 0f;
            steerInput = Input.GetAxis("Horizontal");

            if (Input.GetKeyDown(KeyCode.R)) starterPressed = true;

            if (clutchInputRaw > 0.8f)
            {
                if (Input.GetKeyDown(KeyCode.E)) ShiftUp();
                if (Input.GetKeyDown(KeyCode.Q)) ShiftDown();
            }
        }

        // Kill throttle if engine is off
        if (!engineRunning) throttle = 0f;

        // Smooth the clutch
        clutch = Mathf.Lerp(clutch, clutchInputRaw, Time.deltaTime * 6f);
    }

    void TryStarterMotor()
    {
        if (!starterPressed) return;
        if (clutchInputRaw < 0.9f && currentGear != 0) return; // Must hold clutch if in gear

        if (engineRunning)
            engineRunning = false;
        else
        {
            engineRunning = true;
            rpm = idleRPM;
        }
    }

    void CheckStall()
    {
        if (!engineRunning) return;

        // If RPM drops too low while in gear and clutch is engaged
        if (rpm <= stallRPM && clutch < bitePoint && currentGear != 0)
        {
            engineRunning = false;
        }
    }

    void ShiftUp()
    {
        if (currentGear < maxGear)
        {
            currentGear++;
            // Don't rev match if we are just shifting into Neutral (0)
            if (currentGear > 0) rpm = Mathf.Max(rpm * 0.85f, idleRPM);
        }
    }

    void ShiftDown()
    {
        // Allow the gear to drop to -1 (Reverse)
        if (currentGear > -1)
        {
            currentGear--;
            // Rev match as long as we aren't shifting into Neutral
            if (currentGear != 0) rpm = Mathf.Min(rpm * 1.15f, maxRPM);
        }
    }

    void SimulateEngineAndDrivetrain()
    {
        // 1. Calculate wheel speed (YOU WERE MISSING THIS LINE)
        float currentWheelRPM = (frontLeft.rpm + frontRight.rpm + rearLeft.rpm + rearRight.rpm) / 4f;

        // 2. --- NEW RATIO LOOKUP ---
        // If gear is -1, use the reverse ratio. Otherwise, use the standard array.
        float activeRatio = (currentGear == -1) ? reverseGearRatio : gearRatios[currentGear];
        float totalRatio = activeRatio * finalDriveRatio;

        float engineTargetRPM = currentWheelRPM * totalRatio;

        // 3. Clutch Logic (Blending Idle with Wheel Speed)
        float engagement = Mathf.Clamp01(1f - clutch);

        if (engineRunning)
        {
            if (currentGear == 0)
            {
                // Neutral revving
                rpm = Mathf.Lerp(rpm, idleRPM + (throttle * (maxRPM - idleRPM)), Time.fixedDeltaTime * 2f);
            }
            else
            {
                // In gear: RPM is a blend of your throttle and the physical wheel speed
                float drivenRPM = Mathf.Lerp(idleRPM + (throttle * (maxRPM - idleRPM)), engineTargetRPM, engagement);
                rpm = Mathf.Clamp(drivenRPM, 0, maxRPM);
            }
        }
        else
        {
            rpm = Mathf.Lerp(rpm, 0, Time.fixedDeltaTime * 2f); // Engine dies
        }

        // 4. Torque Calculation (WRX Turbo Curve)
        float normalizedRPM = Mathf.InverseLerp(0, maxRPM, rpm);

        // Simulates turbo lag down low, massive boost peaking at 60% RPM (~4300 RPM), and slight taper at redline
        float torqueCurve = 1f - Mathf.Pow(normalizedRPM - 0.6f, 2f);

        // 5. --- THE REVERSE FIX ---
        // I changed "currentGear > 0" to "currentGear != 0"
        // Now it applies power in 1st-5th AND in Reverse (-1), but not in Neutral (0)
        float motorTorque = 0f;
        if (engineRunning && currentGear != 0)
        {
            motorTorque = throttle * engineMaxTorque * torqueCurve * totalRatio * engagement;
        }

        // Apply Torque to all 4 wheels
        frontLeft.motorTorque = motorTorque / 4f;
        frontRight.motorTorque = motorTorque / 4f;
        rearLeft.motorTorque = motorTorque / 4f;
        rearRight.motorTorque = motorTorque / 4f;

        // 6. Braking
        float appliedBrake = brakeInput * brakePower;
        frontLeft.brakeTorque = appliedBrake;
        frontRight.brakeTorque = appliedBrake;
        rearLeft.brakeTorque = appliedBrake;
        rearRight.brakeTorque = appliedBrake;

        // 7. SMOOTHED STEERING
        smoothedSteer = Mathf.Lerp(smoothedSteer, steerInput, Time.fixedDeltaTime * 5f);
        frontLeft.steerAngle = smoothedSteer * maxSteerAngle;
        frontRight.steerAngle = smoothedSteer * maxSteerAngle;
    }

    void SyncVisualWheels()
    {
        UpdateSingleWheel(frontLeft, meshFL);
        UpdateSingleWheel(frontRight, meshFR);
        UpdateSingleWheel(rearLeft, meshRL);
        UpdateSingleWheel(rearRight, meshRR);
    }

    void UpdateSingleWheel(WheelCollider col, Transform mesh)
    {
        if (mesh == null) return;
        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}