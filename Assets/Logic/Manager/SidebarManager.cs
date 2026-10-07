using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SidebarManager : MonoBehaviour
{
    public static SidebarManager Instance;

    public IconData[] icons;
    public GameObject iconButtonPrefab;
    public Transform content;
    public TMP_InputField searchBar;

    [Header("Kategorie Buttons")]
    public Button allButton;
    public Button cellButton;
    public Button memButton;
    public Button protButton;
    public Button nucleiButton;
    [Tooltip("Optional: Button fuer importierte Bilder")]
    public Button customButton;

    [Header("Tag-Filter (Dropdown ODER Buttons)")]
    [Tooltip("Variante A: Dropdown zum Filtern nach Tags")]
    public TMP_Dropdown tagFilterDropdown;

    [Tooltip("Variante B: Container, in den pro Tag ein Button gespawnt wird")]
    public Transform tagButtonContainer;
    [Tooltip("Prefab fuer einen Tag-Button: Button mit TMP-Text als Kind")]
    public GameObject tagButtonPrefab;

    [Tooltip("An: ein Icon muss ALLE gewaehlten Tags haben. Aus: mindestens einen.")]
    public bool requireAllTags = false;

    [Header("Tag-Button Farben")]
    public Color tagNormalColor = new Color(0.945f, 0.949f, 0.961f);
    public Color tagActiveColor = new Color(0.357f, 0.373f, 0.937f);
    public Color tagNormalTextColor = new Color(0.169f, 0.176f, 0.200f);
    public Color tagActiveTextColor = Color.white;

    private const string AllTagsOption = "Alle Tags";

    private string currentCategory = "Alle";
    private string currentSearch = "";
    private string currentTag = "";
    private List<string> activeTags = new List<string>();
    private string hoveredTag = "";

    void Awake() => Instance = this;

    void Start()
    {
        RefreshIcons();

        searchBar.onValueChanged.AddListener(OnSearchChanged);

        allButton.onClick.AddListener(() => SetCategory("Alle"));
        cellButton.onClick.AddListener(() => SetCategory("Cells"));
        memButton.onClick.AddListener(() => SetCategory("Membranes"));
        protButton.onClick.AddListener(() => SetCategory("Proteins"));
        nucleiButton.onClick.AddListener(() => SetCategory("Nucleics"));

        if (customButton != null)
            customButton.onClick.AddListener(() => SetCategory(CustomImageLibrary.CustomCategory));

        if (tagFilterDropdown != null)
            tagFilterDropdown.onValueChanged.AddListener(OnTagFilterChanged);

        HighlightButton(allButton);
    }

    void Update()
    {
        // Maus ueber einem Tag-Button + Entf -> Tag loeschen
        if (string.IsNullOrEmpty(hoveredTag)) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.deleteKey.wasPressedThisFrame ||
            Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            DeleteTag(hoveredTag);
        }
    }

    public void SetHoveredTag(string tag) => hoveredTag = tag;

    public void ClearHoveredTag(string tag)
    {
        // Nur loeschen, wenn wirklich dieser Button verlassen wurde -
        // sonst raeumt ein spaet feuerndes OnPointerExit den neuen Hover weg.
        if (string.Equals(hoveredTag, tag, StringComparison.OrdinalIgnoreCase))
            hoveredTag = "";
    }

    // Entfernt einen Tag ueberall. Bei importierten Bildern dauerhaft (JSON),
    // bei eingebauten IconData-Assets nur fuer diese Sitzung - deren Tags
    // stehen im Asset und werden beim naechsten Start wieder geladen.
    public void DeleteTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        // Loescht den Tag aus dem Verzeichnis UND aus allen Bildern -
        // das ist der bewusste Weg, einen Tag loszuwerden.
        if (CustomImageLibrary.Instance != null)
            CustomImageLibrary.Instance.DeleteTagCompletely(tag);

        foreach (IconData icon in icons)
        {
            if (icon == null || icon.tags == null || icon.IsCustom) continue;

            var kept = new List<string>();
            foreach (string t in icon.tags)
            {
                if (t == null) continue;
                if (!string.Equals(t.Trim(), tag, StringComparison.OrdinalIgnoreCase))
                    kept.Add(t);
            }
            icon.tags = kept.ToArray();
        }

        activeTags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        hoveredTag = "";

        RebuildTagFilter();
        RebuildTagButtons();
        UpdateList();
    }

    // Loescht ein importiertes Bild aus der Bibliothek - inklusive Bilddatei.
    // Eingebaute Icons sind Assets und werden ignoriert.
    public void DeleteCustomIcon(IconData icon)
    {
        if (icon == null || !icon.IsCustom) return;
        if (CustomImageLibrary.Instance == null) return;

        CustomImageLibrary.Instance.RemoveEntry(icon.customFileName);
        RefreshIcons();
    }

    // Laedt eingebaute Icons aus Resources und importierte aus der Bibliothek.
    // Wird auch nach einem Import aufgerufen, damit das neue Bild und seine
    // Tags sofort erscheinen.
    public void RefreshIcons()
    {
        var allIcons = new List<IconData>();
        allIcons.AddRange(Resources.LoadAll<IconData>("Cells/"));
        allIcons.AddRange(Resources.LoadAll<IconData>("Membranes/"));
        allIcons.AddRange(Resources.LoadAll<IconData>("Proteins/"));
        allIcons.AddRange(Resources.LoadAll<IconData>("Nucleics/"));

        if (CustomImageLibrary.Instance != null)
            allIcons.AddRange(CustomImageLibrary.Instance.GetIcons());

        icons = allIcons.ToArray();

        RebuildTagFilter();
        RebuildTagButtons();
        UpdateList();
    }

    // Alle Tags aus ALLEN Icons - eingebaute wie importierte. Das ist die
    // zentrale Tag-Quelle, auch fuer das Dropdown im Import-Dialog.
    public List<string> GetAllTags()
    {
        var all = new List<string>();

        // Tags aus dem Verzeichnis: bleiben erhalten, auch wenn gerade kein
        // Bild sie benutzt (z.B. weil das letzte geloescht wurde).
        if (CustomImageLibrary.Instance != null)
        {
            foreach (string raw in CustomImageLibrary.Instance.GetKnownTags())
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string tag = raw.Trim();
                if (!all.Exists(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                    all.Add(tag);
            }
        }

        foreach (IconData icon in icons)
        {
            if (icon == null || icon.tags == null) continue;

            foreach (string raw in icon.tags)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                // Erst trimmen, dann vergleichen: sonst gilt " Membran" als
                // anderer Tag als "Membran" und landet doppelt in der Liste.
                string tag = raw.Trim();
                if (!all.Exists(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                    all.Add(tag);
            }
        }

        all.Sort(StringComparer.CurrentCultureIgnoreCase);
        return all;
    }

    void RebuildTagFilter()
    {
        if (tagFilterDropdown == null) return;

        var options = new List<string> { AllTagsOption };
        options.AddRange(GetAllTags());

        // Aktuelle Auswahl beibehalten, falls der Tag noch existiert
        int keepIndex = 0;
        if (!string.IsNullOrEmpty(currentTag))
        {
            int found = options.FindIndex(o => string.Equals(o, currentTag, StringComparison.OrdinalIgnoreCase));
            if (found > 0) keepIndex = found;
            else currentTag = "";
        }

        tagFilterDropdown.ClearOptions();
        tagFilterDropdown.AddOptions(options);
        tagFilterDropdown.SetValueWithoutNotify(keepIndex);
        tagFilterDropdown.interactable = options.Count > 1;
    }

    void OnTagFilterChanged(int index)
    {
        currentTag = index <= 0 ? "" : tagFilterDropdown.options[index].text;
        UpdateList();
    }

    // Pro Tag einen Button spawnen. Mehrfachauswahl moeglich: ein Klick
    // schaltet den Tag an bzw. wieder aus.
    void RebuildTagButtons()
    {
        if (tagButtonContainer == null || tagButtonPrefab == null) return;

        foreach (Transform child in tagButtonContainer)
            Destroy(child.gameObject);

        List<string> tags = GetAllTags();

        // Tags, die es nicht mehr gibt, aus der Auswahl werfen
        activeTags.RemoveAll(t => !tags.Exists(
            x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase)));

        foreach (string tag in tags)
        {
            GameObject btnObj = Instantiate(tagButtonPrefab, tagButtonContainer);

            var label = btnObj.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = tag;

            bool isActive = IsTagActive(tag);
            ApplyTagButtonStyle(btnObj, isActive);

            var hover = btnObj.GetComponent<TagButtonHover>();
            if (hover == null) hover = btnObj.AddComponent<TagButtonHover>();
            hover.tag = tag;

            string captured = tag; // lokale Kopie fuer die Lambda
            var btn = btnObj.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => ToggleTag(captured));
        }
    }

    void ApplyTagButtonStyle(GameObject btnObj, bool isActive)
    {
        var img = btnObj.GetComponent<Image>();
        if (img != null) img.color = isActive ? tagActiveColor : tagNormalColor;

        var label = btnObj.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = isActive ? tagActiveTextColor : tagNormalTextColor;
    }

    void ToggleTag(string tag)
    {
        if (IsTagActive(tag))
            activeTags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        else
            activeTags.Add(tag);

        RebuildTagButtons();
        UpdateList();
    }

    bool IsTagActive(string tag)
    {
        return activeTags.Exists(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
    }

    public void ClearTagFilter()
    {
        activeTags.Clear();
        RebuildTagButtons();
        UpdateList();
    }

    bool MatchesActiveTags(IconData icon)
    {
        if (activeTags.Count == 0) return true;

        if (requireAllTags)
        {
            foreach (string tag in activeTags)
                if (!icon.HasTag(tag)) return false;
            return true;
        }

        foreach (string tag in activeTags)
            if (icon.HasTag(tag)) return true;
        return false;
    }

    void SetCategory(string category)
    {
        currentCategory = category;
        UpdateList();

        Button target =
            category == "Alle" ? allButton :
            category == "Cells" ? cellButton :
            category == "Membranes" ? memButton :
            category == "Proteins" ? protButton :
            category == CustomImageLibrary.CustomCategory ? customButton :
                                    nucleiButton;

        HighlightButton(target);
    }

    void OnSearchChanged(string searchText)
    {
        currentSearch = searchText;
        UpdateList();
    }

    void UpdateList()
    {
        var filtered = new List<IconData>();

        foreach (IconData icon in icons)
        {
            if (icon == null) continue;

            bool categoryMatch = currentCategory == "Alle" ||
                                 icon.category == currentCategory;

            bool searchMatch = icon.MatchesSearch(currentSearch);

            // Dropdown-Filter und Button-Filter wirken beide, je nachdem
            // welche Variante im Inspector verkabelt ist.
            bool dropdownMatch = string.IsNullOrEmpty(currentTag) || icon.HasTag(currentTag);
            bool buttonMatch = MatchesActiveTags(icon);

            if (categoryMatch && searchMatch && dropdownMatch && buttonMatch)
                filtered.Add(icon);
        }

        SpawnButtons(filtered.ToArray());
    }

    void SpawnButtons(IconData[] iconsToShow)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (IconData icon in iconsToShow)
        {
            GameObject btn = Instantiate(iconButtonPrefab, content);
            btn.GetComponent<IconButton>().Setup(icon);
        }
    }

    void HighlightButton(Button activeBtn)
    {
        var buttons = new List<Button> { allButton, cellButton, memButton, protButton, nucleiButton };
        if (customButton != null) buttons.Add(customButton);

        foreach (Button btn in buttons)
        {
            if (btn == null) continue;
            btn.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);
        }

        if (activeBtn != null)
            activeBtn.GetComponent<Image>().color = Color.white;
    }
}
