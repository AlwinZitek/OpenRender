using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Zeigt einen kleinen Text-Tooltip neben dem Mauszeiger.
// Wird von TooltipTrigger auf den einzelnen Buttons angesteuert.
public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [Header("Referenzen")]
    public RectTransform panel;      // das Tooltip-Panel (Image + Layout)
    public TextMeshProUGUI label;    // Text im Panel

    [Header("Verhalten")]
    [Tooltip("Verzoegerung in Sekunden, bevor der Tooltip erscheint")]
    public float showDelay = 0.4f;

    [Tooltip("Abstand zum Mauszeiger in Pixeln (x = rechts, y = runter)")]
    public Vector2 offset = new Vector2(14f, -14f);

    [Tooltip("Maximale Breite des Tooltips in Pixeln, bevor der Text umbricht")]
    public float maxWidth = 240f;

    private Canvas parentCanvas;
    private RectTransform parentRect;
    private float hoverTimer = -1f;
    private string pendingText;

    void Awake()
    {
        Instance = this;
        parentCanvas = GetComponentInParent<Canvas>();

        // WICHTIG: anchoredPosition ist relativ zum DIREKTEN Elternobjekt des
        // Panels, nicht zum Canvas. Frueher wurde gegen den Canvas gerechnet -
        // dadurch entstand ein konstanter Versatz, sobald dieses GameObject
        // nicht exakt mittig und canvasgross war.
        if (panel != null)
            parentRect = panel.parent as RectTransform;

        if (panel != null)
        {
            // Pivot oben-links: der Tooltip haengt mit seiner oberen linken
            // Ecke am Cursor und waechst nach rechts/unten.
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0f, 1f);
            panel.gameObject.SetActive(false);
        }

        if (label != null)
            label.enableWordWrapping = true;
    }

    void Update()
    {
        if (hoverTimer < 0f) return;

        hoverTimer += Time.unscaledDeltaTime;

        if (hoverTimer >= showDelay && !panel.gameObject.activeSelf)
        {
            label.text = pendingText;
            panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        if (panel.gameObject.activeSelf)
            FollowMouse();
    }

    void FollowMouse()
    {
        if (parentRect == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();

        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPos, cam, out localPoint))
            return;

        Vector2 size = panel.rect.size;

        // Bildschirmraender in Koordinaten des Elternobjekts bestimmen
        Vector2 minCorner, maxCorner;
        GetScreenBoundsInParentSpace(cam, out minCorner, out maxCorner);

        // Standard: rechts unterhalb des Cursors (Pivot oben-links)
        Vector2 pos = localPoint + offset;

        // Am rechten Rand nach links umklappen
        if (pos.x + size.x > maxCorner.x)
            pos.x = localPoint.x - offset.x - size.x;

        // Am unteren Rand nach oben umklappen
        if (pos.y - size.y < minCorner.y)
            pos.y = localPoint.y - offset.y + size.y;

        // Sicherheitsnetz: komplett im sichtbaren Bereich halten
        pos.x = Mathf.Clamp(pos.x, minCorner.x, maxCorner.x - size.x);
        pos.y = Mathf.Clamp(pos.y, minCorner.y + size.y, maxCorner.y);

        panel.anchoredPosition = pos;
    }

    // Die Bildschirmecken in die lokalen Koordinaten des Elternobjekts
    // umrechnen, damit das Umklappen am Rand unabhaengig davon funktioniert,
    // wo und wie gross das Elternobjekt im Canvas sitzt.
    void GetScreenBoundsInParentSpace(Camera cam, out Vector2 min, out Vector2 max)
    {
        Vector2 bottomLeft, topRight;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, Vector2.zero, cam, out bottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, new Vector2(Screen.width, Screen.height), cam, out topRight);

        min = Vector2.Min(bottomLeft, topRight);
        max = Vector2.Max(bottomLeft, topRight);
    }

    public void Show(string text)
    {
        if (panel == null || label == null) return;
        if (string.IsNullOrEmpty(text)) return;

        pendingText = text;
        hoverTimer = 0f;
    }

    public void Hide()
    {
        hoverTimer = -1f;
        if (panel != null) panel.gameObject.SetActive(false);
    }
}
