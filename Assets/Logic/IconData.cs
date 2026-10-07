using UnityEngine;

[CreateAssetMenu(fileName = "NewIcon", menuName = "BioRender/Icon")]
public class IconData : ScriptableObject
{
    public string iconName;
    public Sprite sprite;
    public string category;

    [Tooltip("Suchbegriffe zusaetzlich zum Namen")]
    public string[] tags;

    [Tooltip("Nur bei importierten Bildern gesetzt: Dateiname im ImportedImages-Ordner. " +
             "Leer = eingebautes Icon aus Resources.")]
    [HideInInspector] public string customFileName;

    public bool IsCustom => !string.IsNullOrEmpty(customFileName);

    // Prueft, ob dieses Icon zu einem Suchbegriff passt (Name oder Tags).
    public bool MatchesSearch(string search)
    {
        if (string.IsNullOrEmpty(search)) return true;

        string s = search.ToLower();
        if (iconName != null && iconName.ToLower().Contains(s)) return true;

        if (tags != null)
        {
            foreach (string tag in tags)
                if (tag != null && tag.ToLower().Contains(s)) return true;
        }

        return false;
    }

    // Exakter Tag-Vergleich fuer den Tag-Filter in der Sidebar
    // (MatchesSearch nutzt bewusst Teilstrings, das waere hier zu unscharf).
    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return true;
        if (tags == null) return false;

        foreach (string t in tags)
            if (t != null && string.Equals(t.Trim(), tag, System.StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
