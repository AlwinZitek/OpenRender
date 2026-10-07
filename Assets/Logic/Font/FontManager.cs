using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Schriftart der ausgewaehlten Textobjekte umstellen.
//
// Die Schriften muessen als TMP-Font-Assets vorliegen. Aus einer .ttf erzeugt
// man eins ueber Window > TextMeshPro > Font Asset Creator.
public class FontManager : MonoBehaviour
{
    public static FontManager Instance;

    [Header("Schriften")]
    [Tooltip("Auswahl fuer das Dropdown. Reihenfolge = Reihenfolge im Dropdown.")]
    public TMP_FontAsset[] fonts;

    [Tooltip("Anzeigenamen. Leer = Name des Font-Assets")]
    public string[] displayNames;

    [Tooltip("Schrift fuer neu erzeugte Textfelder. Leer = TMP-Standard")]
    public TMP_FontAsset defaultFont;

    [Header("UI")]
    public TMP_Dropdown fontDropdown;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (fontDropdown == null) return;

        var options = new List<string>();
        for (int i = 0; i < fonts.Length; i++)
        {
            if (fonts[i] == null) continue;

            string label = (displayNames != null && i < displayNames.Length &&
                            !string.IsNullOrWhiteSpace(displayNames[i]))
                ? displayNames[i]
                : fonts[i].name;

            options.Add(label);
        }

        fontDropdown.ClearOptions();
        fontDropdown.AddOptions(options);
        fontDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnDropdownChanged(int index)
    {
        if (index < 0 || index >= fonts.Length) return;
        ApplyFont(fonts[index]);
    }

    public void ApplyFont(TMP_FontAsset font)
    {
        if (font == null) return;
        if (SelectionManager.Instance == null) return;

        var targets = new List<GameObject>();
        Collect(SelectionManager.Instance.selectedObjects, targets);

        if (targets.Count == 0) return;

        var cmd = new FontCommand();

        foreach (GameObject obj in targets)
        {
            TMP_FontAsset old = FontCommand.GetFont(obj);
            if (old == font) continue;
            cmd.AddEntry(obj, old, font);
        }

        if (cmd.IsEmpty) return;

        cmd.Execute();
        UndoManager.Instance.RegisterCommand(cmd);
    }

    // Nur Textobjekte, Gruppen rekursiv aufloesen - wie beim ColorManager
    void Collect(List<GameObject> selection, List<GameObject> result)
    {
        foreach (GameObject obj in selection)
        {
            if (obj == null) continue;

            var grp = obj.GetComponent<GroupObject>();
            if (grp != null)
            {
                Collect(grp.members, result);
            }
            else if (obj.GetComponent<TextMeshPro>() != null && !result.Contains(obj))
            {
                result.Add(obj);
            }
        }
    }

    // Fuer Save/Load: Schrift ueber ihren Asset-Namen wiederfinden
    public TMP_FontAsset FindFont(string fontName)
    {
        if (string.IsNullOrEmpty(fontName)) return null;

        foreach (TMP_FontAsset f in fonts)
            if (f != null && f.name == fontName) return f;

        return null;
    }
}
