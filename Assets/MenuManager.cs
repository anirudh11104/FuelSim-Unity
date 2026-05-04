using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

    // The invisible node that keeps the Input System awake
    private GameObject hiddenFocusNode;

    void Awake()
    {
        // Creates an invisible, unclickable Canvas in the background
        hiddenFocusNode = new GameObject("Hidden_UI_Focus_Node");
        Canvas canvas = hiddenFocusNode.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Adds a button, but completely disables its ability to navigate
        Button dummyButton = hiddenFocusNode.AddComponent<Button>();
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        dummyButton.navigation = nav;

        // Makes it 100% invisible
        CanvasGroup cg = hiddenFocusNode.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
    }

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

        // --- THE INSTANT WAKE-UP LOGIC ---
        // If the invisible node is selected, wait for the absolute tiniest stick movement
        if (EventSystem.current.currentSelectedGameObject == hiddenFocusNode || EventSystem.current.currentSelectedGameObject == null)
        {
            if (Gamepad.current != null)
            {
                // sqrMagnitude of 0.05 ignores stick drift but instantly catches a real thumb push
                if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.05f ||
                    Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.05f)
                {
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
        isPaused = false;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        InputSystem.PauseHaptics();
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);

        CloseAllPanels();
        mainMenuPanel.SetActive(true);

        // THE FIX: Select the invisible node instead of null
        SetSelected(hiddenFocusNode);
        if (hudCanvas != null) hudCanvas.SetActive(false);
    }

    public void ShowVehicleSelect()
    {
        CloseAllPanels();
        vehicleSelectPanel.SetActive(true);
        SetSelected(hiddenFocusNode);
    }

    public void ShowSettings()
    {
        CloseAllPanels();
        settingsPanel.SetActive(true);
        SetSelected(hiddenFocusNode);
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
        isPaused = false;
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

        SetSelected(hiddenFocusNode);
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

        Selectable selectable = firstButton.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.Select();
        }
    }

    void OnApplicationQuit()
    {
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
    }
}