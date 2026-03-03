using UnityEngine;

public class DashboardController : MonoBehaviour
{
    public BikeEngineSimulator vehicle;

    public NeedleController speedNeedle;
    public NeedleController rpmNeedle;

    void Update()
    {
        speedNeedle.SetValue(vehicle.speed);
        rpmNeedle.SetValue(vehicle.rpm);
    }
}
