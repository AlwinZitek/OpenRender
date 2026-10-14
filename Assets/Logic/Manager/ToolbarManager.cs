using UnityEngine;
using UnityEngine.UI;

public class ToolbarManager : MonoBehaviour
{
    [Header("Bearbeiten")]
    public Button undoButton;
    public Button redoButton;
    public Button deleteButton;

    [Header("Werkzeuge")]
    public Button textButton;
    public Button arrowButton;
    public Button lineButton;

    [Header("Datei")]
    public Button saveButton;
    public Button loadButton;
    public Button exportButton;

    [Header("Ebenen")]
    public Button forwardButton;
    public Button backwardButton;
    public Button toFrontButton;
    public Button toBackButton;

    [Header("Gruppen")]
    public Button groupButton;
    public Button ungroupButton;

    [Header("Referenzen")]
    [Tooltip("Leer lassen = wird beim Start einmal gesucht")]
    public ExportManager exportManager;

    void Start()
    {
        if (exportManager == null)
            exportManager = Object.FindFirstObjectByType<ExportManager>();

        // Bearbeiten
        Wire(undoButton, () => UndoManager.Instance.PerformUndo());
        Wire(redoButton, () => UndoManager.Instance.PerformRedo());
        Wire(deleteButton, DeleteSelected);

        // Werkzeuge
        Wire(textButton, () => TextSpawner.Instance.SpawnText());
        Wire(arrowButton, () => ArrowSpawner.Instance.StartArrow());
        Wire(lineButton, () => ArrowSpawner.Instance.StartLine());

        // Datei
        Wire(saveButton, () => SaveManager.Instance.Save());
        Wire(loadButton, () => SaveManager.Instance.Load());
        Wire(exportButton, Export);

        // Ebenen
        Wire(forwardButton, () => ZOrderController.Instance.MoveForward());
        Wire(backwardButton, () => ZOrderController.Instance.MoveBackward());
        Wire(toFrontButton, () => ZOrderController.Instance.BringToFront());
        Wire(toBackButton, () => ZOrderController.Instance.SendToBack());

        // Gruppen
        Wire(groupButton, () => SelectionManager.Instance.GroupSelectedPublic());
        Wire(ungroupButton, () => SelectionManager.Instance.UngroupSelectedPublic());
    }

    // Raeumt vorher auf, damit nichts doppelt feuert: weder ein zweiter
    // Aufruf dieser Methode noch ein Eintrag, der im Inspector unter
    // On Click () stehengeblieben ist. Nicht zugewiesene Buttons werden
    // uebersprungen statt eine NullReferenceException zu werfen.
    void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    void DeleteSelected()
    {
        var selection = SelectionManager.Instance.selectedObjects;
        if (selection.Count == 0) return;

        // Kopie, weil DeleteCommand die Auswahl veraendert
        foreach (GameObject obj in new System.Collections.Generic.List<GameObject>(selection))
        {
            if (obj == null) continue;
            UndoManager.Instance.ExecuteCommand(
                new DeleteCommand(obj, obj.transform.parent)
            );
        }

        SelectionManager.Instance.DeselectAll();
    }

    void Export()
    {
        if (exportManager == null)
        {
            Debug.LogError("Kein ExportManager in der Szene gefunden.");
            return;
        }

        exportManager.Export();
    }
}
