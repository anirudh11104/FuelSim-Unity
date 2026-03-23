using UnityEngine;

public class HandlebarSteering : MonoBehaviour
{
    public BikeEngineSimulator bike; // Drag your Vehicle here
    public float maxVisualSteerAngle = 30f;

    void Update()
    {
        if (bike == null) return;

        // Rotate the handlebars locally based on the bike's steer input
        // Most models rotate on the Y axis (Up/Down) for steering
        transform.localRotation = Quaternion.Euler(0, bike.steerInput * maxVisualSteerAngle, 0);
    }
}