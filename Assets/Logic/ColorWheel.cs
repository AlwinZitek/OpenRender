using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// HSV Color Wheel: Winkel = Hue, Radius = Saturation, Slider = Value.
// Kommt auf ein quadratisches RawImage im UI-Canvas.
[RequireComponent(typeof(RawImage))]
public class ColorWheel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Referenzen (im Inspector reinziehen)")]
    public RectTransform cursor;   // kleiner Kreis, Child vom Wheel, Anchor mittig, Raycast Target AUS
    public Slider valueSlider;     // Helligkeit 0..1
    public Image preview;          // optional: Farbfeld mit der exakten aktuellen Farbe

    [Header("Events")]
    public UnityEvent onEditStarted;            // Maus gedrückt → alte Farben merken
    public UnityEvent<Color> onColorChanged;    // jeden Frame beim Ziehen → Live-Vorschau
    public UnityEvent<Color> onColorCommitted;  // Maus losgelassen → EIN Undo-Command

    private const int TextureSize = 256;

    private RawImage wheelImage;
    private RectTransform rect;
    private bool isDragging = false;
    private float h = 0f, s = 0f, v = 1f;

    public Color CurrentColor => Color.HSVToRGB(h, s, v);

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        wheelImage = GetComponent<RawImage>();
        wheelImage.texture = CreateWheelTexture(TextureSize);

        if (valueSlider != null)
        {
            valueSlider.minValue = 0f;
            valueSlider.maxValue = 1f;
            valueSlider.SetValueWithoutNotify(v);
            valueSlider.onValueChanged.AddListener(OnValueChanged);

            // Slider hat kein eigenes "losgelassen"-Event → per EventTrigger nachrüsten
            var trigger = valueSlider.gameObject.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerDown, () => onEditStarted.Invoke());
            AddTrigger(trigger, EventTriggerType.PointerUp, () => onColorCommitted.Invoke(CurrentColor));
        }

        Refresh();
    }

    // ---------- Eingabe ----------

    public void OnPointerDown(PointerEventData e)
    {
        // Klick in die transparenten Ecken des Quadrats ignorieren
        if (!TryGetNormalized(e, out Vector2 n) || n.magnitude > 1f) return;

        isDragging = true;
        onEditStarted.Invoke();
        Pick(n);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!isDragging) return;
        if (TryGetNormalized(e, out Vector2 n)) Pick(n);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!isDragging) return;
        isDragging = false;
        onColorCommitted.Invoke(CurrentColor);
    }

    // Mausposition → -1..1 relativ zur Mitte des Wheels
    bool TryGetNormalized(PointerEventData e, out Vector2 n)
    {
        n = Vector2.zero;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, e.position, e.pressEventCamera, out Vector2 local))
            return false;

        n = (local - rect.rect.center) / (rect.rect.size * 0.5f);
        return true;
    }

    void Pick(Vector2 n)
    {
        s = Mathf.Clamp01(n.magnitude); // außerhalb der Scheibe → am Rand festhalten
        h = Mathf.Repeat(Mathf.Atan2(n.y, n.x) / (2f * Mathf.PI), 1f);
        Refresh();
        onColorChanged.Invoke(CurrentColor);
    }

    void OnValueChanged(float value)
    {
        v = value;
        Refresh();
        onColorChanged.Invoke(CurrentColor);
    }

    // ---------- Von außen setzen (z.B. wenn ein Objekt selektiert wird) ----------

    public void SetColorWithoutNotify(Color color)
    {
        Color.RGBToHSV(color, out float newH, out float newS, out float newV);

        // Bei Grau/Schwarz Hue und Saturation behalten, sonst springt der Cursor in die Mitte
        if (newV > 0.001f)
        {
            if (newS > 0.001f) h = newH;
            s = newS;
        }
        v = newV;

        if (valueSlider != null) valueSlider.SetValueWithoutNotify(v);
        Refresh();
    }

    // ---------- Darstellung ----------

    void Refresh()
    {
        // Textur ist bei Value = 1 gebacken; Abdunkeln läuft über die Tint-Farbe,
        // dadurch muss die Textur nie neu berechnet werden.
        wheelImage.color = new Color(v, v, v, 1f);

        if (cursor != null)
        {
            float angle = h * 2f * Mathf.PI;
            Vector2 half = rect.rect.size * 0.5f;
            cursor.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * s * half;
        }

        if (preview != null) preview.color = CurrentColor;
    }

    Texture2D CreateWheelTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - radius) / radius;
                float dy = (y + 0.5f - radius) / radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float hue = Mathf.Repeat(Mathf.Atan2(dy, dx) / (2f * Mathf.PI), 1f);

                Color c = Color.HSVToRGB(hue, Mathf.Clamp01(dist), 1f);
                c.a = Mathf.Clamp01((1f - dist) * radius); // 1px weicher Rand
                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }
}
