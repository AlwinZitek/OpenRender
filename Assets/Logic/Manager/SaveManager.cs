using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Collections.Generic;
using TMPro;
using SFB;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    public Transform iconsContainer;
    public IconData[] icons; // alle verfügbaren Icons

    void Awake()
    {
        Instance = this;

        var allIcons = new System.Collections.Generic.List<IconData>();
        allIcons.AddRange(Resources.LoadAll<IconData>("Cells/"));
        allIcons.AddRange(Resources.LoadAll<IconData>("Membranes/"));
        allIcons.AddRange(Resources.LoadAll<IconData>("Proteins/"));
        allIcons.AddRange(Resources.LoadAll<IconData>("Nucleics/"));
        icons = allIcons.ToArray();
    }

    void Update()
    {
        // Strg+S = Speichern
        if (Keyboard.current.ctrlKey.isPressed &&
            Keyboard.current.sKey.wasPressedThisFrame)
            Save();

        // Strg+L = Laden
        if (Keyboard.current.ctrlKey.isPressed &&
            Keyboard.current.lKey.wasPressedThisFrame)
            Load();
    }

    public void Save()
    {
        SceneData sceneData = new SceneData();
        Dictionary<GameObject, int> indexMap = new Dictionary<GameObject, int>();

        // 1. Alle normalen Objekte (Icon/Text/Arrow) zuerst speichern,
        //    damit Gruppen später per Index darauf verweisen können.
        foreach (Transform child in iconsContainer)
        {
            // Soft-gelöschte Objekte (DeleteCommand macht SetActive(false) statt Destroy)
            // nicht mitspeichern
            if (!child.gameObject.activeSelf) continue;
            if (child.GetComponent<GroupObject>() != null) continue; // Gruppen erst danach

            ObjectData data = BuildObjectData(child);
            if (data == null) continue;

            indexMap[child.gameObject] = sceneData.objects.Count;
            sceneData.objects.Add(data);
        }

        // 2. Gruppen mit Referenzen auf die Member-Indizes speichern
        foreach (Transform child in iconsContainer)
        {
            GroupObject group = child.GetComponent<GroupObject>();
            if (group == null || !child.gameObject.activeSelf) continue;

            ObjectData data = new ObjectData();
            data.type = "Group";
            data.name = group.groupName;
            data.sortingOrder = ZOrderUtility.GetOrder(child.gameObject);
            data.hasSortingOrder = true;

            foreach (GameObject member in group.members)
            {
                if (member != null && indexMap.TryGetValue(member, out int idx))
                    data.memberIndices.Add(idx);
            }

            sceneData.objects.Add(data);
        }

        string json = JsonUtility.ToJson(sceneData, true);

        var extensions = new[] { new ExtensionFilter("Szene", "json") };
        string path = StandaloneFileBrowser.SaveFilePanel(
            "Szene speichern", "", "scene.json", extensions
        );

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Speichern abgebrochen.");
            return;
        }

        File.WriteAllText(path, json);
        Debug.Log("Gespeichert: " + path);
    }

    ObjectData BuildObjectData(Transform child)
    {
        ObjectData data = new ObjectData();

        data.posX = child.position.x;
        data.posY = child.position.y;
        data.scaleX = child.localScale.x;
        data.scaleY = child.localScale.y;
        data.rotation = child.eulerAngles.z;
        data.sortingOrder = ZOrderUtility.GetOrder(child.gameObject);
        data.hasSortingOrder = true;

        ArrowObject arrow = child.GetComponent<ArrowObject>();
        TextLabel text = child.GetComponent<TextLabel>();
        CustomImageMarker customImg = child.GetComponent<CustomImageMarker>();
        SpriteRenderer sr = child.GetComponent<SpriteRenderer>();

        if (arrow != null)
        {
            data.type = "Arrow";
            data.name = "Arrow";
            data.startX = arrow.startPoint.x;
            data.startY = arrow.startPoint.y;
            data.endX = arrow.endPoint.x;
            data.endY = arrow.endPoint.y;
            data.colorR = arrow.arrowColor.r;
            data.colorG = arrow.arrowColor.g;
            data.colorB = arrow.arrowColor.b;
            data.colorA = arrow.arrowColor.a;
            data.hideArrowHead = !arrow.showArrowHead;
        }
        else if (text != null)
        {
            data.type = "Text";
            data.name = text.labelText;
            var tmp = child.GetComponent<TextMeshPro>();
            if (tmp.font != null) data.fontName = tmp.font.name;
            data.colorR = tmp.color.r;
            data.colorG = tmp.color.g;
            data.colorB = tmp.color.b;
            data.colorA = tmp.color.a;
        }
        else if (customImg != null)
        {
            // Muss vor der generischen Icon-Prüfung kommen, da importierte
            // Bilder ebenfalls einen SpriteRenderer haben, aber kein IconData-Asset
            data.type = "CustomImage";
            data.name = customImg.sourceFileName;
            data.colorR = sr.color.r;
            data.colorG = sr.color.g;
            data.colorB = sr.color.b;
            data.colorA = sr.color.a;
        }
        else if (sr != null)
        {
            data.type = "Icon";
            data.name = child.name;
            data.colorR = sr.color.r;
            data.colorG = sr.color.g;
            data.colorB = sr.color.b;
            data.colorA = sr.color.a;
        }
        else
        {
            return null; // unbekannter Typ, z.B. Handles o.ä.
        }

        return data;
    }

    public void Load()
    {
        var extensions = new[] { new ExtensionFilter("Szene", "json") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Szene laden", "", extensions, false
        );

        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            Debug.Log("Laden abgebrochen.");
            return;
        }

        string path = paths[0];
        if (!File.Exists(path))
        {
            Debug.Log("Datei nicht gefunden: " + path);
            return;
        }

        // Selektion sauber auflösen, bevor wir die Objekte zerstören
        SelectionManager.Instance.DeselectAll();

        // Alte Objekte löschen — inkl. ihrer Scale-/Rotate-/Endpoint-Handles,
        // die als eigenständige Root-Objekte NICHT automatisch mitgelöscht werden
        // (SetParent(null) in SelectionOutline/ArrowObject). Ohne das bleiben
        // Zombie-Handles mit toter target-Referenz in der Szene zurück und
        // blockieren danach Klicks auf die neu geladenen Objekte.
        foreach (Transform child in iconsContainer)
        {
            child.GetComponent<SelectionOutline>()?.DestroyAll();
            child.GetComponent<ArrowObject>()?.DestroyHandles();
            Destroy(child.gameObject);
        }

        // Undo-Stack leeren – er würde sonst auf gleich zerstörte Objekte zeigen
        UndoManager.Instance.ClearStacks();

        string json = File.ReadAllText(path);
        SceneData sceneData = JsonUtility.FromJson<SceneData>(json);

        GameObject[] created = new GameObject[sceneData.objects.Count];

        // 1. Alle normalen Objekte erzeugen
        for (int i = 0; i < sceneData.objects.Count; i++)
        {
            ObjectData data = sceneData.objects[i];
            if (data.type == "Group") continue;
            created[i] = CreateObjectFromData(data);
        }

        // 2. Gruppen anhand der gespeicherten Indizes rekonstruieren
        for (int i = 0; i < sceneData.objects.Count; i++)
        {
            ObjectData data = sceneData.objects[i];
            if (data.type != "Group") continue;

            List<GameObject> validMembers = new List<GameObject>();
            foreach (int idx in data.memberIndices)
            {
                if (idx >= 0 && idx < created.Length && created[idx] != null)
                    validMembers.Add(created[idx]);
            }

            // Gruppen mit < 2 gültigen Mitgliedern lösen sich ohnehin auf
            if (validMembers.Count < 2) continue;

            GameObject groupObj = new GameObject("Group");
            groupObj.transform.SetParent(iconsContainer);
            GroupObject group = groupObj.AddComponent<GroupObject>();
            group.SetTitle(data.name);

            foreach (GameObject member in validMembers)
                group.AddMember(member);

            group.UpdateBounds();

            // Gruppen-Rahmen: eigene Reihenfolge, ohne die Mitglieder
            // mitzuziehen - die haben ihre eigene schon aus dem Speicherstand.
            if (data.hasSortingOrder)
            {
                var lr = groupObj.GetComponent<LineRenderer>();
                if (lr != null) lr.sortingOrder = data.sortingOrder;
            }
        }

        Debug.Log("Geladen!");
    }

    GameObject CreateObjectFromData(ObjectData data)
    {
        GameObject obj = BuildObject(data);

        // Zeichenreihenfolge erst nach dem Aufbau setzen: die Typen setzen
        // in ihrem Konstruktor eigene Werte, die wir sonst ueberschreiben wuerden.
        if (obj != null && data.hasSortingOrder)
            ZOrderUtility.SetOrder(obj, data.sortingOrder);

        return obj;
    }

    GameObject BuildObject(ObjectData data)
    {
        Color color = new Color(data.colorR, data.colorG, data.colorB, data.colorA);

        if (data.type == "Arrow")
        {
            GameObject obj = new GameObject("Arrow");
            obj.transform.SetParent(iconsContainer);
            obj.transform.position = new Vector3(data.posX, data.posY, 0);
            obj.transform.localScale = new Vector3(data.scaleX, data.scaleY, 1);

            ArrowObject arrow = obj.AddComponent<ArrowObject>();
            arrow.SetArrowHead(!data.hideArrowHead);
            obj.AddComponent<DraggableIcon>();
            var outline = obj.AddComponent<SelectionOutline>();
            outline.showScaleHandle = false;
            outline.showRotateHandle = false;

            Vector3 start = new Vector3(data.startX, data.startY, 0);
            Vector3 end = new Vector3(data.endX, data.endY, 0);
            arrow.SetPoints(start, end);
            arrow.SetColor(color);

            return obj;
        }
        else if (data.type == "Text")
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(iconsContainer);
            obj.transform.position = new Vector3(data.posX, data.posY, 0);
            obj.transform.localScale = new Vector3(data.scaleX, data.scaleY, 1);
            obj.transform.eulerAngles = new Vector3(0, 0, data.rotation);

            TextLabel label = obj.AddComponent<TextLabel>();
            label.SetText(data.name);
            var tmp = obj.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                tmp.color = color;

                if (!string.IsNullOrEmpty(data.fontName) && FontManager.Instance != null)
                {
                    var font = FontManager.Instance.FindFont(data.fontName);
                    if (font != null) tmp.font = font;
                }
            }

            return obj;
        }
        else if (data.type == "Icon")
        {
            IconData iconData = FindIcon(data.name);
            if (iconData == null) return null;

            GameObject obj = new GameObject(data.name);
            obj.transform.SetParent(iconsContainer);
            obj.transform.position = new Vector3(data.posX, data.posY, 0);
            obj.transform.localScale = new Vector3(data.scaleX, data.scaleY, 1);
            obj.transform.eulerAngles = new Vector3(0, 0, data.rotation);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = iconData.sprite;
            sr.color = color;
            sr.sortingOrder = 1;

            obj.AddComponent<DraggableIcon>();
            obj.AddComponent<BoxCollider2D>();
            obj.AddComponent<SelectionOutline>();

            return obj;
        }
        else if (data.type == "CustomImage")
        {
            GameObject obj = CustomImageImporter.Instance.SpawnImportedSprite(
                data.name, registerUndo: false
            );
            if (obj == null) return null;

            obj.transform.position = new Vector3(data.posX, data.posY, 0);
            obj.transform.localScale = new Vector3(data.scaleX, data.scaleY, 1);
            obj.transform.eulerAngles = new Vector3(0, 0, data.rotation);

            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;

            return obj;
        }

        return null;
    }

    IconData FindIcon(string name)
    {
        foreach (IconData icon in icons)
            if (icon.iconName == name) return icon;
        return null;
    }
}
