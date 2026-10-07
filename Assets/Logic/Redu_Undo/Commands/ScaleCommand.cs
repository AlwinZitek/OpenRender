using UnityEngine;

public class ScaleCommand : ICommand
{
    private GameObject obj;
    private Vector3 oldScale;
    private Vector3 newScale;

    public ScaleCommand(GameObject obj, Vector3 oldScale, Vector3 newScale)
    {
        this.obj = obj;
        this.oldScale = oldScale;
        this.newScale = newScale;
    }

    public void Execute() => obj.transform.localScale = newScale;
    public void Undo() => obj.transform.localScale = oldScale;
}

