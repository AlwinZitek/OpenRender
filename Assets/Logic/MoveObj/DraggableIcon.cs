using UnityEngine;
using UnityEngine.InputSystem;

public class DraggableIcon : MonoBehaviour
{
    public bool snapToGrid = true;
    public float gridSize = 0.5f;

    [Tooltip("Klick-Ablauf in der Console mitschreiben (nur zum Debuggen)")]
    public bool debugClicks = false;

    private bool isDragging = false;
    private Vector3 offset;
    private Vector3 positionBeforeDrag;
    private Camera cam;

    private float lastClickTime = -10f;
    private float doubleClickDelay = 0.4f;

    void Awake() => cam = Camera.main;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, 0)
            );
            worldPos.z = 0f;

            if (HitUtility.IsHandleHit(worldPos))
            {
                if (debugClicks) Debug.Log($"[{name}] Klick von Handle abgefangen");
                return;
            }

            GameObject target = HitUtility.GetPreferredTarget(worldPos);

            if (target != gameObject)
            {
                if (debugClicks && target != null)
                    Debug.Log($"[{name}] Klick ging an: {target.name}");
                return;
            }

            float since = Time.time - lastClickTime;
            bool isDoubleClick = since < doubleClickDelay;

            if (debugClicks)
                Debug.Log($"[{name}] Treffer. Abstand zum letzten Klick: {since:F2}s, Doppelklick: {isDoubleClick}");

            GroupObject grp = GetComponent<GroupObject>();
            if (grp != null)
            {
                grp.HandleFrameClick();
            }
            else if (isDoubleClick)
            {
                TextLabel label = GetComponent<TextLabel>();

                if (debugClicks)
                    Debug.Log($"[{name}] Doppelklick. TextLabel: {(label != null)}, TextEditUI.Instance: {(TextEditUI.Instance != null)}");

                if (label != null && TextEditUI.Instance != null)
                    TextEditUI.Instance.OpenEdit(label.labelText, label.SetText);
            }

            lastClickTime = Time.time;
            isDragging = true;
            positionBeforeDrag = transform.position;
            offset = transform.position - worldPos;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            if (snapToGrid)
                transform.position = SnapToGrid(transform.position);

            if (transform.position != positionBeforeDrag)
            {
                UndoManager.Instance.RegisterCommand(
                    new MoveCommand(gameObject, positionBeforeDrag, transform.position)
                );
            }

            if (GetComponent<GroupObject>() == null)
                UpdateGroupMembership();
        }

        if (isDragging)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 newPos = cam.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, 0)
            );
            newPos.z = 0f;
            newPos += offset;
            if (snapToGrid) newPos = SnapToGrid(newPos);

            GroupObject group = GetComponent<GroupObject>();
            if (group != null)
            {
                Vector3 delta = newPos - transform.position;
                foreach (GameObject member in group.members)
                {
                    if (member != null)
                        member.transform.position += delta;
                }
            }

            transform.position = newPos;
        }
    }

    void UpdateGroupMembership()
    {
        GroupObject currentGroup = null;
        foreach (GroupObject g in GroupObject.AllGroups)
        {
            if (g != null && g.members.Contains(gameObject))
            {
                currentGroup = g;
                break;
            }
        }

        if (currentGroup != null && currentGroup.ContainsPoint(transform.position))
            return;

        if (currentGroup != null)
            currentGroup.RemoveMember(gameObject);

        foreach (GroupObject g in GroupObject.AllGroups)
        {
            if (g == null || g == currentGroup) continue;
            if (g.ContainsPoint(transform.position))
            {
                g.AddMember(gameObject);
                break;
            }
        }
    }

    // Nutzt die zentralen SnapSettings, damit Bewegen, Skalieren und Rotieren
    // dieselben Regeln teilen (inkl. Shift zum kurzzeitigen Umkehren).
    // snapToGrid/gridSize hier bleiben als lokale Abschaltung erhalten.
    Vector3 SnapToGrid(Vector3 pos)
    {
        if (SnapSettings.Instance != null)
            return SnapSettings.SnapPosition(pos);

        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float y = Mathf.Round(pos.y / gridSize) * gridSize;
        return new Vector3(x, y, 0f);
    }
}
