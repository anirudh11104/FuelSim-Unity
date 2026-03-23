using UnityEngine;
using TMPro;

public class GearDisplay : MonoBehaviour
{
    public BikeEngineSimulator vehicle;
    public TMP_Text gearText;

    void Update()
    {
        if (vehicle != null && gearText != null)
        {
            if (vehicle.currentGear == 0)
            {
                gearText.text = "GEAR: N";
            }
            else
            {
                gearText.text = "GEAR: " + vehicle.currentGear;
            }
        }
    }
}
