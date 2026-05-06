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

    public Transform frontAssembly;



    // ================= SPEED =================

    public float speed;

    public float wheelSpeed;



    // ================= INPUT =================

    public float throttle;

    public float clutch; // 0 = released, 1 = pulled



    public float throttleResponse = 8f;

    public float clutchSpeed = 6f;



    public float clutchInputRaw;

    public float brakeResponse = 5f;

    float smoothedBrakeInput = 0f;



    // ================= TRANSMISSION =================

    public int currentGear = 0;

    public int maxGear = 5;

    float[] gearRatios = { 0f, 12.5f, 9.0f, 7.0f, 5.5f, 4.5f, 3.8f };

    public float finalDriveRatio = 1f;


    // ================= CLUTCH =================

    [Header("Clutch")]

    public float bitePoint = 0.65f;

    public float maxClutchTorque = 250f;

    public float clutchStiffness = 30f;



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



    // ================= TORQUE =================

    public AnimationCurve torqueCurve = new AnimationCurve(

        new Keyframe(0.0f, 0.5f),

        new Keyframe(0.2f, 0.8f),

        new Keyframe(0.5f, 1.0f),

        new Keyframe(0.8f, 0.85f),

        new Keyframe(1.0f, 0.4f)

    );



    // --- TELEPORT VARIABLES ---

    private Vector3 spawnPosition;

    private Quaternion spawnRotation;



    void Awake()

    {

        // Snapshot the exact start line location

        spawnPosition = transform.position;

        spawnRotation = transform.rotation;

    }



    void Start()

    {

        GetComponent<Rigidbody>().centerOfMass = new Vector3(0, -0.8f, 0);

    }



    void OnEnable()

    {




        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)

        {

            rb.isKinematic = true;



            transform.position = spawnPosition;

            transform.rotation = spawnRotation;



            Physics.SyncTransforms();



            // THE FIX: Turn physics back on BEFORE clearing momentum!

            rb.isKinematic = false;

            rb.velocity = Vector3.zero;

            rb.angularVelocity = Vector3.zero;

        }



        engineRunning = false;

        currentGear = 0;

        rpm = 0f;

        speed = 0f;

        wheelSpeed = 0f;

        throttle = 0f;

        clutch = 0f;

    }



    void Update()

    {

        // Safety freeze for when the game is paused. 

        // No haptic spam allowed here!

        if (Time.timeScale == 0f) return;



        HandleInput();

        TryStarterMotor();

    }



    void OnDisable()

    {

        if (Gamepad.current != null)

        {

            Gamepad.current.SetMotorSpeeds(0f, 0f);

        }



        // Failsafe wipe for when script is disabled directly

        engineRunning = false;

        currentGear = 0;

        rpm = 0f;

        speed = 0f;

        wheelSpeed = 0f;

        throttle = 0f;

        clutch = 0f;



        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null) rb.velocity = Vector3.zero;

    }



    void FixedUpdate()

    {

        SimulateEngine();

        CheckStall();

    }



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



            if (clutchInputRaw > 0.8f)

            {

                if (Gamepad.current.buttonEast.wasPressedThisFrame) ShiftUp();

                if (Gamepad.current.buttonWest.wasPressedThisFrame) ShiftDown();

            }



            if (Gamepad.current.buttonSouth.wasPressedThisFrame)

                starterPressed = true;

        }



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

        else steerInput = Input.GetAxis("Horizontal");



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



    void SimulateEngine()

    {

        float gearRatio = gearRatios[currentGear] * finalDriveRatio; // <-- ADD MULTIPLIER

        float engineRadS = rpm * Mathf.PI / 30f;

        float wheelRadS = wheelSpeed;



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



        float engagement = Mathf.Clamp01(1f - clutch);

        float currentClutchCapacity = maxClutchTorque * engagement;

        float transmissionRadS = wheelRadS * gearRatio;

        float slipSpeed = engineRadS - transmissionRadS;



        float activeClutchStiffness = 100f;

        float transmittedTorque = slipSpeed * activeClutchStiffness * engagement;

        transmittedTorque = Mathf.Clamp(transmittedTorque, -currentClutchCapacity, currentClutchCapacity);



        float maxSyncTorque = ((Mathf.Abs(slipSpeed) / Time.fixedDeltaTime) * engineInertia) + Mathf.Abs(totalEngineTorque);

        transmittedTorque = Mathf.Clamp(transmittedTorque, -maxSyncTorque, maxSyncTorque);



        if (currentGear == 0 || !engineRunning) transmittedTorque = 0f;



        float closedThrottleFactor = engineRunning ? (1f - throttle) : 1f;

        float pumpingLoss = (rpm / maxRPM) * 80f * closedThrottleFactor;

        float dynamicEngineDrag = engineFriction + pumpingLoss;



        float netEngineTorque = totalEngineTorque - transmittedTorque - dynamicEngineDrag;

        float engineAccel = netEngineTorque / engineInertia;

        engineRadS += engineAccel * Time.fixedDeltaTime;



        rpm = engineRadS * 30f / Mathf.PI;

        rpm = Mathf.Clamp(rpm, 0f, maxRPM);



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



        Rigidbody rb = GetComponent<Rigidbody>();

        float realForwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

        speed = Mathf.Max(0f, realForwardSpeed * 3.6f);



        wheelRadS = (speed / 3.6f) / wheelRadius;

        wheelSpeed = wheelRadS;



        if (frontAssembly != null)

        {

            Quaternion targetSteer = Quaternion.Euler(0, steerInput * maxSteerAngle, 0);

            frontAssembly.localRotation = Quaternion.Slerp(frontAssembly.localRotation, targetSteer, Time.fixedDeltaTime * 12f);

        }



        if (currentGear > 0 && clutch < 0.75f)

        {

            float driveForce = netWheelTorque / wheelRadius;

            Vector3 forwardFlat = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

            rb.AddForce(forwardFlat * driveForce, ForceMode.Force);

        }



        if (currentBrakeForce > 10f)

        {

            rb.drag = 2.0f;

        }

        else if (!engineRunning)

        {

            rb.drag = (currentGear == 0) ? 0.005f : 5.0f;

        }

        else if (clutch > 0.8f)

        {

            rb.drag = 0.005f;

        }

        else

        {

            rb.drag = (throttle < 0.1f) ? 0.15f : 0.05f;

        }



        float cleanSteer = steerInput;

        if (Mathf.Abs(cleanSteer) < 0.2f)

        {

            cleanSteer = 0f;

        }



        if (speed > 1f)

        {

            float turnAmount = cleanSteer * 40f * Time.fixedDeltaTime;

            float targetLean = -cleanSteer * 35f;

            float currentLean = transform.eulerAngles.z;

            if (currentLean > 180f) currentLean -= 360f;

            float newLean = Mathf.Lerp(currentLean, targetLean, Time.fixedDeltaTime * 5.5f);



            Vector3 newEuler = transform.eulerAngles;

            newEuler.y += turnAmount;

            newEuler.z = newLean;

            rb.MoveRotation(Quaternion.Euler(newEuler));



            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

            Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

            float forwardSpeedMath = Vector3.Dot(flatVelocity, flatForward);



            Vector3 targetVelocity = (flatForward * forwardSpeedMath) + new Vector3(0, rb.velocity.y, 0);

            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.fixedDeltaTime * 15f);



            rb.angularVelocity = new Vector3(rb.angularVelocity.x, 0f, 0f);

        }

        else

        {

            Vector3 currentEuler = rb.rotation.eulerAngles;

            Quaternion upright = Quaternion.Euler(currentEuler.x, currentEuler.y, 0f);

            rb.MoveRotation(Quaternion.Slerp(rb.rotation, upright, Time.fixedDeltaTime * 5f));



            if (throttle < 0.05f && rb.velocity.magnitude < 1.5f)

            {

                rb.velocity = new Vector3(0, rb.velocity.y, 0);

                rb.angularVelocity = Vector3.zero;

            }

        }



        if (speed < 0.1f) speed = 0f;

        if (wheelSpeed < 0.1f) wheelSpeed = 0f;

        if (clutch < 0.001f) clutch = 0f;

    }

}