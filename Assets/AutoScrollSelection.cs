using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for Left Stick/D-pad tracking

public class AutoScrollToSelection : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect scrollRect;
    public RectTransform viewport;
    public RectTransform content;

    private GameObject lastSelected;

    void Update()
    {
        // 1. Find out what button your Left Stick/D-pad just highlighted
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // 2. If it is a new item, and it actually lives inside this dropdown's content box...
        if (selected != null && selected != lastSelected && selected.transform.IsChildOf(content))
        {
            lastSelected = selected;
            SnapToTarget(selected.GetComponent<RectTransform>());
        }
    }

    void SnapToTarget(RectTransform target)
    {
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        // If the list is so short it doesn't need scrolling, ignore it
        if (contentHeight <= viewportHeight) return;

        // Because we set the Content Pivot to Top (1) earlier, Y positions are negative. 
        // We use Mathf.Abs to turn them positive so the math is easy.
        float itemTop = Mathf.Abs(target.anchoredPosition.y);
        float itemBottom = itemTop + target.rect.height;

        // Figure out exactly what part of the list the window is currently looking at
        float visibleTop = (1f - scrollRect.verticalNormalizedPosition) * (contentHeight - viewportHeight);
        float visibleBottom = visibleTop + viewportHeight;

        // If the neon box just moved BELOW the screen edge, snap the scrollbar down
        if (itemBottom > visibleBottom)
        {
            float newScrollTop = itemBottom - viewportHeight;
            scrollRect.verticalNormalizedPosition = 1f - (newScrollTop / (contentHeight - viewportHeight));
        }
        // If the neon box just moved ABOVE the screen edge, snap the scrollbar up
        else if (itemTop < visibleTop)
        {
            scrollRect.verticalNormalizedPosition = 1f - (itemTop / (contentHeight - viewportHeight));
        }
    }
}