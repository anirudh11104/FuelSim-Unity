using UnityEngine;

public class VisualSteeringWheel : MonoBehaviour
{
    [Header("Connections")]
    public CarEngineSimulator car;
    public Transform steeringWheelMesh;

    [Header("Settings")]
    public float maxWheelRotation = 270f; // A real WRX wheel turns about 1.5 times (540 degrees lock-to-lock)
    public float turnSpeed = 15f; // How snappy the wheel is

    private Vector3 initialRotation;
    private float currentRotation = 0f;

    void Start()
    {
        // Memorize the exact tilt and angle the wheel starts at so we don't accidentally flatten it
        if (steeringWheelMesh != null)
        {
            initialRotation = steeringWheelMesh.localEulerAngles;
        }
    }

    void Update()
    {
        if (car == null || steeringWheelMesh == null || Time.timeScale == 0f) return;

        // 1. Get the raw input from your Car script (-1 for left, 1 for right)
        // Note: Assuming your script has a public float 'steerInput'. 
        // If it's called something else like 'steerAxis', change it here!
        float targetRotation = car.steerInput * maxWheelRotation;

        // 2. Smoothly lerp the rotation so it doesn't instantly snap
        currentRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * turnSpeed);

        // 3. Apply the rotation ON TOP of the original tilt of the steering column
        // (Most 3D car models use the Z axis for steering wheel rotation, but if yours spins 
        // like a coin, swap the 'currentRotation' to the X or Y slot below!)
        steeringWheelMesh.localEulerAngles = new Vector3(
            initialRotation.x,
            initialRotation.y,
            initialRotation.z - currentRotation
        );
    }
}