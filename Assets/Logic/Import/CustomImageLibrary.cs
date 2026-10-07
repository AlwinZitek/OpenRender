using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class CustomImageEntry
{
    public string fileName;     // Dateiname im ImportedImages-Ordner
    public string displayName;  // Anzeigename in der Sidebar
    public List<string> tags = new List<string>();
}

[System.Serializable]
public class CustomImageLibraryData
{
    public List<CustomImageEntry> entries = new List<CustomImageEntry>();

    // Tag-Verzeichnis, unabhaengig von den Bildern. Bleibt bestehen, auch
    // wenn das letzte Bild mit diesem Tag geloescht wird.
    public List<string> knownTags = new List<string>();
}

// Verwaltet die Bibliothek der importierten Bilder: Metadaten in einer JSON,
// Bilddateien im ImportedImages-Ordner. Erzeugt daraus zur Laufzeit IconData,
// damit importierte Bilder in der Sidebar genauso funktionieren wie die
// eingebauten Icons aus Resources.
public class CustomImageLibrary : MonoBehaviour
{
    public static CustomImageLibrary Instance;

    public const string CustomCategory = "Eigene";

    private const string FolderName = "ImportedImages";
    private const string LibraryFileName = "imported_library.json";

    private CustomImageLibraryData data = new CustomImageLibraryData();
    private List<IconData> runtimeIcons = new List<IconData>();

    void Awake()
    {
        Instance = this;
        Directory.CreateDirectory(GetFolderPath());
        Load();
    }

    public string GetFolderPath()
    {
        return Path.Combine(Application.persistentDataPath, FolderName);
    }

    public string GetLibraryPath()
    {
        return Path.Combine(GetFolderPath(), LibraryFileName);
    }

    public string GetImagePath(string fileName)
    {
        return Path.Combine(GetFolderPath(), fileName);
    }

    void Load()
    {
        runtimeIcons.Clear();

        string path = GetLibraryPath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<CustomImageLibraryData>(json) ?? new CustomImageLibraryData();
        }
        else
        {
            data = new CustomImageLibraryData();
        }

        // Eintraege ohne vorhandene Bilddatei aussortieren
        data.entries.RemoveAll(e => e == null || !File.Exists(GetImagePath(e.fileName)));

        foreach (CustomImageEntry entry in data.entries)
        {
            IconData icon = BuildIconData(entry);
            if (icon != null) runtimeIcons.Add(icon);
        }
    }

    void Save()
    {
        File.WriteAllText(GetLibraryPath(), JsonUtility.ToJson(data, true));
    }

    IconData BuildIconData(CustomImageEntry entry)
    {
        Sprite sprite = LoadSprite(entry.fileName);
        if (sprite == null) return null;

        IconData icon = ScriptableObject.CreateInstance<IconData>();
        icon.iconName = entry.displayName;
        icon.sprite = sprite;
        icon.category = CustomCategory;
        icon.tags = entry.tags != null ? entry.tags.ToArray() : new string[0];
        icon.customFileName = entry.fileName;
        return icon;
    }

    public Sprite LoadSprite(string fileName)
    {
        string path = GetImagePath(fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning("Importierte Grafik fehlt: " + path);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);

        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            Vector2.one * 0.5f,
            100f
        );
    }

    // Neues Bild in die Bibliothek aufnehmen. Gibt das erzeugte IconData zurueck.
    public IconData AddEntry(string fileName, string displayName, List<string> tags)
    {
        // Tags ins Verzeichnis aufnehmen, damit sie das Bild ueberleben
        if (tags != null)
            foreach (string tag in tags) RegisterTag(tag);

        CustomImageEntry entry = new CustomImageEntry
        {
            fileName = fileName,
            displayName = string.IsNullOrWhiteSpace(displayName)
                ? Path.GetFileNameWithoutExtension(fileName)
                : displayName.Trim(),
            tags = tags ?? new List<string>()
        };

        data.entries.Add(entry);
        Save();

        IconData icon = BuildIconData(entry);
        if (icon != null) runtimeIcons.Add(icon);
        return icon;
    }

    // Entfernt einen Tag aus allen importierten Bildern und speichert.
    // Gibt zurueck, ob ueberhaupt etwas geaendert wurde.
    public bool RemoveTagFromAll(string tag)
    {
        bool changed = false;

        foreach (CustomImageEntry entry in data.entries)
        {
            if (entry.tags == null) continue;
            int removed = entry.tags.RemoveAll(
                t => string.Equals(t?.Trim(), tag, System.StringComparison.OrdinalIgnoreCase));
            if (removed > 0) changed = true;
        }

        if (!changed) return false;

        Save();

        // Runtime-IconData nachziehen, damit die Sidebar sofort stimmt
        foreach (IconData icon in runtimeIcons)
        {
            CustomImageEntry entry = data.entries.Find(e => e.fileName == icon.customFileName);
            if (entry != null)
                icon.tags = entry.tags.ToArray();
        }

        return true;
    }

    // Nimmt einen Tag ins Verzeichnis auf, falls noch nicht vorhanden.
    public void RegisterTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        tag = tag.Trim();

        if (data.knownTags == null) data.knownTags = new List<string>();

        if (!data.knownTags.Exists(t => string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase)))
        {
            data.knownTags.Add(tag);
            Save();
        }
    }

    // Loescht einen Tag endgueltig: aus dem Verzeichnis UND aus allen Bildern.
    public void DeleteTagCompletely(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        tag = tag.Trim();

        if (data.knownTags != null)
            data.knownTags.RemoveAll(
                t => string.Equals(t?.Trim(), tag, System.StringComparison.OrdinalIgnoreCase));

        RemoveTagFromAll(tag);
        Save();
    }

    public List<string> GetKnownTags()
    {
        return data.knownTags ?? new List<string>();
    }

    // Loescht ein Bild. Die Tags bleiben im Verzeichnis erhalten.
    public void RemoveEntry(string fileName)
    {
        data.entries.RemoveAll(e => e.fileName == fileName);
        runtimeIcons.RemoveAll(i => i.customFileName == fileName);
        Save();

        string path = GetImagePath(fileName);
        if (File.Exists(path)) File.Delete(path);
    }

    public List<IconData> GetIcons()
    {
        return runtimeIcons;
    }

    // Alle bisher vergebenen Tags, alphabetisch sortiert und ohne Duplikate.
    // Fuellt das Dropdown im Import-Dialog.
    public List<string> GetAllTags()
    {
        List<string> all = new List<string>();

        foreach (CustomImageEntry entry in data.entries)
        {
            if (entry.tags == null) continue;
            foreach (string tag in entry.tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                if (!all.Exists(t => string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase)))
                    all.Add(tag);
            }
        }

        all.Sort(System.StringComparer.CurrentCultureIgnoreCase);
        return all;
    }

    // Tags aus einem Eingabefeld parsen: "zelle, membran; rot" -> drei Tags
    public static List<string> ParseTags(string raw)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrWhiteSpace(raw)) return result;

        foreach (string part in raw.Split(',', ';'))
        {
            string t = part.Trim();
            if (t.Length > 0 && !result.Contains(t)) result.Add(t);
        }
        return result;
    }
}
