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
    public bool engineRunning = true;
    public float rpm;
    public float idleRPM = 900f;
    public float maxRPM = 7000f;
    public float engineMaxTorque = 250f;

    // ================= INPUT & GEARS =================
    public float throttle;
    public float steerInput;
    public float brakeInput;
    public float maxSteerAngle = 35f;
    public float brakePower = 3000f;

    public int currentGear = 1;
    float[] gearRatios = { 0f, 3.5f, 2.0f, 1.4f, 1.0f, 0.8f };
    public float finalDriveRatio = 3.4f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    void Update()
    {
        HandleInput();
        SyncVisualWheels();
    }

    void FixedUpdate()
    {
        SimulateCarEngine();
    }

    void HandleInput()
    {
        throttle = 0f;
        brakeInput = 0f;
        steerInput = 0f;

        // --- GAMEPAD INPUT ---
        if (Gamepad.current != null)
        {
            throttle = Gamepad.current.rightTrigger.ReadValue();
            brakeInput = Gamepad.current.leftTrigger.ReadValue();
            steerInput = Gamepad.current.leftStick.x.ReadValue();

            // Shifting with Buttons (East = Up, West = Down)
            if (Gamepad.current.buttonEast.wasPressedThisFrame && currentGear < 5) currentGear++;
            if (Gamepad.current.buttonWest.wasPressedThisFrame && currentGear > 0) currentGear--;
        }
        else
        {
            // --- KEYBOARD FALLBACK ---
            if (Input.GetKey(KeyCode.W)) throttle = 1f;
            if (Input.GetKey(KeyCode.S)) brakeInput = 1f;
            steerInput = Input.GetAxis("Horizontal");

            if (Input.GetKeyDown(KeyCode.E) && currentGear < 5) currentGear++;
            if (Input.GetKeyDown(KeyCode.Q) && currentGear > 0) currentGear--;
        }

        // Kill stick drift
        if (Mathf.Abs(steerInput) < 0.1f) steerInput = 0f;
    }

    void SimulateCarEngine()
    {
        float wheelRPM = (rearLeft.rpm + rearRight.rpm) / 2f;
        float totalRatio = gearRatios[currentGear] * finalDriveRatio;

        if (currentGear > 0)
        {
            rpm = wheelRPM * totalRatio;
            rpm = Mathf.Clamp(rpm, idleRPM, maxRPM);
        }
        else
        {
            rpm = idleRPM;
        }

        float normalizedRPM = Mathf.InverseLerp(0, maxRPM, rpm);
        float torqueCurve = Mathf.Lerp(1f, 0.5f, normalizedRPM);
        float motorTorque = throttle * engineMaxTorque * torqueCurve * totalRatio;

        if (currentGear > 0)
        {
            frontLeft.motorTorque = motorTorque / 4f;
            frontRight.motorTorque = motorTorque / 4f;
            rearLeft.motorTorque = motorTorque / 4f;
            rearRight.motorTorque = motorTorque / 4f;
        }
        else
        {
            frontLeft.motorTorque = 0;
            frontRight.motorTorque = 0;
            rearLeft.motorTorque = 0;
            rearRight.motorTorque = 0;
        }

        float appliedBrake = brakeInput * brakePower;
        frontLeft.brakeTorque = appliedBrake;
        frontRight.brakeTorque = appliedBrake;
        rearLeft.brakeTorque = appliedBrake;
        rearRight.brakeTorque = appliedBrake;

        frontLeft.steerAngle = steerInput * maxSteerAngle;
        frontRight.steerAngle = steerInput * maxSteerAngle;
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