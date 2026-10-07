using UnityEngine;
using UnityEngine.UI;

public class ToolbarManager : MonoBehaviour
{
    public Button undoButton;
    public Button redoButton;
    public Button deleteButton;
    public Button exportButton;
    public Button textButton;
    public Button arrowButton;
    public Button saveButton;
    public Button loadButton;


    void Start()
    {
        saveButton.onClick.AddListener(() => SaveManager.Instance.Save());
        loadButton.onClick.AddListener(() => SaveManager.Instance.Load());

        arrowButton.onClick.AddListener(() =>
        ArrowSpawner.Instance.SetArrowMode(true)
        );

        textButton.onClick.RemoveAllListeners();
        textButton.onClick.AddListener(() =>
    TextSpawner.Instance.StartPlacement()

        );

        undoButton.onClick.AddListener(() =>
            UndoManager.Instance.PerformUndo()
        );

        redoButton.onClick.AddListener(() =>
            UndoManager.Instance.PerformRedo()
        );

        deleteButton.onClick.AddListener(() =>
        {
            GameObject selected = SelectionManager.Instance.selectedObject;
            if (selected != null)
            {
                UndoManager.Instance.ExecuteCommand(
                    new DeleteCommand(selected, selected.transform.parent)
                );
                SelectionManager.Instance.Deselect();
            }
        });

        exportButton.onClick.AddListener(() =>
            Object.FindFirstObjectByType<ExportManager>().Export()
        );
    }
}
