using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FontCommand : ICommand
{
    private class Entry
    {
        public GameObject obj;
        public TMP_FontAsset oldFont;
        public TMP_FontAsset newFont;
    }

    private List<Entry> entries = new List<Entry>();

    public bool IsEmpty => entries.Count == 0;

    public void AddEntry(GameObject obj, TMP_FontAsset oldFont, TMP_FontAsset newFont)
    {
        entries.Add(new Entry { obj = obj, oldFont = oldFont, newFont = newFont });
    }

    public void Execute()
    {
        foreach (Entry e in entries) Apply(e.obj, e.newFont);
    }

    public void Undo()
    {
        foreach (Entry e in entries) Apply(e.obj, e.oldFont);
    }

    public static void Apply(GameObject obj, TMP_FontAsset font)
    {
        if (obj == null || font == null) return;

        var tmp = obj.GetComponent<TextMeshPro>();
        if (tmp != null) tmp.font = font;
    }

    public static TMP_FontAsset GetFont(GameObject obj)
    {
        if (obj == null) return null;
        var tmp = obj.GetComponent<TextMeshPro>();
        return tmp != null ? tmp.font : null;
    }
}
