using UnityEngine;

public class NeedleController : MonoBehaviour
{
    public BikeEngineSimulator bike;

    [Header("What is this needle reading?")]
    public bool isRPMNeedle = true; // Check box for RPM, uncheck for Speed

    [Header("Values")]
    public float minValue = 0f;
    public float maxValue = 8000f; // E.g., 8000 for RPM, 180 for Speed

    [Header("Rotation Calibration")]
    public float minAngle = -120f;
    public float maxAngle = 120f;

    [Header("3D Axis To Spin")]
    public Vector3 spinAxis = new Vector3(0, 0, 1); // 1 on the axis you want to spin (X, Y, or Z)

    [Header("Physics Feel")]
    public float needleSnappiness = 15f;

    private float currentAngle;

    void Start()
    {
        currentAngle = minAngle;
    }

    void Update() // Changed to Update so it reads automatically!
    {
        if (bike == null) return;

        // 1. Read the correct value from the engine
        float value = isRPMNeedle ? bike.rpm : bike.speed;

        // 2. Do the math
        float t = Mathf.InverseLerp(minValue, maxValue, value);
        float targetAngle = Mathf.Lerp(minAngle, maxAngle, t);
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * needleSnappiness);

        // 3. Spin the 3D mesh!
        transform.localRotation = Quaternion.Euler(spinAxis * currentAngle);
    }
}