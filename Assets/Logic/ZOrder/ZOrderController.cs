using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Ebenen-Steuerung fuer die aktuelle Auswahl.
//
// Tastatur:
//   +  /  -            - eine Ebene vor/zurueck
//   Strg + / Strg -    - ganz nach vorne/hinten
//
// Sowohl die Tasten im Hauptblock als auch die des Nummernblocks.
// Die vier Methoden lassen sich auch an Toolbar-Buttons haengen.
public class ZOrderController : MonoBehaviour
{
    public static ZOrderController Instance;

    void Awake() => Instance = this;

    void Update()
    {
        if (Keyboard.current == null) return;

        // Waehrend man in ein Eingabefeld tippt, gehoeren +/- dem Textfeld
        if (IsTypingInInputField()) return;

        bool ctrl = Keyboard.current.ctrlKey.isPressed;

        if (PlusPressed())
        {
            if (ctrl) BringToFront(); else MoveForward();
        }
        else if (MinusPressed())
        {
            if (ctrl) SendToBack(); else MoveBackward();
        }
    }

    // Auf der deutschen Tastatur liegt + auf einer eigenen Taste, die Unity
    // als equalsKey meldet; dazu noch der Nummernblock.
    bool PlusPressed()
    {
        var k = Keyboard.current;
        return k.numpadPlusKey.wasPressedThisFrame
            || k.equalsKey.wasPressedThisFrame;
    }

    bool MinusPressed()
    {
        var k = Keyboard.current;
        return k.numpadMinusKey.wasPressedThisFrame
            || k.minusKey.wasPressedThisFrame
            || k.slashKey.wasPressedThisFrame; // deutsche Tastatur: - liegt hier
    }

    static bool IsTypingInInputField()
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;

        GameObject selected = es.currentSelectedGameObject;
        if (selected == null) return false;

        return selected.GetComponent<TMPro.TMP_InputField>() != null
            || selected.GetComponent<UnityEngine.UI.InputField>() != null;
    }

    List<GameObject> GetSelection()
    {
        var result = new List<GameObject>();
        if (SelectionManager.Instance == null) return result;

        foreach (GameObject obj in SelectionManager.Instance.selectedObjects)
            if (obj != null) result.Add(obj);

        return result;
    }

    public void MoveForward() => Shift(+1);
    public void MoveBackward() => Shift(-1);

    void Shift(int delta)
    {
        var selection = GetSelection();
        if (selection.Count == 0) return;

        var cmd = new ZOrderCommand();

        foreach (GameObject obj in selection)
        {
            int oldOrder = ZOrderUtility.GetOrder(obj);
            int newOrder = Mathf.Clamp(oldOrder + delta, ZOrderUtility.Min, ZOrderUtility.Max);
            if (newOrder != oldOrder) cmd.AddEntry(obj, oldOrder, newOrder);
        }

        Apply(cmd);
    }

    public void BringToFront() => ToExtreme(true);
    public void SendToBack() => ToExtreme(false);

    void ToExtreme(bool front)
    {
        var selection = GetSelection();
        if (selection.Count == 0) return;

        // Hoechsten bzw. niedrigsten Wert aller anderen Objekte suchen
        int extreme = front ? int.MinValue : int.MaxValue;
        bool found = false;

        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            GameObject go = renderer.gameObject;
            if (selection.Contains(go)) continue;
            if (go.name == "Outline") continue;
            if (go.GetComponent<ScaleHandle>() != null) continue;
            if (go.GetComponent<RotateHandle>() != null) continue;
            if (go.GetComponent<EndpointHandle>() != null) continue;

            int order = renderer.sortingOrder;
            if (order < ZOrderUtility.Min || order > ZOrderUtility.Max) continue;

            extreme = front ? Mathf.Max(extreme, order) : Mathf.Min(extreme, order);
            found = true;
        }

        if (!found) extreme = 0;

        int target = front ? extreme + 1 : extreme - 1;
        var cmd = new ZOrderCommand();

        foreach (GameObject obj in selection)
        {
            int oldOrder = ZOrderUtility.GetOrder(obj);
            if (oldOrder != target) cmd.AddEntry(obj, oldOrder, target);
        }

        Apply(cmd);
    }

    void Apply(ZOrderCommand cmd)
    {
        if (cmd.IsEmpty) return;

        cmd.Execute();
        UndoManager.Instance.RegisterCommand(cmd);
    }
}
