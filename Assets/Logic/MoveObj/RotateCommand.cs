using UnityEngine;

public class RotateCommand : ICommand
{
    private GameObject obj;
    private float oldRotation;
    private float newRotation;

    public RotateCommand(GameObject obj, float oldRot, float newRot)
    {
        this.obj = obj;
        this.oldRotation = oldRot;
        this.newRotation = newRot;
    }

    public void Execute() => obj.transform.eulerAngles = new Vector3(0, 0, newRotation);
    public void Undo() => obj.transform.eulerAngles = new Vector3(0, 0, oldRotation);

}
