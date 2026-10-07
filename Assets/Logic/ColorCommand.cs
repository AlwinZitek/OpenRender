using System.Collections.Generic;
using UnityEngine;

public class ColorCommand : ICommand
{
    private class Entry
    {
        public GameObject obj;
        public Color oldColor;
        public Color newColor;
    }

    private List<Entry> entries = new List<Entry>();

    public bool IsEmpty => entries.Count == 0;

    public void AddEntry(GameObject obj, Color oldColor, Color newColor)
    {
        entries.Add(new Entry { obj = obj, oldColor = oldColor, newColor = newColor });
    }

    public void Execute()
    {
        foreach (Entry e in entries)
            ApplyColorToObject(e.obj, e.newColor);
    }

    public void Undo()
    {
        foreach (Entry e in entries)
            ApplyColorToObject(e.obj, e.oldColor);
    }

    public static void ApplyColorToObject(GameObject obj, Color color)
    {
        if (obj == null) return;

        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;

        var tmp = obj.GetComponent<TMPro.TextMeshPro>();
        if (tmp != null) tmp.color = color;

        var arrow = obj.GetComponent<ArrowObject>();
        if (arrow != null) arrow.SetColor(color);
    }

    public static Color GetCurrentColor(GameObject obj)
    {
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.color;

        var tmp = obj.GetComponent<TMPro.TextMeshPro>();
        if (tmp != null) return tmp.color;

        var arrow = obj.GetComponent<ArrowObject>();
        if (arrow != null) return arrow.arrowColor;

        return Color.white;
    }
}
