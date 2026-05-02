using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GamepadScroller : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollSpeed = 5f;

    void Update()
    {
        // Ensures we have a gamepad and the menu is actually active
        if (Gamepad.current != null)
        {
            float scroll = Gamepad.current.rightStick.y.ReadValue();

            // The deadzone prevents microscopic stick drift from making it jitter
            if (Mathf.Abs(scroll) > 0.1f && scrollRect != null)
            {
                // Directly pushes the scrollbar up and down!
                scrollRect.verticalNormalizedPosition += scroll * scrollSpeed * Time.unscaledDeltaTime;
            }
        }
    }
}