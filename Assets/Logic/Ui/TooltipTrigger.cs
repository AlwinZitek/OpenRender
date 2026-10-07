using UnityEngine;
using UnityEngine.EventSystems;

// Auf jeden Toolbar-Button ziehen und den Text eintragen.
[RequireComponent(typeof(RectTransform))]
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    public string tooltipText = "";

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Show(tooltipText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }

    // Falls der Button waehrend des Hoverns deaktiviert oder zerstoert wird,
    // bleibt der Tooltip sonst haengen.
    void OnDisable()
    {
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Hide();
    }
}
