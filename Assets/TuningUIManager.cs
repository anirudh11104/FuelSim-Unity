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

    void Update()
    {
        UpdateUI();
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
                actualWeight = 212f * weightMod;
            }
            else if (car != null && car.activeInHierarchy)
            {
                actualWeight = 1520f * weightMod;
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

    public void ResetToDefault()
    {
        if (engineMapSlider != null) engineMapSlider.value = 1f;
        if (weightSlider != null) weightSlider.value = 1f;
        if (finalDriveSlider != null) finalDriveSlider.value = 1f;
    }
}