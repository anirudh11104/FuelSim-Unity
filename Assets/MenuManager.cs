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
    public GameObject hudCanvas;

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
            if (Time.unscaledTime - lastPauseTime > 0.25f)
            {
                lastPauseTime = Time.unscaledTime;

                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }

        // --- BACK LOGIC ('B' Button) ---
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            if (settingsPanel.activeSelf)
            {
                if (inMainMenu) ShowMainMenu();
                else PauseGame();
            }
            else if (vehicleSelectPanel.activeSelf) ShowMainMenu();
            else if (pauseMenuPanel.activeSelf) ResumeGame();
        }

        // --- INVISIBLE UI WAKE-UP LOGIC ---
        // Keeps the box hidden until you move the stick, but instantly catches the very first movement!
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            bool wakeUp = false;

            if (Gamepad.current != null)
            {
                // The threshold is now 0.1f. The tiniest flick will wake it up on the first try.
                if (Gamepad.current.leftStick.ReadValue().magnitude > 0.1f ||
                    Gamepad.current.dpad.ReadValue().magnitude > 0.1f)
                {
                    wakeUp = true;
                }
            }

            if (wakeUp)
            {
                if (mainMenuPanel.activeSelf) SetSelected(mainPlayButton);
                else if (vehicleSelectPanel.activeSelf) SetSelected(vehicleMotorcycleButton);
                else if (settingsPanel.activeSelf) SetSelected(settingsFirstOption);
                else if (pauseMenuPanel.activeSelf) SetSelected(pauseResumeButton);
            }
        }
    }

    // --- PANEL NAVIGATION ---

    public void ShowMainMenu()
    {
        inMainMenu = true;
        isPaused = false; // Reset pause state just to be safe
        Time.timeScale = 0f;
        AudioListener.pause = true;

        InputSystem.PauseHaptics();
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);

        CloseAllPanels();
        mainMenuPanel.SetActive(true);

        // Hide the box initially
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

    public void BackFromSettings()
    {
        if (inMainMenu) ShowMainMenu();
        else PauseGame();
    }

    // --- GAMEPLAY STATE ---

    public void PlayAsCar()
    {
        motorcyclePlayer.SetActive(false);
        carPlayer.SetActive(true);
        ActiveVehicle.Current = carPlayer;
        StartGame();
    }

    public void PlayAsMotorcycle()
    {
        carPlayer.SetActive(false);
        motorcyclePlayer.SetActive(true);
        ActiveVehicle.Current = motorcyclePlayer;
        StartGame();
    }

    public void StartGame()
    {
        inMainMenu = false;
        isPaused = false; // THE FIX: Forgets the old pause state when you start driving!
        StartCoroutine(EnableGameplayAfterDelay());
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        InputSystem.PauseHaptics();
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);

        CloseAllPanels();
        pauseMenuPanel.SetActive(true);

        // Hide the box initially
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
        CloseAllPanels();
        if (hudCanvas != null) hudCanvas.SetActive(true);

        yield return new WaitForSecondsRealtime(0.15f);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        InputSystem.ResumeHaptics();
    }

    public void QuitToDesktop()
    {
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
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    void OnApplicationQuit()
    {
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}