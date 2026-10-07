using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;
    public GameObject selectedObject { get; private set; }
    public List<GameObject> selectedObjects = new List<GameObject>();

    void Awake() => Instance = this;

    void Update()
    {
        HandleSelection();
        HandleDelete();
        HandleGroup();
    }

    static bool PointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }

    void HandleSelection()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (PointerOverUI()) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;

        // Handles haben Vorrang - keine Selektionsaenderung ausloesen
        if (HitUtility.IsHandleHit(worldPos)) return;

        GameObject target = HitUtility.GetPreferredTarget(worldPos);

        if (target != null &&
            (target.GetComponent<DraggableIcon>() != null ||
             target.GetComponent<TextLabel>() != null ||
             target.GetComponent<GroupObject>() != null))
        {
            if (Keyboard.current.shiftKey.isPressed)
                AddToSelection(target);
            else
                Select(target);
            return;
        }

        DeselectAll();
    }

    void HandleDelete()
    {
        if (!Keyboard.current.deleteKey.wasPressedThisFrame &&
            !Keyboard.current.backspaceKey.wasPressedThisFrame) return;

        // Wenn die Maus ueber der UI ist, gehoert die Entf-Taste der UI
        // (z.B. dem Tag-Filter in der Sidebar), nicht der Canvas.
        if (PointerOverUI()) return;

        foreach (GameObject obj in new List<GameObject>(selectedObjects))
        {
            if (obj == null) continue;
            UndoManager.Instance.ExecuteCommand(
                new DeleteCommand(obj, obj.transform.parent)
            );
        }
        selectedObjects.Clear();
        selectedObject = null;
    }

    void HandleGroup()
    {
        if (!Keyboard.current.ctrlKey.isPressed) return;

        if (Keyboard.current.shiftKey.isPressed &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            UngroupSelected();
            return;
        }

        if (Keyboard.current.gKey.wasPressedThisFrame)
            GroupSelected();
    }

    public void Select(GameObject obj)
    {
        DeselectAll();
        if (obj == null) return;

        selectedObject = obj;
        selectedObjects.Add(obj);

        GroupObject grp = obj.GetComponent<GroupObject>();
        if (grp == null)
            obj.GetComponent<SelectionOutline>()?.Show();
        else
            grp.SetSelected(true);

        obj.GetComponent<ArrowObject>()?.ShowHandles();
    }

    public void AddToSelection(GameObject obj)
    {
        if (obj == null) return;

        if (selectedObjects.Contains(obj))
        {
            selectedObjects.Remove(obj);
            obj.GetComponent<SelectionOutline>()?.Hide();
            obj.GetComponent<GroupObject>()?.SetSelected(false);
            obj.GetComponent<ArrowObject>()?.HideHandles();

            selectedObject = selectedObjects.Count > 0
                ? selectedObjects[selectedObjects.Count - 1]
                : null;
        }
        else
        {
            selectedObjects.Add(obj);
            selectedObject = obj;

            GroupObject grp = obj.GetComponent<GroupObject>();
            if (grp == null)
                obj.GetComponent<SelectionOutline>()?.Show();
            else
                grp.SetSelected(true);

            obj.GetComponent<ArrowObject>()?.ShowHandles();
        }
    }

    public void Deselect()
    {
        if (selectedObject != null)
        {
            selectedObject.GetComponent<SelectionOutline>()?.Hide();
            selectedObject.GetComponent<GroupObject>()?.SetSelected(false);
            selectedObject.GetComponent<ArrowObject>()?.HideHandles();
            selectedObjects.Remove(selectedObject); // war frueher vergessen -> Zombie-Referenz
        }
        selectedObject = null;
    }

    public void DeselectAll()
    {
        // Defensiv: kaputte/null Referenzen ueberspringen statt die ganze
        // Selektion fuer immer zu blockieren, falls doch mal eine reinrutscht.
        foreach (GameObject obj in selectedObjects)
        {
            if (obj == null) continue;
            obj.GetComponent<SelectionOutline>()?.Hide();
            obj.GetComponent<GroupObject>()?.SetSelected(false);
            obj.GetComponent<ArrowObject>()?.HideHandles();
        }
        selectedObjects.Clear();
        selectedObject = null;
    }

    // Fuer Toolbar-Buttons
    public void GroupSelectedPublic() => GroupSelected();
    public void UngroupSelectedPublic() => UngroupSelected();

    void GroupSelected()
    {
        if (selectedObjects.Count < 2)
        {
            Debug.Log("Mindestens 2 Objekte selektieren!");
            return;
        }

        GameObject groupObj = new GameObject("Group");
        groupObj.transform.SetParent(selectedObjects[0].transform.parent);
        GroupObject group = groupObj.AddComponent<GroupObject>();

        foreach (GameObject obj in selectedObjects)
        {
            if (obj == null) continue;

            // Aus einer eventuellen alten Gruppe entfernen
            foreach (GroupObject g in new List<GroupObject>(GroupObject.AllGroups))
            {
                if (g != null && g != group && g.members.Contains(obj))
                    g.RemoveMember(obj);
            }

            group.AddMember(obj);
            obj.GetComponent<SelectionOutline>()?.Hide();
        }

        group.UpdateBounds();

        // Fuer Strg+Z merken. Die Gruppe steht schon, deshalb nur
        // registrieren statt nochmal auszufuehren.
        UndoManager.Instance.RegisterCommand(GroupCommand.ForGrouping(groupObj));

        DeselectAll();
        Select(groupObj);
    }

    void UngroupSelected()
    {
        if (selectedObject == null) return;

        GroupObject group = selectedObject.GetComponent<GroupObject>();
        if (group == null) return;

        // Mitglieder merken, bevor die Gruppe verschwindet
        var cmd = GroupCommand.ForUngrouping(group.gameObject);

        group.Dissolve();
        DeselectAll();

        UndoManager.Instance.RegisterCommand(cmd);
    }
}
