using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GroupObject : MonoBehaviour
{
    // Zentrales Register aller Gruppen (für Membership-Checks von außen)
    public static List<GroupObject> AllGroups = new List<GroupObject>();

    public List<GameObject> members = new List<GameObject>();
    public string groupName = "Group";
    public float padding = 0.5f;

    private LineRenderer lr;
    private TextMeshPro titleTmp;
    private Color normalColor = new Color(0.5f, 0f, 0.8f, 1f);   // lila
    private Color selectedColor = new Color(0.8f, 0f, 1f, 1f);   // helles lila

    private float lastTitleClickTime = -10f;
    private const float doubleClickDelay = 0.3f;

    void Awake()
    {
        AllGroups.Add(this);

        // Rahmen
        lr = gameObject.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.positionCount = 4;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.useWorldSpace = true;
        lr.sortingOrder = 1; // unterhalb der Members
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = normalColor;
        lr.endColor = normalColor;

        // Titel-Label
        GameObject titleObj = new GameObject("GroupTitle");
        titleObj.transform.SetParent(transform);
        titleTmp = titleObj.AddComponent<TextMeshPro>();
        titleTmp.text = groupName;
        titleTmp.fontSize = 1.2f;
        titleTmp.color = normalColor;
        titleTmp.alignment = TextAlignmentOptions.BottomLeft;
        titleTmp.sortingOrder = 21;

        // Ganze Gruppe draggable (bewegt bei Drag alle Members mit, siehe DraggableIcon)
        gameObject.AddComponent<DraggableIcon>();

        // Collider für Klicks auf Rahmen/Hintergrund
        gameObject.AddComponent<BoxCollider2D>();
    }

    void OnDestroy()
    {
        AllGroups.Remove(this);
    }

    public void AddMember(GameObject obj)
    {
        if (!members.Contains(obj))
            members.Add(obj);
        UpdateBounds();
    }

    public void RemoveMember(GameObject obj)
    {
        if (!members.Remove(obj)) return;

        // Wie im Shader Graph: Gruppe mit < 2 Mitgliedern löst sich auf
        if (members.Count < 2)
            Dissolve();
        else
            UpdateBounds();
    }

    public void Dissolve()
    {
        members.Clear();
        Destroy(gameObject);
    }

    public bool ContainsPoint(Vector3 worldPos)
    {
        if (members.Count == 0) return false;
        GetBoundsRect(out float minX, out float minY, out float maxX, out float maxY);
        return worldPos.x >= minX && worldPos.x <= maxX &&
               worldPos.y >= minY && worldPos.y <= maxY;
    }

    void GetBoundsRect(out float minX, out float minY, out float maxX, out float maxY)
    {
        minX = float.MaxValue; minY = float.MaxValue;
        maxX = float.MinValue; maxY = float.MinValue;

        foreach (GameObject member in members)
        {
            if (member == null) continue;
            Vector3 pos = member.transform.position;
            SpriteRenderer sr = member.GetComponent<SpriteRenderer>();
            TextMeshPro tmp = member.GetComponent<TextMeshPro>();

            float hw = 1f, hh = 1f;
            if (sr != null)
            {
                hw = sr.bounds.size.x / 2f;
                hh = sr.bounds.size.y / 2f;
            }
            else if (tmp != null)
            {
                hw = tmp.preferredWidth * member.transform.localScale.x / 2f;
                hh = tmp.preferredHeight * member.transform.localScale.y / 2f;
            }

            minX = Mathf.Min(minX, pos.x - hw);
            minY = Mathf.Min(minY, pos.y - hh);
            maxX = Mathf.Max(maxX, pos.x + hw);
            maxY = Mathf.Max(maxY, pos.y + hh);
        }

        minX -= padding; minY -= padding;
        maxX += padding; maxY += padding;
    }

    public void UpdateBounds()
    {
        if (members.Count == 0) return;

        GetBoundsRect(out float minX, out float minY, out float maxX, out float maxY);

        lr.SetPosition(0, new Vector3(minX, minY, 0));
        lr.SetPosition(1, new Vector3(maxX, minY, 0));
        lr.SetPosition(2, new Vector3(maxX, maxY, 0));
        lr.SetPosition(3, new Vector3(minX, maxY, 0));

        var col = GetComponent<BoxCollider2D>();
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
        col.offset = center - transform.position;
        col.size = new Vector2(maxX - minX, maxY - minY);

        titleTmp.transform.position = new Vector3(minX, maxY + 0.15f, 0);
    }

    public void SetTitle(string newName)
    {
        groupName = newName;
        titleTmp.text = newName;
    }

    // Wird von DraggableIcon aufgerufen, wenn auf dieses GroupObject geklickt wurde
    public void HandleFrameClick()
    {
        if (Time.time - lastTitleClickTime < doubleClickDelay)
        {
            if (TextEditUI.Instance != null)
                TextEditUI.Instance.OpenEdit(groupName, SetTitle);
        }
        lastTitleClickTime = Time.time;
    }

    public void SetSelected(bool selected)
    {
        Color c = selected ? selectedColor : normalColor;
        lr.startColor = c;
        lr.endColor = c;
        titleTmp.color = c;
    }

    void Update()
    {
        // Bounds jeden Frame live nachführen – kein Edit-Modus nötig
        UpdateBounds();
    }
}
