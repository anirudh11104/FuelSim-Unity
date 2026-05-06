using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using UnityEngine.UI;

public class MileagePredictor : MonoBehaviour
{
    private InferenceSession session;
    public float currentMileage;
    public CarEngineSimulator car;
    public BikeEngineSimulator bike;

    [Header("Tuning Panel Sliders")]
    [Tooltip("0.5 (Eco) to 1.5 (Sport/Rich)")]
    public Slider engineMapSlider;

    [Tooltip("0.7 (Stripped/Light) to 1.3 (Armored/Heavy)")]
    public Slider weightSlider;

    [Tooltip("0.8 (Acceleration/Short) to 1.2 (Top Speed/Tall)")]
    public Slider finalDriveSlider;

    public float GetCurrentMileage()
    {
        return currentMileage;
    }

    void Awake()
    {
        string modelPath = Application.streamingAssetsPath + "/mileage_model.onnx";
        Debug.Log("Loading model from: " + modelPath);
        session = new InferenceSession(modelPath);
        Debug.Log("Mileage model loaded successfully");
    }

    void OnEnable()
    {
        StartCoroutine(RealtimePredictionLoop());
    }

    void RunPrediction()
    {
        float speed = 0f; float rpm = 0f; int gear = 0; float throttle = 0f; bool engineRunning = false;
        float powerKw = 0f; float torqueNm = 0f; float powerRpm = 0f; float torqueRpm = 0f; float compression = 0f; float weight = 0f;
        float clutch = 0f; float maxRPM = 8000f;

        // 1. DYNAMIC VEHICLE SPECS
        if (bike != null && bike.gameObject.activeInHierarchy)
        {
            speed = bike.speed; rpm = bike.rpm; gear = bike.currentGear; throttle = bike.throttle; engineRunning = bike.engineRunning;
            clutch = bike.clutch; maxRPM = bike.maxRPM;

            powerKw = 34.6f; torqueNm = 52.3f; powerRpm = 7150f; torqueRpm = 5150f; compression = 9.5f; weight = 212f;
        }
        else if (car != null && car.gameObject.activeInHierarchy)
        {
            speed = car.GetComponent<Rigidbody>().velocity.magnitude * 3.6f; rpm = car.rpm; gear = car.currentGear; throttle = car.throttle; engineRunning = car.engineRunning;
            clutch = 0f; maxRPM = car.maxRPM;

            powerKw = 220f; torqueNm = 407f; powerRpm = 6000f; torqueRpm = 4000f; compression = 8.2f; weight = 1520f;
        }

        if (bike == null && car == null) return;

        // 2. GRAB SLIDER TUNING VALUES (Default to 1f if slider isn't assigned yet)
        float mapMod = (engineMapSlider != null) ? engineMapSlider.value : 1f;
        float weightMod = (weightSlider != null) ? weightSlider.value : 1f;
        float driveMod = (finalDriveSlider != null) ? finalDriveSlider.value : 1f;

        // Apply slider tuning to the physical stats
        powerKw *= mapMod;
        torqueNm *= mapMod;
        weight *= weightMod;

        // 3. FEED THE AI
        float[] inputData = new float[21]
        {
            powerKw, torqueNm, powerRpm, torqueRpm, compression, weight,
            throttle * 100f, rpm, gear, speed,
            0f, 1f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f, 0f
        };

        var tensor = new DenseTensor<float>(inputData, new int[] { 1, 21 });
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", tensor) };
        using var results = session.Run(inputs);
        float basePrediction = results.First().AsEnumerable<float>().First();

        // 4. SANITY CHECK CLAMP
        if (bike != null && bike.gameObject.activeInHierarchy)
        {
            if (basePrediction < 18f || basePrediction > 30f) basePrediction = 23.0f;
        }
        else if (car != null && car.gameObject.activeInHierarchy)
        {
            if (basePrediction > 12f || basePrediction < 5f) basePrediction = 9.0f;
        }

        // Apply Final Drive tuning to baseline (Taller gears = better cruise economy)
        basePrediction *= driveMod;

        // 5. REAL-TIME PHYSICS MODIFIER
        float finalMileage = 0f;

        if (engineRunning && speed > 1f)
        {
            if (throttle < 0.05f && (clutch > 0.5f || gear == 0))
            {
                float coastingBonus = Mathf.Max(2.0f, speed / 10f);
                finalMileage = Mathf.Min(basePrediction * coastingBonus, 99.9f);
            }
            else if (throttle < 0.05f && gear > 0)
            {
                float engineBrakeBonus = Mathf.Max(1.5f, speed / 15f);
                finalMileage = Mathf.Min(basePrediction * engineBrakeBonus, 99.9f);
            }
            else
            {
                float gearClamp = Mathf.Clamp((float)gear, 1f, 5f);
                float gearFactor = Mathf.Lerp(0.9f, 1.5f, gearClamp / 5f);

                // Sport mapping (mapMod > 1) penalizes throttle efficiency further
                float throttlePenalty = Mathf.Lerp(1.1f, 0.85f - ((mapMod - 1f) * 0.2f), throttle);
                float rpmFactor = Mathf.Lerp(0.9f, 1.3f, rpm / maxRPM);

                if ((clutch > 0.5f || gear == 0) && throttle > 0.1f) throttlePenalty *= 0.7f;

                finalMileage = (basePrediction * gearFactor * throttlePenalty) / rpmFactor;
                finalMileage = Mathf.Clamp(finalMileage, 4f, 60f);
            }
        }
        else if (engineRunning && speed <= 1f)
        {
            finalMileage = 0f;
        }

        currentMileage = finalMileage;
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
        if (session != null) session.Dispose();
    }

    public float PredictMileage(float engineCC, float weight)
    {
        return 0f;
    }
}