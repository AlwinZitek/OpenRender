using UnityEngine;

public class SpawnCommand : ICommand
{
    private GameObject spawnedObj;
    private Transform container;
    private IconData data;

    public SpawnCommand(GameObject obj, Transform container, IconData data)
    {
        this.spawnedObj = obj;
        this.container = container;
        this.data = data;
    }

    public void Execute() { } // Objekt wurde bereits gespawnt

    public void Undo()
    {
        if (spawnedObj != null)
        {
            spawnedObj.GetComponent<SelectionOutline>()?.DestroyAll();
            GameObject.Destroy(spawnedObj);
        }
    }
}
