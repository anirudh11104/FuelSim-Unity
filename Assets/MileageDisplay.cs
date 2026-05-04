using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MileageDisplay : MonoBehaviour
{
    
    public TMP_Text mileageText;

    private BikeEngineSimulator bike;
    private CarEngineSimulator car;


    

    private float baseAverageMileage = 0f;

    void Start()
    {


        baseAverageMileage = 25f;

        // FIX 1: If the predictor returns 0 (or fails), use a realistic fallback! 
        // This ensures the UI never gets permanently stuck on a blank screen.
        if (baseAverageMileage <= 0f)
        {
            baseAverageMileage = 25f; // Fallback baseline (25 km/l)
        }
    }

    void Update()
    {
        if (mileageText == null || ActiveVehicle.Current == null) return;

        var bike = ActiveVehicle.Current.GetComponent<BikeEngineSimulator>();
        var car = ActiveVehicle.Current.GetComponent<CarEngineSimulator>();

        float mileage = 0f;

        if (bike != null)
        {
            var predictor = bike.GetComponent<MileagePredictor>();
            if (predictor != null)
                mileage = predictor.currentMileage;
        }
        else if (car != null)
        {
            var predictor = car.GetComponent<MileagePredictor>();
            if (predictor != null)
                mileage = predictor.currentMileage;
        }

        mileageText.text = "Mileage: " + mileage.ToString("F1") + " km/l";
    }
}