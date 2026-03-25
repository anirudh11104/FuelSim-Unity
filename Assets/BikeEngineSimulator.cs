using UnityEngine;
using UnityEngine.InputSystem;

public class BikeEngineSimulator : MonoBehaviour
{
    // ================= ENGINE =================
    [Header("Engine")]
    public bool engineRunning = false;
    public float rpm;
    public float idleRPM = 1200f;
    public float maxRPM = 8000f;
    public float stallRPM = 900f;

    public float steerInput;
    public float maxSteerAngle = 35f;

    // ================= SPEED =================
    public float speed;
    public float wheelSpeed;

    // ================= INPUT =================
    public float throttle;
    public float clutch; // 0 = released, 1 = pulled

    public float throttleResponse = 8f;
    public float clutchSpeed = 6f;

    float clutchInputRaw; // raw lever input (instant, not smoothed)
    public float brakeResponse = 5f; // Simulates hydraulic pressure buildup time
    float smoothedBrakeInput = 0f;

    // ================= TRANSMISSION =================
    public int currentGear = 0; // Starts in Neutral
    public int maxGear = 5;

    // TOTAL reduction ratios (Primary Drive * Gearbox * Final Drive)
    float[] gearRatios = { 0f, 12.5f, 9.0f, 7.0f, 5.5f, 4.5f };

    // ================= CLUTCH =================
    [Header("Clutch")]
    public float bitePoint = 0.65f; // friction zone start
    public float maxClutchTorque = 250f;    // Max torque clutch can hold before slipping
    public float clutchStiffness = 30f;     // Friction multiplier during slip

    // ================= FORCES & PHYSICS =================
    [Header("Physics Constants")]
    public float engineMaxTorque = 80f;
    public float engineInertia = 0.2f;
    public float engineFriction = 5f;
    public float idleGain = 0.05f;
    public float maxIdleTorque = 15f;

    public float wheelRadius = 0.33f;
    public float bikeMass = 250f;
    public float wheelMassInertia = 27f;

    public float brakePower = 200f;
    public float currentBrakeForce = 0f;

    // ================= TORQUE (ROYAL ENFIELD TUNE) =================
    public AnimationCurve torqueCurve = new AnimationCurve(
        new Keyframe(0.0f, 0.5f),  // Idle RPM
        new Keyframe(0.2f, 0.8f),  // Low RPM
        new Keyframe(0.5f, 1.0f),  // Mid RPM
        new Keyframe(0.8f, 0.85f), // High RPM
        new Keyframe(1.0f, 0.4f)   // Redline
    );

    // ================= UPDATE (Inputs & State) =================
    void Update()
    {
        HandleInput();
        TryStarterMotor();
        TryPushStart();
    }

    // ================= FIXED UPDATE (Physics Only) =================
    void FixedUpdate()
    {
        SimulateEngine();
        CheckStall();
    }

    // =================================================
    // INPUT
    // =================================================
    bool pushStartPressed;
    bool starterPressed;

