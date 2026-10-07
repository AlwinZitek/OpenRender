using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UndoManager : MonoBehaviour
{
    public static UndoManager Instance;

    private Stack<ICommand> undoStack = new Stack<ICommand>();
    private Stack<ICommand> redoStack = new Stack<ICommand>();

    void Awake() => Instance = this;

    private bool zWasPressed = false;
    private bool yWasPressed = false;

    public void PerformUndo() => Undo();
    public void PerformRedo() => Redo();

    // Wird beim Laden einer Szene aufgerufen, damit der Stack nicht
    // auf inzwischen zerstörte Objekte zeigt.
    public void ClearStacks()
    {
        undoStack.Clear();
        redoStack.Clear();
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.Z)
        {
            Debug.Log("Undo aufgerufen. Stack größe: " + undoStack.Count);
            Undo();
            e.Use();
        }
        if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.Y)
        {
            Debug.Log("Redo aufgerufen. Stack größe: " + redoStack.Count);
            Redo();
            e.Use();
        }
    }

    public void ExecuteCommand(ICommand command)
    {
        command.Execute();
        undoStack.Push(command);
        redoStack.Clear(); // neue Aktion löscht Redo-History
    }

    public void RegisterCommand(ICommand command)
    {
        undoStack.Push(command);
        redoStack.Clear();
        Debug.Log("Command registriert. Stack größe: " + undoStack.Count);
    }

    void Undo()
    {
        Debug.Log("Undo aufgerufen. Stack größe: " + undoStack.Count);
        if (undoStack.Count == 0) return;
        ICommand command = undoStack.Pop();
        command.Undo();
        redoStack.Push(command);
    }

    void Redo()
    {
        if (redoStack.Count == 0) return;
        ICommand command = redoStack.Pop();
        command.Execute();
        undoStack.Push(command);
    }
}
