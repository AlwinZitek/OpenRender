using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Dialog nach der Dateiauswahl: Name und Tags fuer das importierte Bild.
// Tags koennen aus bereits vergebenen ausgewaehlt (Dropdown) oder neu
// eingetippt werden (Textfeld). Ausgewaehlte Tags erscheinen als Chips.
public class ImportDialogUI : MonoBehaviour
{
    public static ImportDialogUI Instance;

    [Header("UI")]
    public GameObject panel;
    public Image previewImage;
    public TMP_InputField nameInput;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Tags")]
    [Tooltip("Dropdown mit bereits vergebenen Tags")]
    public TMP_Dropdown tagDropdown;
    [Tooltip("Freitext fuer neue Tags, mit Komma getrennt")]
    public TMP_InputField newTagInput;
    [Tooltip("Button, der den Freitext als Tag uebernimmt (optional)")]
    public Button addTagButton;
    [Tooltip("Container fuer die Tag-Chips")]
    public Transform chipContainer;
    [Tooltip("Prefab fuer einen Chip: Button mit TMP-Text als Kind")]
    public GameObject chipPrefab;

    [Header("Optional")]
    [Tooltip("Haken: Bild nach dem Import direkt auf die Canvas setzen")]
    public Toggle spawnAfterImportToggle;

    private const string DropdownPlaceholder = "Tag auswählen…";

    private string pendingFileName;
    private Action<string, string, bool> onConfirm;
    private List<string> selectedTags = new List<string>();

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);

        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Cancel);

        if (tagDropdown != null)
            tagDropdown.onValueChanged.AddListener(OnDropdownChanged);

        if (addTagButton != null)
            addTagButton.onClick.AddListener(AddFromInput);

        if (newTagInput != null)
            newTagInput.onSubmit.AddListener(_ => AddFromInput());
    }

    public void Open(string storedFileName, string originalPath, Sprite preview,
                     Action<string, string, bool> confirmCallback)
    {
        pendingFileName = storedFileName;
        onConfirm = confirmCallback;

        nameInput.text = Path.GetFileNameWithoutExtension(originalPath);
        if (newTagInput != null) newTagInput.text = "";

        selectedTags.Clear();
        RebuildChips();
        RebuildDropdown();

        if (previewImage != null)
        {
            previewImage.sprite = preview;
            previewImage.enabled = preview != null;
            previewImage.preserveAspect = true;
        }

        panel.SetActive(true);
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    // Dropdown mit allen bisher vergebenen Tags fuellen. Bereits ausgewaehlte
    // werden ausgelassen, damit man sie nicht doppelt anklicken kann.
    void RebuildDropdown()
    {
        if (tagDropdown == null) return;

        List<string> options = new List<string> { DropdownPlaceholder };

        // Tags vom SidebarManager holen: der kennt eingebaute UND importierte
        // Icons, die Library allein nur die importierten.
        if (SidebarManager.Instance != null)
        {
            foreach (string tag in SidebarManager.Instance.GetAllTags())
            {
                if (!ContainsTag(tag)) options.Add(tag);
            }
        }

        tagDropdown.ClearOptions();
        tagDropdown.AddOptions(options);
        tagDropdown.SetValueWithoutNotify(0);
        tagDropdown.interactable = options.Count > 1;
    }

    void OnDropdownChanged(int index)
    {
        if (index <= 0) return; // Platzhalter

        AddTag(tagDropdown.options[index].text);
        tagDropdown.SetValueWithoutNotify(0);
        RebuildDropdown();
    }

    void AddFromInput()
    {
        if (newTagInput == null) return;

        foreach (string tag in CustomImageLibrary.ParseTags(newTagInput.text))
            AddTag(tag);

        newTagInput.text = "";
        newTagInput.ActivateInputField();
        RebuildDropdown();
    }

    void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        tag = tag.Trim();
        if (ContainsTag(tag)) return;

        selectedTags.Add(tag);
        RebuildChips();
    }

    void RemoveTag(string tag)
    {
        selectedTags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        RebuildChips();
        RebuildDropdown();
    }

    bool ContainsTag(string tag)
    {
        return selectedTags.Exists(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
    }

    // Chips neu aufbauen. Klick auf einen Chip entfernt ihn wieder.
    void RebuildChips()
    {
        if (chipContainer == null || chipPrefab == null) return;

        foreach (Transform child in chipContainer)
            Destroy(child.gameObject);

        foreach (string tag in selectedTags)
        {
            GameObject chip = Instantiate(chipPrefab, chipContainer);

            var label = chip.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = tag + "  ×";

            string captured = tag; // lokale Kopie fuer die Lambda
            var btn = chip.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => RemoveTag(captured));
        }
    }

    void Confirm()
    {
        // Was noch im Freitextfeld steht, aber nicht bestaetigt wurde,
        // trotzdem uebernehmen - sonst geht es beim Klick auf Importieren verloren.
        if (newTagInput != null && !string.IsNullOrWhiteSpace(newTagInput.text))
        {
            foreach (string tag in CustomImageLibrary.ParseTags(newTagInput.text))
                AddTag(tag);
        }

        bool spawn = spawnAfterImportToggle == null || spawnAfterImportToggle.isOn;
        onConfirm?.Invoke(nameInput.text, string.Join(",", selectedTags), spawn);
        Close();
    }

    void Cancel()
    {
        // Bereits kopierte Datei wieder entfernen, damit keine Leichen
        // im ImportedImages-Ordner zurueckbleiben.
        if (!string.IsNullOrEmpty(pendingFileName) && CustomImageLibrary.Instance != null)
        {
            string path = CustomImageLibrary.Instance.GetImagePath(pendingFileName);
            if (File.Exists(path)) File.Delete(path);
        }

        Close();
    }

    void Close()
    {
        panel.SetActive(false);
        pendingFileName = null;
        onConfirm = null;
        selectedTags.Clear();
    }
}
