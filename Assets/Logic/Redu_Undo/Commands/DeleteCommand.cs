using UnityEngine;

public class DeleteCommand : ICommand
{
    private GameObject deletedObj;
    private Transform container;
    private Vector3 position;
    private Vector3 scale;

    public DeleteCommand(GameObject obj, Transform container)
    {
        this.deletedObj = obj;
        this.container = container;
        this.position = obj.transform.position;
        this.scale = obj.transform.localScale;
    }

    public void Execute()
    {
        if (deletedObj == null) return;

        // Verstecken statt zerstören
        deletedObj.GetComponent<SelectionOutline>()?.Hide();

        // Die Endpunkt-Handles eines Pfeils sind eigenstaendige Root-Objekte,
        // kein Kind des Pfeils. SetActive(false) erreicht sie deshalb nicht -
        // ohne das bleiben die roten/blauen Anker nach dem Loeschen sichtbar.
        deletedObj.GetComponent<ArrowObject>()?.HideHandles();

        deletedObj.SetActive(false);
    }

    public void Undo()
    {
        if (deletedObj == null) return;

        // Wieder anzeigen
        deletedObj.SetActive(true);
        deletedObj.transform.position = position;
        deletedObj.transform.localScale = scale;
    }
}
