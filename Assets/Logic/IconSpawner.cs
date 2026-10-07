using UnityEngine;

public class IconSpawner : MonoBehaviour
{
    public static IconSpawner Instance;

    public Transform iconsContainer;   // leeres GameObject "Icons_Container"

    void Awake()
    {
        Instance = this;
    }

    public void SpawnIcon(IconData data)
    {
        GameObject obj = new GameObject(data.iconName);
        obj.transform.SetParent(iconsContainer);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = data.sprite;
        sr.sortingOrder = 1;

        obj.AddComponent<DraggableIcon>();
        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<SelectionOutline>();

        // Importierte Bilder markieren, damit SaveManager sie als "CustomImage"
        // speichert und ueber die Bibliothek wiederherstellen kann.
        if (data.IsCustom)
            obj.AddComponent<CustomImageMarker>().sourceFileName = data.customFileName;

        Vector3 spawnPos = Camera.main.transform.position;
        spawnPos.z = 0f;
        obj.transform.position = spawnPos;
        obj.transform.localScale = Vector3.one * 2f;

        UndoManager.Instance.RegisterCommand(
            new SpawnCommand(obj, iconsContainer, data)
        );
    }
}
