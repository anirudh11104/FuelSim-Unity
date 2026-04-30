using UnityEngine;

public class CameraJitterTest : MonoBehaviour
{
    public Transform cameraMountPoint; // Drag your Vehicle's CameraMount here in the Inspector

    void LateUpdate()
    {
        if (cameraMountPoint == null) return;

        // HARD LOCK: Zero smoothing, zero slerping. 
        // We let the Rigidbody Interpolation do 100% of the work.
        transform.position = cameraMountPoint.position;
        transform.rotation = cameraMountPoint.rotation;
    }
}