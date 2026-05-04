using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject vehicleSelectPanel;
    public GameObject settingsPanel;
    public GameObject pauseMenuPanel;
    public GameObject hudCanvas; // Your dashboard canvas (optional)

    [Header("First Selected (For Gamepad)")]
    public GameObject mainPlayButton;
    public GameObject vehicleMotorcycleButton;
    public GameObject settingsFirstOption;
    public GameObject pauseResumeButton;

    [Header("Vehicles")]
    public GameObject motorcyclePlayer;
    public GameObject carPlayer;

    private bool isPaused = false;
    private bool inMainMenu = true;
    private float lastPauseTime = 0f;

    void Start()
    {
        // When the game boots up, freeze time and show the Main Menu
        ShowMainMenu();
    }

    void Update()
    {
        // --- PAUSE LOGIC (Start Button) ---
        bool pauseInput = false;
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) pauseInput = true;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) pauseInput = true;

        if (pauseInput && !inMainMenu)
        {
            // THE FIX: Only allow a pause toggle if 0.25 real-time seconds have passed
            if (Time.unscaledTime - lastPauseTime > 0.25f)
            {
                lastPauseTime = Time.unscaledTime; // Record the exact time we pressed it

                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }

        // --- BACK LOGIC ('B' Button) ---
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            // If we are in settings, go back to where we came from
            if (settingsPanel.activeSelf)
            {
                if (inMainMenu) ShowMainMenu();
                else PauseGame();
            }
            // If in vehicle select, go back to main menu
            else if (vehicleSelectPanel.activeSelf) ShowMainMenu();
            // If in pause menu, just resume the game
            else if (pauseMenuPanel.activeSelf) ResumeGame();
        }

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (Gamepad.current != null)
            {
                // Check if you bump the Left Stick, Right Stick, or D-pad
                if (Gamepad.current.leftStick.ReadValue().magnitude > 0.5f ||
                    Gamepad.current.rightStick.ReadValue().magnitude > 0.5f ||
                    Gamepad.current.dpad.ReadValue().magnitude > 0.5f)
                {
                    // Snap the neon box to the correct active menu
                    if (mainMenuPanel.activeSelf) SetSelected(mainPlayButton);
                    else if (vehicleSelectPanel.activeSelf) SetSelected(vehicleMotorcycleButton);
                    else if (settingsPanel.activeSelf) SetSelected(settingsFirstOption);
                    else if (pauseMenuPanel.activeSelf) SetSelected(pauseResumeButton);
                }
            }
        }
    }

    // --- PANEL NAVIGATION ---

    public void ShowMainMenu()
    {
        inMainMenu = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Globally cuts power to the controller's vibration motors
        InputSystem.PauseHaptics();
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);

        CloseAllPanels();
        mainMenuPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
        if (hudCanvas != null) hudCanvas.SetActive(false);
    }

    public void ShowVehicleSelect()
    {
        CloseAllPanels();
        vehicleSelectPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ShowSettings()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);
    }

    // --- NEW: Smart UI Back Button Logic ---
    public void BackFromSettings()
    {
        // Check our state to see where we came from
        if (inMainMenu)
        {
            ShowMainMenu();
        }
        else
        {
            PauseGame(); // This brings the pause menu back up!
        }
    }

    // --- GAMEPLAY STATE ---

    public void PlayAsCar()
    {
        motorcyclePlayer.SetActive(false);
        carPlayer.SetActive(true);

        ActiveVehicle.Current = carPlayer; // 👈 ADD THIS LINE

        StartGame();
    }

    public void PlayAsMotorcycle()
    {
        carPlayer.SetActive(false);
        motorcyclePlayer.SetActive(true);

        ActiveVehicle.Current = motorcyclePlayer; // 👈 ADD THIS LINE

        StartGame();
    }

    public void StartGame()
    {
        inMainMenu = false;
        StartCoroutine(EnableGameplayAfterDelay());
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Globally cuts power to the controller's vibration motors
        InputSystem.PauseHaptics();
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);

        CloseAllPanels();
        pauseMenuPanel.SetActive(true);
        EventSystem.current.SetSelectedGameObject(null);

        if (hudCanvas != null) hudCanvas.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        StartCoroutine(EnableGameplayAfterDelay());
    }

    private IEnumerator EnableGameplayAfterDelay()
    {
        // 1. Hide the menus immediately so the UI feels snappy and responsive
        CloseAllPanels();
        if (hudCanvas != null) hudCanvas.SetActive(true);

        // 2. Force the game to wait 0.15 seconds in real-time. 
        // This gives the player time to lift their thumb off the 'A' button!
        yield return new WaitForSecondsRealtime(0.15f);

        // 3. Now that the button is released, it is safe to unfreeze physics and turn the audio on
        Time.timeScale = 1f;
        AudioListener.pause = false;
        InputSystem.ResumeHaptics();
    }

    public void QuitToDesktop()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }

    // --- UTILS ---

    private void CloseAllPanels()
    {
        mainMenuPanel.SetActive(false);
        vehicleSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
    }

    private void SetSelected(GameObject firstButton)
    {
        // Clear the current memory, then forcefully select the new button so the joystick works
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    void OnApplicationQuit()
    {
        // Failsafe: Shut off motors when you exit Play Mode or quit the game
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }
}