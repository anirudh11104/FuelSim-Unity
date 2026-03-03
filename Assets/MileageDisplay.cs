using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MileageDisplay : MonoBehaviour
{
    public MileagePredictor predictor;
    public TMP_Text mileageText;

    [Header("Link Your Bike!")]
    public BikeEngineSimulator bike; // We need to watch the engine's real-time stats

    [Header("UI Fallbacks")]
    public Slider engineCCSlider;
    public Slider weightSlider;

    private float baseAverageMileage = 0f;

    void Start()
    {
        // 1. Calculate the BASE average mileage ONCE using your ML model
        float engineCC = (engineCCSlider != null) ? engineCCSlider.value : 350f;
        float weight = (weightSlider != null) ? weightSlider.value : 200f;

        if (predictor != null)
        {
            baseAverageMileage = predictor.PredictMileage(engineCC, weight);
        }
    }

    void Update()
    {
        // Safety check
        if (bike == null || mileageText == null || baseAverageMileage == 0f) return;

        float instantMileage = 0f;

        // 2. Only calculate mileage if the engine is on and moving
        if (bike.engineRunning && bike.speed > 2f)
        {
            // --- THE REAL-TIME MATH ---
            // High RPM = Worse Fuel Economy (Dividing by baseline 3500 RPM)
            float rpmFactor = Mathf.Clamp(bike.rpm / 3500f, 0.5f, 3.0f);

            // High Gear = Better Fuel Economy (Bonus multiplier for 5th gear)
            float gearFactor = Mathf.Clamp(bike.currentGear / (float)bike.maxGear, 0.3f, 1.2f);

            // Heavy Throttle = Worse Fuel Economy
            float throttlePenalty = Mathf.Lerp(1.0f, 0.6f, bike.throttle);

            // Calculate the final fluctuating number
            instantMileage = (baseAverageMileage * gearFactor * throttlePenalty) / rpmFactor;
        }
        else if (bike.engineRunning && bike.speed <= 2f)
        {
            // Engine is running but we are stopped (0 km/l)
            instantMileage = 0f;
        }

        // 3. Display it!
        mileageText.text = "Mileage: " + instantMileage.ToString("F1") + " km/l";
    }
}