    void HandleInput()
    {
        float throttleInput = 0;
        float clutchInput = 0;
        float brakeInput = 0;
        starterPressed = false;

        if (Gamepad.current != null)
        {
            float rt = Gamepad.current.rightTrigger.ReadValue();
            float lt = Gamepad.current.leftTrigger.ReadValue();
            clutchInputRaw = lt;
            bool rb = Gamepad.current.rightShoulder.isPressed;

            clutchInput = lt;

            if (rb) brakeInput = rt;
            else throttleInput = rt;

            // SHIFT ONLY IF CLUTCH LEVER PULLED
            if (clutchInput > 0.8f)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame) ShiftUp();
                if (Gamepad.current.buttonWest.wasPressedThisFrame) ShiftDown();
            }

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                starterPressed = true;
        }

        // keyboard fallback
        if (Input.GetKey(KeyCode.W)) throttleInput = 1;
        if (Input.GetKey(KeyCode.LeftShift)) clutchInput = 1;
        if (Input.GetKey(KeyCode.LeftShift)) clutchInputRaw = 1f;
        if (Input.GetKey(KeyCode.S)) brakeInput = 1;

        if (clutchInput > 0.8f)
        {
            if (Input.GetKeyDown(KeyCode.E)) ShiftUp();
            if (Input.GetKeyDown(KeyCode.Q)) ShiftDown();
        }

        if (Input.GetKeyDown(KeyCode.R))
            starterPressed = true;

        throttle = Mathf.Lerp(throttle, throttleInput, Time.deltaTime * throttleResponse);
        clutch = Mathf.Lerp(clutch, clutchInput, Time.deltaTime * clutchSpeed);

        smoothedBrakeInput = Mathf.Lerp(smoothedBrakeInput, brakeInput, Time.deltaTime * brakeResponse);
        float brakeStrength = smoothedBrakeInput * smoothedBrakeInput;

        if (Gamepad.current != null) steerInput = Gamepad.current.leftStick.x.ReadValue();
        else steerInput = Input.GetAxis("Horizontal"); // fallback for keyboard

        currentBrakeForce = brakeStrength * brakePower;
    }

    void TryStarterMotor()
    {
        if (!starterPressed) return;

        if (clutchInputRaw < 0.9f && currentGear != 0) return;

        if (engineRunning)
        {
            engineRunning = false;
        }
        else
        {
            engineRunning = true;
            rpm = idleRPM;
        }
    }

    void TryPushStart()
    {
        if (!pushStartPressed) return;

        if (!engineRunning && clutch > 0.9f && wheelSpeed > 5f)
        {
            engineRunning = true;
            rpm = Mathf.Max(idleRPM, wheelSpeed * 40f);
        }
    }

    void CheckStall()
    {
        if (!engineRunning) return;

        if (rpm <= 50f)
        {
            StallEngine();
            return;
        }

        if (rpm <= stallRPM && clutch < bitePoint && currentGear > 0)
        {
            StallEngine();
        }
    }

    void StallEngine()
    {
        engineRunning = false;
    }

    void ShiftUp()
    {
        if (currentGear < maxGear)
        {
            currentGear++;
            if (currentGear > 1)
            {
                rpm *= 0.90f;
                rpm = Mathf.Max(rpm, idleRPM * 0.8f);
            }
        }
    }

    void ShiftDown()
    {
        if (currentGear > 0)
        {
            currentGear--;
            if (currentGear > 0)
            {
                rpm *= 1.10f;
                rpm = Mathf.Min(rpm, maxRPM);
            }
        }
    }

    // =================================================
    // PHYSICS (Now uses FixedDeltaTime)
    // =================================================
    void SimulateEngine()
    {
        float gearRatio = gearRatios[currentGear];
        float engineRadS = rpm * Mathf.PI / 30f;
        float wheelRadS = wheelSpeed;

        // ---------------- ENGINE TORQUE ----------------
        float totalEngineTorque = 0f;

        if (engineRunning)
        {
            float normalizedRPM = Mathf.InverseLerp(0, maxRPM, rpm);
            float throttleTorque = throttle * torqueCurve.Evaluate(normalizedRPM) * engineMaxTorque;

            float idleError = idleRPM - rpm;
            float idleSupport = 0f;
            if (idleError > 0 && throttle < 0.05f)
            {
                idleSupport = Mathf.Min(idleError * idleGain, maxIdleTorque);
            }
            totalEngineTorque = throttleTorque + idleSupport;
        }

        // ---------------- CLUTCH ----------------
        float engagement = Mathf.Clamp01(1f - clutch);
        float currentClutchCapacity = maxClutchTorque * engagement;

        float transmissionRadS = wheelRadS * gearRatio;
        float slipSpeed = engineRadS - transmissionRadS;

        float transmittedTorque = slipSpeed * clutchStiffness * engagement;
        transmittedTorque = Mathf.Clamp(transmittedTorque, -currentClutchCapacity, currentClutchCapacity);

        float maxSyncTorque = (Mathf.Abs(slipSpeed) / Time.fixedDeltaTime) * engineInertia;
        transmittedTorque = Mathf.Clamp(transmittedTorque, -maxSyncTorque, maxSyncTorque);

        if (currentGear == 0) transmittedTorque = 0f;

        // ---------------- ENGINE ACCELERATION ----------------
        float closedThrottleFactor = engineRunning ? (1f - throttle) : 1f;
        float pumpingLoss = (rpm / maxRPM) * 80f * closedThrottleFactor;
        float dynamicEngineDrag = engineFriction + pumpingLoss;

        float netEngineTorque = totalEngineTorque - transmittedTorque - dynamicEngineDrag;
        float engineAccel = netEngineTorque / engineInertia;
        engineRadS += engineAccel * Time.fixedDeltaTime;

        rpm = engineRadS * 30f / Mathf.PI;
        rpm = Mathf.Clamp(rpm, 0f, maxRPM);

        // ---------------- WHEEL ACCELERATION ----------------
        float wheelDriveTorque = transmittedTorque * gearRatio;
        float linearSpeed = wheelRadS * wheelRadius;
        float aeroDragMain = (0.5f * 1.225f * 0.4f * linearSpeed * linearSpeed) * wheelRadius * 0.2f;
        float rollingResMain = (bikeMass * 9.81f * 0.005f) * wheelRadius;

        float maxBrakeTorqueAllowed = (wheelRadS / Time.fixedDeltaTime) * wheelMassInertia;
        float appliedBrakeTorque = Mathf.Min(currentBrakeForce, maxBrakeTorqueAllowed);

        float netWheelTorque = wheelDriveTorque - aeroDragMain - rollingResMain - appliedBrakeTorque;
        float wheelAccel = netWheelTorque / wheelMassInertia;
        wheelRadS += wheelAccel * Time.fixedDeltaTime;
        wheelRadS = Mathf.Max(0f, wheelRadS);

        wheelSpeed = wheelRadS;
        speed = wheelSpeed * wheelRadius * 3.6f;

        // --- 3D MOVEMENT & STEERING ---
        float moveSpeed = speed / 3.6f;

        // GHOST MOVEMENT FIX: Kill physical movement if in neutral or clutching
        // MOMENTUM FIX: Let the bike coast if you pull the clutch or hit neutral
        if (currentGear == 0 || clutch > 0.98f)
        {
            // Gradually slow down instead of instantly stopping (Coasting)
            speed = Mathf.Lerp(speed, 0f, Time.fixedDeltaTime * 0.5f);
            moveSpeed = speed / 3.6f;
        }

        transform.Translate(Vector3.forward * moveSpeed * Time.fixedDeltaTime);

        float speedTurnFactor = Mathf.Clamp(15f / Mathf.Max(speed, 1f), 0.2f, 1f);
        float turnAmount = steerInput * maxSteerAngle * speedTurnFactor * Time.fixedDeltaTime;

        if (speed > 5f)
        {
            transform.Rotate(Vector3.up * turnAmount * 1.5f);
        }
    }
}