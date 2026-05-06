using UnityEngine;
using UnityEngine.InputSystem;

public class CarEngineSimulator : MonoBehaviour
{
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    public Transform meshFL;
    public Transform meshFR;
    public Transform meshRL;
    public Transform meshRR;

    public bool engineRunning = false;
    public float rpm;
    public float idleRPM = 900f;
    public float maxRPM = 7000f;
    public float stallRPM = 600f;
    public float engineMaxTorque = 300f;

    public float wheelSpeed;
    public float throttle;
    public float clutch;
    public float clutchInputRaw;

    public float steerInput;
    private float smoothedSteer;
    public float maxSteerAngle = 35f;

    public float brakeInput;
    public float brakePower = 3000f;
    public float baseEngineBraking = 500f;

    public int currentGear = 0;
    public int maxGear = 5;
    float[] gearRatios = { 0f, 3.16f, 1.88f, 1.29f, 0.97f, 0.73f };
    public float reverseGearRatio = -3.2f;
    public float finalDriveRatio = 3.4f;

    public float bitePoint = 0.65f;
    public float maxClutchTorque = 500f;

    public float downforceMultiplier = 50f;

    private bool starterPressed;
    private Rigidbody rb;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    void Start()
    {
        rb.centerOfMass = new Vector3(0, -0.2f, 0.4f);
    }

    void OnEnable()
    {

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
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        HandleInput();
        TryStarterMotor();
        SyncVisualWheels();

        wheelSpeed = rb.velocity.magnitude * 3.6f;
    }

    void FixedUpdate()
    {
        SimulateEngineAndDrivetrain();
        CheckStall();
        ApplyDownforce();
    }

    void ApplyDownforce()
    {
        rb.AddForce(-transform.up * downforceMultiplier * rb.velocity.magnitude);
    }

    void HandleInput()
    {
        throttle = 0f;
        brakeInput = 0f;
        steerInput = 0f;
        starterPressed = false;

        if (Gamepad.current != null)
        {
            float rt = Gamepad.current.rightTrigger.ReadValue();
            float lt = Gamepad.current.leftTrigger.ReadValue();
            bool rbKey = Gamepad.current.rightShoulder.isPressed;

            clutchInputRaw = lt;

            if (rbKey) brakeInput = rt;
            else throttle = rt;

            steerInput = Gamepad.current.leftStick.x.ReadValue();

            if (Gamepad.current.buttonSouth.wasPressedThisFrame) starterPressed = true;

            if (clutchInputRaw > 0.8f)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame) ShiftUp();
                if (Gamepad.current.buttonWest.wasPressedThisFrame) ShiftDown();
            }
        }
        else
        {
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

        if (!engineRunning) throttle = 0f;

        clutch = Mathf.Lerp(clutch, clutchInputRaw, Time.deltaTime * 6f);
    }

    void TryStarterMotor()
    {
        if (!starterPressed) return;
        if (clutchInputRaw < 0.9f && currentGear != 0) return;

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
            if (currentGear > 0) rpm = Mathf.Max(rpm * 0.85f, idleRPM);
        }
    }

    void ShiftDown()
    {
        if (currentGear > -1)
        {
            currentGear--;
            if (currentGear != 0) rpm = Mathf.Min(rpm * 1.15f, maxRPM);
        }
    }

    void SimulateEngineAndDrivetrain()
    {
        float currentWheelRPM = (frontLeft.rpm + frontRight.rpm + rearLeft.rpm + rearRight.rpm) / 4f;
        float activeRatio = (currentGear == -1) ? reverseGearRatio : gearRatios[currentGear];
        float totalRatio = activeRatio * finalDriveRatio;
        float engineTargetRPM = currentWheelRPM * totalRatio;
        float engagement = Mathf.Clamp01(1f - clutch);

        if (engineRunning)
        {
            if (currentGear == 0)
            {
                rpm = Mathf.Lerp(rpm, idleRPM + (throttle * (maxRPM - idleRPM)), Time.fixedDeltaTime * 2f);
            }
            else
            {
                float drivenRPM = Mathf.Lerp(idleRPM + (throttle * (maxRPM - idleRPM)), engineTargetRPM, engagement);
                rpm = Mathf.Clamp(drivenRPM, 0, maxRPM);
            }
        }
        else
        {
            rpm = Mathf.Lerp(rpm, 0, Time.fixedDeltaTime * 2f);
        }

        float normalizedRPM = Mathf.InverseLerp(0, maxRPM, rpm);
        float torqueCurve = 1f - Mathf.Pow(normalizedRPM - 0.6f, 2f);

        float motorTorque = 0f;
        float engineBrakingForce = 0f;

        if (engineRunning && currentGear != 0)
        {
            if (engineTargetRPM > maxRPM && throttle > 0f)
            {
                motorTorque = 0f;
                engineBrakingForce = 2000f;
            }
            else if (throttle > 0f)
            {
                float gameyTorqueBoost = Mathf.Lerp(1f, 2.5f, (currentGear - 1f) / (maxGear - 1f));
                motorTorque = throttle * engineMaxTorque * torqueCurve * totalRatio * engagement * gameyTorqueBoost;
            }
            else
            {
                engineBrakingForce = baseEngineBraking * Mathf.Abs(totalRatio) * engagement * (rpm / maxRPM);
            }
        }

        frontLeft.motorTorque = motorTorque / 4f;
        frontRight.motorTorque = motorTorque / 4f;
        rearLeft.motorTorque = motorTorque / 4f;
        rearRight.motorTorque = motorTorque / 4f;

        float finalBrake = brakeInput * brakePower;

        if (brakeInput < 0.1f && engineBrakingForce > 0f)
        {
            finalBrake = engineBrakingForce;
        }

        frontLeft.brakeTorque = finalBrake;
        frontRight.brakeTorque = finalBrake;
        rearLeft.brakeTorque = finalBrake;
        rearRight.brakeTorque = finalBrake;

        float currentSpeed = rb.velocity.magnitude * 3.6f;
        float speedSteerFactor = Mathf.Lerp(1.0f, 0.2f, currentSpeed / 120f);
        float dynamicMaxSteer = maxSteerAngle * speedSteerFactor;

        smoothedSteer = Mathf.Lerp(smoothedSteer, steerInput, Time.fixedDeltaTime * 5f);
        frontLeft.steerAngle = smoothedSteer * dynamicMaxSteer;
        frontRight.steerAngle = smoothedSteer * dynamicMaxSteer;
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