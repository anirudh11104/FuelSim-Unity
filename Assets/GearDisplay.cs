using UnityEngine;
using TMPro;

public class GearDisplay : MonoBehaviour
{
    

    public TMP_Text gearText;

    private BikeEngineSimulator bike;
    private CarEngineSimulator car;



    void Update()
    {
        if (gearText == null || ActiveVehicle.Current == null) return;

        bike = ActiveVehicle.Current.GetComponent<BikeEngineSimulator>();
        car = ActiveVehicle.Current.GetComponent<CarEngineSimulator>();

        int gear = 0;

        if (bike != null)
            gear = bike.currentGear;
        else if (car != null)
            gear = car.currentGear;

        gearText.text = (gear == 0) ? "Gear: N" : "Gear: " + gear;
    }
}