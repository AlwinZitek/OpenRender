using UnityEngine;
using UnityEngine.EventSystems;

// Meldet dem SidebarManager, ueber welchem Tag-Button die Maus gerade steht,
// damit dort die Entf-Taste den richtigen Tag loeschen kann.
public class TagButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tag;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SidebarManager.Instance != null)
            SidebarManager.Instance.SetHoveredTag(tag);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SidebarManager.Instance != null)
            SidebarManager.Instance.ClearHoveredTag(tag);
    }

    void OnDisable()
    {
        if (SidebarManager.Instance != null)
            SidebarManager.Instance.ClearHoveredTag(tag);
    }
}
