using UnityEngine;
using UnityEngine.InputSystem;

public class ArrowSpawner : MonoBehaviour
{
    public static ArrowSpawner Instance;
    public Transform iconsContainer;

    // Bewusst NICHT serialisiert: als public-Feld hat Unity den Wert in der
    // Szene gespeichert. Stand der Haken im Inspector, war der Pfeil-Modus
    // schon beim Start scharf und der erste Klick auf die Canvas zog einen
    // Pfeil auf, obwohl niemand den Button gedrueckt hatte.
    [System.NonSerialized] public bool isArrowMode = false;

    // Bestimmt, ob das naechste gezeichnete Objekt eine Pfeilspitze bekommt.
    private bool drawArrowHead = true;

    private GameObject currentArrow;
    private bool isDrawing = false;
    private Vector3 startPos;
    private Camera cam;

    void Awake()
    {
        // Zwei aktive Spawner zeichnen beide mit - das ergibt doppelte Pfeile.
        // Seit es StartArrow und StartLine in einer Komponente gibt, reicht einer.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Zweiter ArrowSpawner auf '" + name +
                             "' gefunden und abgeschaltet. Es wird nur einer gebraucht.");
            enabled = false;
            return;
        }

        Instance = this;
        cam = Camera.main;
    }

    void Update()
    {
        if (!isArrowMode) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            startPos = GetMouseWorldPos();
            isDrawing = true;

            currentArrow = new GameObject(drawArrowHead ? "Arrow" : "Line");
            currentArrow.transform.SetParent(iconsContainer);

            var arrow = currentArrow.AddComponent<ArrowObject>();
            arrow.showArrowHead = drawArrowHead;   // vor SetPoints setzen
            arrow.SetArrowHead(drawArrowHead);
            arrow.SetPoints(startPos, startPos);

            currentArrow.AddComponent<DraggableIcon>();

            // SelectionOutline ohne Scale-/Rotate-Handles
            var outline = currentArrow.AddComponent<SelectionOutline>();
            outline.showScaleHandle = false;
            outline.showRotateHandle = false;
        }

        if (isDrawing && Mouse.current.leftButton.isPressed)
        {
            Vector3 currentPos = GetMouseWorldPos();
            currentArrow.GetComponent<ArrowObject>().SetPoints(startPos, currentPos);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDrawing)
        {
            isDrawing = false;

            currentArrow.GetComponent<ArrowObject>().FinalizeCollider();

            UndoManager.Instance.RegisterCommand(
                new SpawnCommand(currentArrow, iconsContainer, null)
            );

            currentArrow = null;
            SetDrawMode(false, drawArrowHead);
        }
    }

    // An den Pfeil-Button haengen
    public void StartArrow() => SetDrawMode(true, true);

    // An den Linien-Button haengen
    public void StartLine() => SetDrawMode(true, false);

    public void SetDrawMode(bool active, bool withHead)
    {
        isArrowMode = active;
        drawArrowHead = withHead;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // Alte Signatur, damit bestehende Verkabelungen weiter funktionieren
    public void SetArrowMode(bool active) => SetDrawMode(active, true);

    Vector3 GetMouseWorldPos()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 0)
        );
        world.z = 0f;
        return world;
    }
}
