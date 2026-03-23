using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using UnityEngine.UI;
using TMPro;

public class MileagePredictor : MonoBehaviour
{
    private InferenceSession session;

    public BikeEngineSimulator bike;
    public TextMeshProUGUI mileageText;

    void Awake()
    {
        string modelPath = Application.streamingAssetsPath + "/mileage_model.onnx";
        Debug.Log("Loading model from: " + modelPath);
        session = new InferenceSession(modelPath);

        Debug.Log("Mileage model loaded successfully");
        StartCoroutine(RealtimePredictionLoop());
    }

    void RunPrediction()
    {
        // Safety check just in case
        if (bike == null || mileageText == null) return;

        float[] inputData = new float[21]
        {
        24.8f,                     // Power_kW
        35f,                       // Torque_Nm
        7500f,                     // Power_Benchmark_RPM
        6000f,                     // Torque_Benchmark_RPM
        11.5f,                     // Compression_Enumerator
        212f,                      // Weight_kg

        bike.throttle * 100f,      // Throttle_pct
        bike.rpm,                  // RPM
        bike.currentGear,          // Gear
        bike.speed,                // Speed_kmh

        // ---- one-hot encoding (adjust per bike) ----
        0f, 1f, 0f,                // Fuel system
        0f, 1f, 0f,                // Engine type
        0f, 0f, 1f, 0f, 0f         // Gearbox
        };

        var tensor = new DenseTensor<float>(inputData, new int[] { 1, 21 });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", tensor)
        };

        using var results = session.Run(inputs);

        // 1. Get the AI's flat baseline prediction
        float basePrediction = results.First().AsEnumerable<float>().First();

        // 2. --- REAL-TIME PHYSICS MODIFIER (TUNED) ---
        float finalMileage = 0f;

        // Only calculate mileage if the engine is actually running and moving
        if (bike.engineRunning && bike.speed > 1f)
        {
            // TRUE COASTING: Throttle closed, clutch pulled OR IN NEUTRAL.
            // You are burning minimal fuel (idle) while covering ground.
            if (bike.throttle < 0.05f && (bike.clutch > 0.5f || bike.currentGear == 0))
            {
                float coastingBonus = Mathf.Max(2.0f, bike.speed / 10f); // Scales up with speed
                finalMileage = basePrediction * coastingBonus;
                
                // Clamp it so it doesn't show an absurd number like 500 km/l
                finalMileage = Mathf.Min(finalMileage, 99.9f); 
            }
            // ENGINE BRAKING: Throttle closed, clutch engaged, IN GEAR.
            else if (bike.throttle < 0.05f && bike.currentGear > 0)
            {
                float engineBrakeBonus = Mathf.Max(1.5f, bike.speed / 15f);
                finalMileage = basePrediction * engineBrakeBonus;
                finalMileage = Mathf.Min(finalMileage, 99.9f);
            }
            else
            {
                // NORMAL DRIVING OR REVVING IN NEUTRAL/CLUTCH
                // Prevent divide-by-zero or weird gear factors if revving in Neutral
                float gearClamp = Mathf.Max(1f, (float)bike.currentGear); 
                float gearFactor = Mathf.Lerp(0.7f, 1.2f, gearClamp / 5f);
                
                // Throttle: More throttle = worse mileage
                float throttleFactor = Mathf.Lerp(1.1f, 0.6f, bike.throttle);

                // RPM: Higher RPM = worse mileage. Base is 1.0 so we heavily penalize high revs
                float rpmFactor = Mathf.Lerp(0.9f, 2.0f, bike.rpm / 7000f);

                // If you rev the engine while the clutch is pulled OR in Neutral, penalize heavily
                if ((bike.clutch > 0.5f || bike.currentGear == 0) && bike.throttle > 0.1f)
                {
                    throttleFactor *= 0.5f; 
                }

                // Apply modifiers
                finalMileage = (basePrediction * gearFactor * throttleFactor) / rpmFactor;
            }
        }
        else if (bike.engineRunning && bike.speed <= 1f)
        {
            // Engine is idling but we are stopped
            finalMileage = 0f;
        }

        // 3. Display it!
        mileageText.text = "Mileage: " + finalMileage.ToString("F1") + " km/l";
    }

    IEnumerator RealtimePredictionLoop()
    {
        while (true)
        {
            RunPrediction();
            // Updates twice a second for a snappier dashboard feel
            yield return new WaitForSeconds(0.5f);
        }
    }

    void OnDestroy()
    {
        if (session != null)
            session.Dispose();
    }

    public float PredictMileage(float engineCC, float weight)
    {
        return 0f;
    }
}