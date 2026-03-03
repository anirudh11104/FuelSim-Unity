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
            gearText.text = "GEAR: " + vehicle.currentGear;
        }
    }
}
