using TMPro;
using UnityEngine;
using System.Collections;

public class TextLabel : MonoBehaviour
{
    public TextMeshPro tmp;
    public string labelText = "Text";

    [Tooltip("Schriftgroesse in Weltunits")]
    public float fontSize = 1.5f;

    public Color textColor = Color.black;

    [Tooltip("Zusaetzlicher Rand um den Text herum, in Weltunits")]
    public float colliderPadding = 0.05f;

    private BoxCollider2D col;

    void Awake()
    {
        tmp = gameObject.AddComponent<TextMeshPro>();
        tmp.text = labelText;
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 5;
        tmp.autoSizeTextContainer = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;

        // Voreingestellte Schrift uebernehmen, falls eine gesetzt ist
        if (FontManager.Instance != null && FontManager.Instance.defaultFont != null)
            tmp.font = FontManager.Instance.defaultFont;

        col = gameObject.AddComponent<BoxCollider2D>();
        UpdateCollider();

        // Ohne diese beiden gibt es weder Auswahl noch Verschieben
        gameObject.AddComponent<DraggableIcon>();
        gameObject.AddComponent<SelectionOutline>();
    }

    public void SetText(string text)
    {
        labelText = text;
        if (tmp == null) return;

        tmp.text = text;
        // Groesse hier nochmal setzen: wer TextLabel per AddComponent anlegt,
        // kann fontSize erst NACH Awake zuweisen.
        tmp.fontSize = fontSize;

        UpdateCollider();
    }

    public void SetFontSize(float size)
    {
        fontSize = size;
        if (tmp != null) tmp.fontSize = size;
        UpdateCollider();
    }

    // Passt den Collider an den tatsaechlich gezeichneten Text an.
    //
    // Frueher ueber preferredWidth/preferredHeight - das ist die vom Text
    // angeforderte Containergroesse und faellt deutlich groesser aus als das,
    // was man sieht. Die Mesh-Begrenzung des Renderers ist genau der sichtbare
    // Bereich. ForceMeshUpdate macht sie sofort aktuell, es braucht also kein
    // Warten auf den naechsten Frame mehr.
    public void UpdateCollider()
    {
        if (tmp == null) return;
        if (col == null) col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        tmp.ForceMeshUpdate();

        // Direkt aus den Eckpunkten der sichtbaren Zeichen rechnen.
        // Renderer.bounds ist direkt nach ForceMeshUpdate noch nicht
        // zuverlaessig, und preferredWidth ist die angeforderte
        // Containergroesse, nicht der sichtbare Text.
        var info = tmp.textInfo;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        bool any = false;

        if (info != null)
        {
            for (int i = 0; i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;

                minX = Mathf.Min(minX, ch.bottomLeft.x);
                minY = Mathf.Min(minY, ch.bottomLeft.y);
                maxX = Mathf.Max(maxX, ch.topRight.x);
                maxY = Mathf.Max(maxY, ch.topRight.y);
                any = true;
            }
        }

        // Leeres Label oder nur Leerzeichen: kleines Feld, damit es
        // anklickbar bleibt
        if (!any)
        {
            float s = fontSize * 0.6f;
            col.size = new Vector2(s, s);
            col.offset = Vector2.zero;
            return;
        }

        float w = maxX - minX;
        float h = maxY - minY;

        col.size = new Vector2(w + colliderPadding * 2f, h + colliderPadding * 2f);
        col.offset = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }
}
