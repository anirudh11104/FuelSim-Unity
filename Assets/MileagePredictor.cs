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
            // RPM: Maps 0-7000 RPM to a soft modifier (0.8 = good, 1.5 = heavy fuel use)
            float rpmFactor = Mathf.Lerp(0.8f, 1.5f, bike.rpm / 7000f);

            // Gear: 1st gear = 0.7x multiplier (worse), 5th gear = 1.2x multiplier (better)
            float gearFactor = Mathf.Lerp(0.7f, 1.2f, bike.currentGear / 5f);

            // Throttle: Coasting (0 throttle) = 1.1x bonus, Full throttle = 0.8x penalty
            float throttlePenalty = Mathf.Lerp(1.1f, 0.8f, bike.throttle);

            // Apply our softer modifiers to the AI's base prediction
            finalMileage = (basePrediction * gearFactor * throttlePenalty) / rpmFactor;
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