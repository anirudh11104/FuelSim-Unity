using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // REQUIRED for the Volume Slider!

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown resolutionDropdown;
    public Slider volumeSlider; // Our new slider reference

    private Resolution[] filteredResolutions;

    void Start()
    {
        // --- VOLUME SETUP ---
        // Force the slider to max value (1.0) by default
        if (volumeSlider != null)
        {
            volumeSlider.value = 1f;
            AudioListener.volume = 1f;
        }

        // --- RESOLUTION SETUP ---
        Resolution[] allResolutions = Screen.resolutions;
        List<Resolution> uniqueResolutions = new List<Resolution>();

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        Resolution currentMonitorRes = Screen.currentResolution;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            if (allResolutions[i].width <= currentMonitorRes.width &&
                allResolutions[i].height <= currentMonitorRes.height)
            {
                bool isDuplicate = false;
                foreach (var res in uniqueResolutions)
                {
                    if (res.width == allResolutions[i].width && res.height == allResolutions[i].height)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    uniqueResolutions.Add(allResolutions[i]);
                    string option = allResolutions[i].width + " x " + allResolutions[i].height;
                    options.Add(option);
                }
            }
        }

        filteredResolutions = uniqueResolutions.ToArray();
        resolutionDropdown.AddOptions(options);

        // Force the dropdown to select the maximum resolution by default
        resolutionDropdown.value = uniqueResolutions.Count - 1;
        resolutionDropdown.RefreshShownValue();
    }

    // --- NEW VOLUME LISTENER ---
    // This function automatically receives the slider's current number whenever you move it
    public void SetGlobalVolume(float sliderValue)
    {
        AudioListener.volume = sliderValue;
    }

    public void ApplyResolution(int resolutionIndex)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, true);
    }
}