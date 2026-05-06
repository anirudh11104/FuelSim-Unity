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
    public GameObject tuningPanel;
    public GameObject pauseMenuPanel;
    public GameObject hudCanvas;

    [Header("Tuning Elements")]
    public GameObject tuningPlayButton;

    [Header("First Selected (For Gamepad)")]
    public GameObject mainPlayButton;
    public GameObject vehicleMotorcycleButton;
    public GameObject settingsFirstOption;
    public GameObject tuningFirstOption;
    public GameObject pauseResumeButton;

    [Header("Vehicles")]
    public GameObject motorcyclePlayer;
    public GameObject carPlayer;

    private bool isPaused = false;
    private bool inMainMenu = true;
    private float lastPauseTime = 0f;

    private GameObject hiddenFocusNode;

    void Awake()
    {
        hiddenFocusNode = new GameObject("Hidden_UI_Focus_Node");
        Canvas canvas = hiddenFocusNode.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        Button dummyButton = hiddenFocusNode.AddComponent<Button>();
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        dummyButton.navigation = nav;

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

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            if (settingsPanel.activeSelf)
            {
                if (inMainMenu) ShowMainMenu();
                else PauseGame();
            }
            else if (tuningPanel.activeSelf)
            {
                BackFromTuning();
            }
            else if (vehicleSelectPanel.activeSelf) ShowMainMenu();
            else if (pauseMenuPanel.activeSelf) ResumeGame();
        }

        if (EventSystem.current.currentSelectedGameObject == hiddenFocusNode || EventSystem.current.currentSelectedGameObject == null)
        {
            if (Gamepad.current != null)
            {
                if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.05f ||
                    Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.05f)
                {
                    if (mainMenuPanel.activeSelf) SetSelected(mainPlayButton);
                    else if (vehicleSelectPanel.activeSelf) SetSelected(vehicleMotorcycleButton);
                    else if (settingsPanel.activeSelf) SetSelected(settingsFirstOption);
                    else if (tuningPanel.activeSelf) SetSelected(tuningFirstOption);
                    else if (pauseMenuPanel.activeSelf) SetSelected(pauseResumeButton);
                }
            }
        }
    }

    // --- NEW SELECTION METHODS ---
    public void SelectMotorcycle()
    {
        if (carPlayer != null) carPlayer.SetActive(false);
        if (motorcyclePlayer != null) motorcyclePlayer.SetActive(true);
        Time.timeScale = 0f; // Force it to stay paused!
        ShowTuning();
    }

    public void SelectCar()
    {
        if (motorcyclePlayer != null) motorcyclePlayer.SetActive(false);
        if (carPlayer != null) carPlayer.SetActive(true);
        Time.timeScale = 0f; // Force it to stay paused!
        ShowTuning();
    }
    // -----------------------------

    public void ShowMainMenu()
    {
        inMainMenu = true;
        isPaused = false;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        InputSystem.PauseHaptics();
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);

        // --- PREVENT 2ND PLAYTHROUGH BUG ---
        if (carPlayer != null) carPlayer.SetActive(false);
        if (motorcyclePlayer != null) motorcyclePlayer.SetActive(false);
        // -----------------------------------

        CloseAllPanels();
        mainMenuPanel.SetActive(true);

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

    public void ShowTuning()
    {
        CloseAllPanels();
        tuningPanel.SetActive(true);

        if (tuningPlayButton != null)
        {
            tuningPlayButton.SetActive(inMainMenu);
        }

        SetSelected(hiddenFocusNode);
    }

    public void BackFromSettings()
    {
        if (inMainMenu) ShowMainMenu();
        else PauseGame();
    }

    public void BackFromTuning()
    {
        if (inMainMenu) ShowVehicleSelect();
        else PauseGame();
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

    private void CloseAllPanels()
    {
        mainMenuPanel.SetActive(false);
        vehicleSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
        tuningPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
    }

    private void SetSelected(GameObject firstButton)
    {
        if (EventSystem.current == null) return;
        if (EventSystem.current.currentSelectedGameObject == firstButton) return;

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