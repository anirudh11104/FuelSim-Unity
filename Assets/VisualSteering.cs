using UnityEngine;

public class VisualSteering : MonoBehaviour
{
    public BikeEngineSimulator bike; // Drag your 'Vehicle' here in the Inspector
    public float maxVisualSteerAngle = 35f;
    public float steeringSmoothness = 5f;

    void Update()
    {
        if (bike == null) return;

        // We take the steerInput (-1 to 1) from your physics script
        float targetAngle = bike.steerInput * maxVisualSteerAngle;

        // Create the rotation (Usually on the Y axis for motorcycles)
        Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);

        // Smoothly rotate the handlebars so they don't just "snap"
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * steeringSmoothness);
    }
}