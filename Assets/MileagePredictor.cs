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

        // 1. Get the AI's flat baseline prediction (It was km/l all along!)
        float basePrediction = results.First().AsEnumerable<float>().First();

        // Let's ensure the baseline sits around 17-18 km/l from the AI
        if (basePrediction < 1f) basePrediction = 17.5f;

        // 2. --- REAL-TIME PHYSICS MODIFIER (CLASSIC 350 TUNE) ---
        float finalMileage = 0f;

        // Only calculate mileage if the engine is actually running and moving
        if (bike.engineRunning && bike.speed > 1f)
        {
            // TRUE COASTING: Throttle closed, clutch pulled OR IN NEUTRAL.
            if (bike.throttle < 0.05f && (bike.clutch > 0.5f || bike.currentGear == 0))
            {
                float coastingBonus = Mathf.Max(2.0f, bike.speed / 10f);
                finalMileage = basePrediction * coastingBonus;
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
                // NORMAL DRIVING: The "Goldilocks" Hybrid Tune
                // We take the AI's solid 17.6 baseline and apply gentle live physics

                float gearClamp = Mathf.Clamp((float)bike.currentGear, 1f, 5f);

                // GEAR: 1st gear = 0.9x multiplier, 5th gear = 1.5x multiplier (Cruising bonus)
                float gearFactor = Mathf.Lerp(0.9f, 1.5f, gearClamp / 5f);

                // THROTTLE: Gentle throttle = 1.1x bonus, Pinned throttle = 0.85x penalty
                float throttleFactor = Mathf.Lerp(1.1f, 0.85f, bike.throttle);

                // RPM: Low RPM = 0.9 divisor (Bonus), Redline = 1.3 divisor (Penalty)
                float rpmFactor = Mathf.Lerp(0.9f, 1.3f, bike.rpm / bike.maxRPM);

                if ((bike.clutch > 0.5f || bike.currentGear == 0) && bike.throttle > 0.1f)
                {
                    // Revving in neutral penalty
                    throttleFactor *= 0.7f;
                }

                // Apply the gentle modifiers to the AI's base prediction
                finalMileage = (basePrediction * gearFactor * throttleFactor) / rpmFactor;

                // Keep it within a realistic window so it never drops to 2 km/l again!
                finalMileage = Mathf.Clamp(finalMileage, 8f, 50f);
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