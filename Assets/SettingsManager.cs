using System.Collections.Generic;
using UnityEngine;
using TMPro; // Crucial for TextMeshPro UI

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown resolutionDropdown;

    private Resolution[] filteredResolutions;

    void Start()
    {
        // 1. Get every resolution supported by the player's physical monitor
        Resolution[] allResolutions = Screen.resolutions;
        List<Resolution> uniqueResolutions = new List<Resolution>();

        // 2. Wipe the dummy options we added in the editor earlier
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        int currentResolutionIndex = 0;
        Resolution currentMonitorRes = Screen.currentResolution;

        // 3. Filter out duplicates and ensure we never exceed the monitor's native limits
        for (int i = 0; i < allResolutions.Length; i++)
        {
            if (allResolutions[i].width <= currentMonitorRes.width &&
                allResolutions[i].height <= currentMonitorRes.height)
            {
                // Unity sometimes returns the exact same resolution 5 times for different refresh rates (59hz, 60hz, 144hz). 
                // This checks to make sure we only list each dimension once to keep your UI clean.
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

                    // Format the text perfectly for your hacker aesthetic
                    string option = allResolutions[i].width + " x " + allResolutions[i].height;
                    options.Add(option);

                    // If the loop finds the resolution the game is CURRENTLY running at, save that spot
                    if (allResolutions[i].width == Screen.width &&
                        allResolutions[i].height == Screen.height)
                    {
                        currentResolutionIndex = uniqueResolutions.Count - 1;
                    }
                }
            }
        }

        filteredResolutions = uniqueResolutions.ToArray();

        // 4. Inject the dynamically generated list right into your neon UI
        resolutionDropdown.AddOptions(options);

        // Force the dropdown to select the very last item in the list (the absolute maximum resolution)
        resolutionDropdown.value = uniqueResolutions.Count - 1;
        resolutionDropdown.RefreshShownValue();
    }

    // 5. The function the UI will trigger when you press 'A' on a new resolution
    public void ApplyResolution(int resolutionIndex)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];

        // true = Fullscreen, false = Windowed. You can change this later if you add a Windowed Mode toggle!
        Screen.SetResolution(resolution.width, resolution.height, true);
    }
}