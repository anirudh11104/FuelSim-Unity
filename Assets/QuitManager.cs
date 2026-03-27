using UnityEngine;

public class QuitManager : MonoBehaviour
{
    void Update()
    {
        // KeyCode.JoystickButton7 is usually the 'Start' button on an Xbox/generic gamepad
        // KeyCode.Escape acts as a backup if you ever do have a keyboard plugged in
        if (Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    public void QuitGame()
    {
        // This will physically close the .exe when playing the built game
        Application.Quit();

        // This just prints a message so you know it works when testing inside the Unity Editor
        Debug.Log("Game Quit Triggered!");
    }
}