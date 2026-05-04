using UnityEngine;
using TMPro;

public class GearDisplay : MonoBehaviour
{
    [Header("Vehicle Links (Link BOTH here!)")]
    public BikeEngineSimulator vehicle;
    public CarEngineSimulator car;

    public TMP_Text gearText;

    // --- THE SMART SWITCH ---
    // Automatically detects if you are driving the car instead of the bike
    private bool UseCar
    {
        get
        {
            if (car != null && car.engineRunning) return true; // Car is turned on
            if (vehicle != null && vehicle.engineRunning) return false; // Bike is turned on

            // Fallback: If engines are off, check which vehicle is enabled in the scene
            if (car != null && car.gameObject.activeInHierarchy && (vehicle == null || !vehicle.gameObject.activeInHierarchy)) return true;
            return false;
        }
    }

    private int CurrentGear => UseCar ? car.currentGear : (vehicle != null ? vehicle.currentGear : 0);

    void Update()
    {
        if ((vehicle != null || car != null) && gearText != null)
        {
            if (CurrentGear == 0)
                gearText.text = "Gear: N";
            else
                gearText.text = "Gear: " + CurrentGear;
        }
    }
}