using UnityEngine;

public class MoveCommand : ICommand
{
    private GameObject obj;
    private Vector3 oldPosition;
    private Vector3 newPosition;

    public MoveCommand(GameObject obj, Vector3 oldPos, Vector3 newPos)
    {
        this.obj = obj;
        this.oldPosition = oldPos;
        this.newPosition = newPos;
    }

    public void Execute() => obj.transform.position = newPosition;
    public void Undo() => obj.transform.position = oldPosition;
}
