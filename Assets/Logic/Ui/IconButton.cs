using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class IconButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public TextMeshProUGUI iconLabel;

    [Header("Löschen (nur importierte Bilder)")]
    [Tooltip("Kleiner ×-Button, erscheint nach längerem Hovern. Optional.")]
    public GameObject deleteButton;
    [Tooltip("Sekunden, bis der ×-Button eingeblendet wird")]
    public float longHoverDelay = 0.8f;

    private IconData data;
    private bool isHovered = false;
    private float hoverTime = 0f;

    public void Setup(IconData iconData)
    {
        data = iconData;

        iconImage.sprite = data.sprite;
        iconImage.preserveAspect = true;
        iconLabel.text = data.iconName;

        // Tooltip mit vollem Namen und Tags, da das Label auf der Karte
        // schnell abgeschnitten wird.
        var trigger = GetComponent<TooltipTrigger>();
        if (trigger == null) trigger = gameObject.AddComponent<TooltipTrigger>();

        string tip = data.iconName;
        if (data.tags != null && data.tags.Length > 0)
            tip += "\n" + string.Join(", ", data.tags);
        if (data.IsCustom)
            tip += "\n(Entf zum Löschen)";
        trigger.tooltipText = tip;

        GetComponent<Button>().onClick.AddListener(() =>
        {
            IconSpawner.Instance.SpawnIcon(data);
        });

        if (deleteButton != null)
        {
            deleteButton.SetActive(false);

            var btn = deleteButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(DeleteThis);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        hoverTime = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        hoverTime = 0f;
        if (deleteButton != null) deleteButton.SetActive(false);
    }

    void Update()
    {
        if (!isHovered) return;

        // Nur eigene Bilder lassen sich loeschen - eingebaute Icons sind
        // Assets im Resources-Ordner und zur Laufzeit nicht entfernbar.
        if (data == null || !data.IsCustom) return;

        hoverTime += Time.unscaledDeltaTime;

        // Nach laengerem Hovern den ×-Button einblenden
        if (deleteButton != null && !deleteButton.activeSelf && hoverTime >= longHoverDelay)
            deleteButton.SetActive(true);

        // Entf-Taste waehrend des Hoverns
        if (Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame)
            DeleteThis();
    }

    void DeleteThis()
    {
        if (data == null || !data.IsCustom) return;
        if (SidebarManager.Instance == null) return;

        SidebarManager.Instance.DeleteCustomIcon(data);
        // Nicht weiterlaufen: dieses GameObject wird beim Refresh zerstoert.
    }

    void OnDisable()
    {
        isHovered = false;
        hoverTime = 0f;
    }
}
