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

    public Transform frontAssembly; // The visual group for the Handlebars/Fork/Wheel



    // ================= SPEED =================

    public float speed;

    public float wheelSpeed;



    // ================= INPUT =================

    public float throttle;

    public float clutch; // 0 = released, 1 = pulled



    public float throttleResponse = 8f;

    public float clutchSpeed = 6f;



    public float clutchInputRaw; // raw lever input (instant, not smoothed)

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



    public DashboardController dash;



    // ================= TORQUE (ROYAL ENFIELD TUNE) =================

    public AnimationCurve torqueCurve = new AnimationCurve(

        new Keyframe(0.0f, 0.5f),  // Idle RPM

        new Keyframe(0.2f, 0.8f),  // Low RPM

        new Keyframe(0.5f, 1.0f),  // Mid RPM

        new Keyframe(0.8f, 0.85f), // High RPM

        new Keyframe(1.0f, 0.4f)   // Redline

    );



    void Start()

    {

        // The Pendulum Trick: Forces the bike to balance itself naturally!

        GetComponent<Rigidbody>().centerOfMass = new Vector3(0, -0.8f, 0);

    }



    // ================= UPDATE (Inputs & State) =================

    void Update()

    {
        if (Time.timeScale == 0f) return;
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

            if (clutchInputRaw > 0.8f)

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



        if (clutchInputRaw > 0.8f)

        {

            if (Input.GetKeyDown(KeyCode.E)) ShiftUp();

            if (Input.GetKeyDown(KeyCode.Q)) ShiftDown();

        }



        if (Input.GetKeyDown(KeyCode.R))
            starterPressed = true;

        // THE FIX: Completely kill the throttle input if the engine is off!
        if (!engineRunning)
        {
            throttleInput = 0f;
        }

        throttle = Mathf.Lerp(throttle, throttleInput, Time.deltaTime * throttleResponse);

        clutch = Mathf.Lerp(clutch, clutchInput, Time.deltaTime * clutchSpeed);



        if (throttle < 0.001f) throttle = 0f;

        if (clutch < 0.001f) clutch = 0f;



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



        // FIX: If the engine is off, the clutch/transmission should not fight the wheels.
        // This stops the 'shiver' when rolling with the ignition off.
        if (currentGear == 0 || !engineRunning) transmittedTorque = 0f;



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



        // --- OVERHAULED RIGIDBODY PHYSICS ---

        Rigidbody rb = GetComponent<Rigidbody>();



        // 1. SYNC THE GAUGES TO REALITY 

        float realForwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        speed = Mathf.Max(0f, realForwardSpeed * 3.6f);



        // Force the engine math to match the physical speed of the heavy bike

        wheelRadS = (speed / 3.6f) / wheelRadius;

        wheelSpeed = wheelRadS;



        // 2. VISUAL STEERING

        if (frontAssembly != null)

        {

            Quaternion targetSteer = Quaternion.Euler(0, steerInput * maxSteerAngle, 0);

            frontAssembly.localRotation = Quaternion.Slerp(frontAssembly.localRotation, targetSteer, Time.fixedDeltaTime * 10f);

        }



        // 3. PUSH THE BIKE FORWARD 

        // THE JITTER KILLER: We completely sever the physical drive force if the clutch is pulled in!

        if (currentGear > 0 && clutch < 0.75f)

        {

            float driveForce = netWheelTorque / wheelRadius;



            // Flatten the forward direction so the engine ONLY pushes horizontally

            Vector3 forwardFlat = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

            rb.AddForce(forwardFlat * driveForce, ForceMode.Force);

        }



        // --- IMPROVED COASTING & STALL PHYSICS ---
        if (currentBrakeForce > 10f)
        {
            rb.drag = 2.0f; // Solid braking
        }
        else if (!engineRunning)
        {
            // If the engine is off and in gear, it should be VERY hard to move (Engine compression)
            // If in neutral (Gear 0), it should roll freely.
            rb.drag = (currentGear == 0) ? 0.05f : 5.0f;
        }
        else if (clutch > 0.9f)
        {
            rb.drag = 0.05f; // Smooth glide
        }
        else
        {
            rb.drag = (throttle < 0.1f) ? 0.15f : 0.05f;
        }



        // --- 4. ARCADE-PERFECT STEERING & ANTI-DRIFT ---



        // A. KILL THE GAMEPAD STICK DRIFT (The Deadzone)

        float cleanSteer = steerInput;

        if (Mathf.Abs(cleanSteer) < 0.2f)

        {

            cleanSteer = 0f;

        }



        // B. STEERING & LEANING

        // B. STEERING & LEANING
        if (speed > 1f)
        {
            // 1. Calculate Turn & Lean SAFELY (Avoid Euler Wrapping Jitter)
            float turnAmount = cleanSteer * 60f * Time.fixedDeltaTime;

            float targetLean = -cleanSteer * 35f;
            float currentLean = transform.eulerAngles.z;
            if (currentLean > 180f) currentLean -= 360f;
            float newLean = Mathf.Lerp(currentLean, targetLean, Time.fixedDeltaTime * 8f);

            // Construct new rotation by reading transform, modifying Y/Z, and leaving X alone
            Vector3 newEuler = transform.eulerAngles;
            newEuler.y += turnAmount;
            newEuler.z = newLean;
            rb.MoveRotation(Quaternion.Euler(newEuler));

            // 2. THE "TRON" GRIP (Smoothed to stop collision jitter)
            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

            float forwardSpeedMath = Vector3.Dot(flatVelocity, flatForward);

            // Instead of instantaneous override, lerp it fast so suspension/bumps aren't severed instantly
            Vector3 targetVelocity = (flatForward * forwardSpeedMath) + new Vector3(0, rb.velocity.y, 0);
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 15f);

            // --- FIX 1: KILLS THE OFFICE CHAIR EFFECT ---
            // Only kill Y and Z angular velocity. Killing X prevents the bike from naturally pitching over bumps!
            rb.angularVelocity = new Vector3(rb.angularVelocity.x, 0f, 0f);
        }

        else
        {
            // Stand up straight when stopped
            Vector3 currentEuler = rb.rotation.eulerAngles;
            Quaternion upright = Quaternion.Euler(currentEuler.x, currentEuler.y, 0f);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, upright, Time.fixedDeltaTime * 5f));

            // THE UPGRADED PARKING BRAKE
            // If we are applying zero throttle and moving extremely slowly, just lock it down.
            // This stops the 'Pendulum Trick' from causing ghost drift when the clutch is pulled.
            if (throttle < 0.05f && rb.velocity.magnitude < 1.5f)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0);
                rb.angularVelocity = Vector3.zero;
            }
        }



        // --- FIX 2: ABSOLUTE INSPECTOR CLEANUP ---

        // By putting this at the VERY END of FixedUpdate, we guarantee 0 on the UI

        if (speed < 0.1f) speed = 0f;

        if (wheelSpeed < 0.1f) wheelSpeed = 0f;

        if (clutch < 0.001f) clutch = 0f;

    }

}