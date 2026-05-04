using UnityEngine;
using TMPro;

public class GearDisplay : MonoBehaviour
{
    public TextMeshProUGUI gearText;

    void Update()
    {
        if (gearText == null) return;
        // 1. Find which vehicle is currently active in the scene
        var car = FindFirstObjectByType<CarEngineSimulator>();
        var bike = FindFirstObjectByType<BikeEngineSimulator>();

        int currentGear = 0;
        bool vehicleFound = false;

        // 2. Safely grab the gear from the active vehicle
        if (car != null && car.isActiveAndEnabled)
        {
            currentGear = car.currentGear;
            vehicleFound = true;
        }
        else if (bike != null && bike.isActiveAndEnabled)
        {
            currentGear = bike.currentGear;
            vehicleFound = true;
        }

        // 3. Translate the numbers into letters!
        if (vehicleFound)
        {
            if (currentGear == -1)
            {
                gearText.text = "Gear: R";
            }
            else if (currentGear == 0)
            {
                gearText.text = "Gear: N";
            }
            else
            {
                gearText.text = "Gear: " + currentGear;
            }
        }
        else
        {
            // Default to Neutral if we are in the main menu
            gearText.text = "Gear: N";
        }
    }
}