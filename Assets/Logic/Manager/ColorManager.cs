using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorManager : MonoBehaviour
{
    public static ColorManager Instance;

    // Vordefinierte Farben
    private Color[] colors = new Color[]
    {
        Color.black,
        Color.white,
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(1f, 0.5f, 0f), // Orange
        new Color(0.5f, 0f, 0.5f) // Lila
    };

    public GameObject colorButtonPrefab;
    public Transform colorPanel;
    public ColorWheel colorWheel; // ← neu: im Inspector reinziehen (optional)

    // Laufender Wheel-Edit ← neu
    private bool isEditing = false;
    private List<GameObject> editTargets = new List<GameObject>();
    private List<Color> editOldColors = new List<Color>();

    void Awake() => Instance = this;

    void Start()
    {
        // Farb-Buttons erstellen
        foreach (Color color in colors)
        {
            GameObject btn = Instantiate(colorButtonPrefab, colorPanel);
            btn.GetComponent<Image>().color = color;
            Color c = color; // lokale Kopie für Lambda
            btn.GetComponent<Button>().onClick.AddListener(() => ApplyColor(c));
        }

        // Color Wheel verkabeln ← neu
        if (colorWheel != null)
        {
            colorWheel.onColorChanged.AddListener(PreviewColor);
            colorWheel.onColorCommitted.AddListener(CommitEdit);
        }
    }

    public void ApplyColor(Color color)
    {
        // Wheel auf die Paletten-Farbe mitziehen ← neu
        if (colorWheel != null) colorWheel.SetColorWithoutNotify(color);

        List<GameObject> targets = new List<GameObject>();
        CollectColorableTargets(SelectionManager.Instance.selectedObjects, targets);

        if (targets.Count == 0) return;

        ColorCommand cmd = new ColorCommand();

        foreach (GameObject obj in targets)
        {
            Color oldColor = ColorCommand.GetCurrentColor(obj);
            if (oldColor == color) continue; // schon diese Farbe, nichts zu tun
            cmd.AddEntry(obj, oldColor, color);
        }

        if (cmd.IsEmpty) return;

        cmd.Execute(); // Farben direkt anwenden
        UndoManager.Instance.RegisterCommand(cmd); // für Strg+Z merken
    }

    // ---------- Color Wheel ← neu ----------

    // Wird beim Ziehen am Wheel/Slider jeden Frame aufgerufen: nur Vorschau, kein Undo.
    // Beim ersten Aufruf werden die alten Farben gemerkt.
    void PreviewColor(Color color)
    {
        if (!isEditing)
        {
            isEditing = true;
            editTargets.Clear();
            editOldColors.Clear();

            CollectColorableTargets(SelectionManager.Instance.selectedObjects, editTargets);
            foreach (GameObject obj in editTargets)
                editOldColors.Add(ColorCommand.GetCurrentColor(obj));
        }

        foreach (GameObject obj in editTargets)
            ColorCommand.ApplyColorToObject(obj, color);
    }

    // Maus losgelassen: EIN Command mit alten → neuer Farbe registrieren.
    void CommitEdit(Color color)
    {
        if (!isEditing) return; // z.B. Slider-Handle nur angeklickt, nichts geändert
        isEditing = false;

        ColorCommand cmd = new ColorCommand();

        for (int i = 0; i < editTargets.Count; i++)
        {
            if (editTargets[i] == null) continue;
            if (editOldColors[i] == color) continue;
            cmd.AddEntry(editTargets[i], editOldColors[i], color);
        }

        editTargets.Clear();
        editOldColors.Clear();

        if (cmd.IsEmpty) return;

        cmd.Execute();
        UndoManager.Instance.RegisterCommand(cmd);
    }

    // Wheel zeigt die Farbe des (ersten) selektierten Objekts.
    // Aus SelectionManager.Select() / AddToSelection() aufrufen.
    public void SyncWheelToSelection()
    {
        if (colorWheel == null) return;

        List<GameObject> targets = new List<GameObject>();
        CollectColorableTargets(SelectionManager.Instance.selectedObjects, targets);

        if (targets.Count == 0) return;

        colorWheel.SetColorWithoutNotify(ColorCommand.GetCurrentColor(targets[0]));
    }

    // Sammelt alle einfärbbaren Objekte aus der Selektion, inkl. rekursiver
    // Auflösung von Gruppen (auch verschachtelte).
    void CollectColorableTargets(List<GameObject> selection, List<GameObject> result)
    {
        foreach (GameObject obj in selection)
        {
            if (obj == null) continue;

            GroupObject grp = obj.GetComponent<GroupObject>();
            if (grp != null)
            {
                CollectColorableTargets(grp.members, result);
            }
            else if (IsColorable(obj) && !result.Contains(obj))
            {
                result.Add(obj);
            }
        }
    }

    bool IsColorable(GameObject obj)
    {
        return obj.GetComponent<SpriteRenderer>() != null ||
               obj.GetComponent<TextMeshPro>() != null ||
               obj.GetComponent<ArrowObject>() != null;
    }
}
