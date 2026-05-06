using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TuningUIManager : MonoBehaviour
{
    public Slider engineMapSlider;
    public Slider weightSlider;
    public Slider finalDriveSlider;

    public TextMeshProUGUI engineMapText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI finalDriveText;

    public GameObject car;
    public GameObject bike;

    // --- BASE PHYSICS STORAGE ---
    // We store the "Stock" values here so we don't permanently break the inspector
    private float baseBikeMass;
    private float baseBikeTorque;

    private float baseCarMass = 1520f;
    private float baseCarTorque;
    private float baseCarFinalDrive;

    void Start()
    {
        // Grab the actual inspector values on frame 1 to use as our "Stock" baseline
        if (bike != null)
        {
            var bSim = bike.GetComponent<BikeEngineSimulator>();
            if (bSim != null)
            {
                baseBikeMass = bSim.bikeMass;
                baseBikeTorque = bSim.engineMaxTorque;
            }
        }
        if (car != null)
        {
            var cSim = car.GetComponent<CarEngineSimulator>();
            Rigidbody rb = car.GetComponent<Rigidbody>();

            if (cSim != null)
            {
                baseCarTorque = cSim.engineMaxTorque;
                baseCarFinalDrive = cSim.finalDriveRatio;
            }
            if (rb != null)
            {
                baseCarMass = rb.mass;
            }
        }
    }

    void Update()
    {
        UpdateUI();
        ApplyTuningToPhysics(); // THE BRIDGE!
    }

    void UpdateUI()
    {
        if (engineMapSlider != null && engineMapText != null)
        {
            float mapVal = Mathf.Round(engineMapSlider.value * 100f);
            engineMapText.text = "Engine Map: " + mapVal.ToString("0") + "% Power";
        }

        if (weightSlider != null && weightText != null)
        {
            float weightMod = weightSlider.value;
            float actualWeight = 0f;

            if (bike != null && bike.activeInHierarchy)
            {
                actualWeight = 212f * weightMod; // Visual UI base
            }
            else if (car != null && car.activeInHierarchy)
            {
                actualWeight = 1520f * weightMod; // Visual UI base
            }

            weightText.text = "Weight: " + Mathf.Round(actualWeight).ToString("0") + " kg";
        }

        if (finalDriveSlider != null && finalDriveText != null)
        {
            float driveVal = finalDriveSlider.value;

            if (driveVal < 0.95f)
                finalDriveText.text = "Final Drive: Short (Accel)";
            else if (driveVal > 1.05f)
                finalDriveText.text = "Final Drive: Tall (Top Speed)";
            else
                finalDriveText.text = "Final Drive: Stock";
        }
    }

    void ApplyTuningToPhysics()
    {
        // Grab the slider multipliers (default to 1 if sliders are missing)
        float mapMod = (engineMapSlider != null) ? engineMapSlider.value : 1f;
        float weightMod = (weightSlider != null) ? weightSlider.value : 1f;
        float driveMod = (finalDriveSlider != null) ? finalDriveSlider.value : 1f;

        // Apply them dynamically to the active vehicle
        if (bike != null && bike.activeInHierarchy)
        {
            var bSim = bike.GetComponent<BikeEngineSimulator>();
            Rigidbody rb = bike.GetComponent<Rigidbody>(); // 1. ADD THIS

            if (bSim != null)
            {
                bSim.bikeMass = baseBikeMass * weightMod;
                bSim.engineMaxTorque = baseBikeTorque * mapMod;
                bSim.finalDriveRatio = 1f / driveMod;
            }
            if (rb != null) // 2. ADD THIS
            {
                rb.mass = 212f * weightMod; // 3. ADD THIS
            }
        }
        else if (car != null && car.activeInHierarchy)
        {
            var cSim = car.GetComponent<CarEngineSimulator>();
            Rigidbody rb = car.GetComponent<Rigidbody>();

            if (cSim != null)
            {
                cSim.engineMaxTorque = baseCarTorque * mapMod;

                // For gearing: Taller gears = lower numerical ratio. 
                // So dividing by a slider > 1 creates a taller gear mathematically.
                cSim.finalDriveRatio = baseCarFinalDrive / driveMod;
            }
            if (rb != null)
            {
                rb.mass = baseCarMass * weightMod;
            }
        }
    }

    public void ResetToDefault()
    {
        if (engineMapSlider != null) engineMapSlider.value = 1f;
        if (weightSlider != null) weightSlider.value = 1f;
        if (finalDriveSlider != null) finalDriveSlider.value = 1f;
    }
}