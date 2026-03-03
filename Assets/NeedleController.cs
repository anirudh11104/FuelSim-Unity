using UnityEngine;

public class NeedleController : MonoBehaviour
{
    public float minValue = 0f;
    public float maxValue = 8000f;

    [Header("Rotation Calibration")]
    public float minAngle = -120f;
    public float maxAngle = 120f;

    [Header("Physics Feel")]
    public float needleSnappiness = 15f;

    private float currentAngle;

    void Start()
    {
        // Start the needle at zero when the game boots
        currentAngle = minAngle;
    }

    public void SetValue(float value)
    {
        // Find out what percentage (0 to 1) the current value is at
        float t = Mathf.InverseLerp(minValue, maxValue, value);

        // Calculate where the needle *wants* to be
        float targetAngle = Mathf.Lerp(minAngle, maxAngle, t);

        // Smoothly glide the needle towards the target instead of teleporting
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * needleSnappiness);

        // Apply it to the graphic
        transform.localRotation = Quaternion.Euler(0, 0, -currentAngle);
    }
}