using System.Collections.Generic;
using UnityEngine;

// Gruppieren und Entgruppieren rueckgaengig machen.
//
// Ein Gruppenobjekt laesst sich nicht einfach aufbewahren und spaeter wieder
// aktivieren: GroupObject traegt sich in Awake in AllGroups ein und baut seinen
// Rahmen auf. Deshalb wird beim Undo/Redo jeweils ein neues Gruppenobjekt
// erzeugt und die gemerkten Mitglieder wieder eingehaengt.
public class GroupCommand : ICommand
{
    private List<GameObject> members = new List<GameObject>();
    private string groupName;
    private Transform container;
    private bool isUngroup;

    private GameObject currentGroup;

    // Gruppieren: group ist die soeben erzeugte Gruppe
    public static GroupCommand ForGrouping(GameObject group)
    {
        var cmd = new GroupCommand();
        cmd.isUngroup = false;
        cmd.Capture(group);
        cmd.currentGroup = group;
        return cmd;
    }

    // Entgruppieren: group ist die Gruppe, die gleich aufgeloest wird
    public static GroupCommand ForUngrouping(GameObject group)
    {
        var cmd = new GroupCommand();
        cmd.isUngroup = true;
        cmd.Capture(group);
        cmd.currentGroup = group;
        return cmd;
    }

    void Capture(GameObject group)
    {
        var grp = group.GetComponent<GroupObject>();
        if (grp == null) return;

        groupName = grp.groupName;
        container = group.transform.parent;

        members.Clear();
        foreach (GameObject m in grp.members)
            if (m != null) members.Add(m);
    }

    public void Execute()
    {
        if (isUngroup) Dissolve();
        else Rebuild();
    }

    public void Undo()
    {
        if (isUngroup) Rebuild();
        else Dissolve();
    }

    void Rebuild()
    {
        // Nur noch vorhandene Mitglieder - einzelne koennen inzwischen
        // geloescht worden sein
        var valid = new List<GameObject>();
        foreach (GameObject m in members)
            if (m != null) valid.Add(m);

        if (valid.Count < 2) return;

        GameObject groupObj = new GameObject("Group");
        groupObj.transform.SetParent(container);

        var grp = groupObj.AddComponent<GroupObject>();
        grp.SetTitle(groupName);

        foreach (GameObject m in valid)
        {
            m.GetComponent<SelectionOutline>()?.Hide();
            grp.AddMember(m);
        }

        grp.UpdateBounds();
        currentGroup = groupObj;
    }

    void Dissolve()
    {
        if (currentGroup == null) return;

        SelectionManager.Instance?.DeselectAll();

        var grp = currentGroup.GetComponent<GroupObject>();
        if (grp != null) grp.Dissolve();
        else Object.Destroy(currentGroup);

        currentGroup = null;
    }
}
