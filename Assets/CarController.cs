using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Wheel Meshes (Visuals)")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Engine & Tuning Base")]
    public float motorTorque = 1500f; // Power
    public float brakeTorque = 3000f; // Stopping force
    public float maxSteeringAngle = 35f; // Turning radius

    private float currentMotorForce;
    private float currentBrakeForce;
    private float currentSteerAngle;

    void Start()
    {
        // Lower the center of mass so the car doesn't flip on every corner!
        GetComponent<Rigidbody>().centerOfMass = new Vector3(0, -0.5f, 0);
    }

    void FixedUpdate()
    {
        HandleInput();
        ApplyMotorTorque();
        ApplySteering();
        SyncVisualWheels();
    }

    void HandleInput()
    {
        float acceleration = 0f;
        float braking = 0f;
        float steering = 0f;

        // Uses the exact same Gamepad mapping as your motorcycle!
        if (Gamepad.current != null)
        {
            acceleration = Gamepad.current.rightTrigger.ReadValue();
            braking = Gamepad.current.leftTrigger.ReadValue();
            steering = Gamepad.current.leftStick.x.ReadValue();
        }
        else
        {
            // Keyboard fallback
            if (Input.GetKey(KeyCode.W)) acceleration = 1f;
            if (Input.GetKey(KeyCode.S)) braking = 1f;
            steering = Input.GetAxis("Horizontal");
        }

        currentMotorForce = acceleration * motorTorque;
        currentBrakeForce = braking * brakeTorque;
        currentSteerAngle = steering * maxSteeringAngle;
    }

    void ApplyMotorTorque()
    {
        // RWD (Rear-Wheel Drive) Setup. Perfect for drifting later!
        rearLeft.motorTorque = currentMotorForce;
        rearRight.motorTorque = currentMotorForce;

        // Apply brakes to all four wheels
        frontLeft.brakeTorque = currentBrakeForce;
        frontRight.brakeTorque = currentBrakeForce;
        rearLeft.brakeTorque = currentBrakeForce;
        rearRight.brakeTorque = currentBrakeForce;
    }

    void ApplySteering()
    {
        // Only steer the front wheels
        frontLeft.steerAngle = currentSteerAngle;
        frontRight.steerAngle = currentSteerAngle;
    }

    void SyncVisualWheels()
    {
        // Makes the 3D rims and tires spin and turn to match the invisible physics colliders
        UpdateSingleWheel(frontLeft, frontLeftMesh);
        UpdateSingleWheel(frontRight, frontRightMesh);
        UpdateSingleWheel(rearLeft, rearLeftMesh);
        UpdateSingleWheel(rearRight, rearRightMesh);
    }

    void UpdateSingleWheel(WheelCollider collider, Transform mesh)
    {
        if (mesh == null) return;
        Vector3 position;
        Quaternion rotation;

        collider.GetWorldPose(out position, out rotation);

        mesh.position = position;
        mesh.rotation = rotation;
    }
}