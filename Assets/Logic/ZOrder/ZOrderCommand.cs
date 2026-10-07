using System.Collections.Generic;
using UnityEngine;

public class ZOrderCommand : ICommand
{
    private class Entry
    {
        public GameObject obj;
        public int oldOrder;
        public int newOrder;
    }

    private List<Entry> entries = new List<Entry>();

    public bool IsEmpty => entries.Count == 0;

    public void AddEntry(GameObject obj, int oldOrder, int newOrder)
    {
        entries.Add(new Entry { obj = obj, oldOrder = oldOrder, newOrder = newOrder });
    }

    public void Execute()
    {
        foreach (Entry e in entries)
            ZOrderUtility.SetOrder(e.obj, e.newOrder);
    }

    public void Undo()
    {
        foreach (Entry e in entries)
            ZOrderUtility.SetOrder(e.obj, e.oldOrder);
    }
}
