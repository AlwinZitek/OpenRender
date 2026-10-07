using UnityEngine;
using UnityEngine.InputSystem;

public class TextSpawner : MonoBehaviour
{
    public static TextSpawner Instance;
    public Transform iconsContainer;

    [Tooltip("Schriftgroesse neuer Textfelder in Weltunits (beim direkten Einfuegen)")]
    public float defaultFontSize = 1.5f;

    [Header("Platzieren mit Ziehen")]
    [Tooltip("Kleinste Schriftgroesse, auch wenn man kaum zieht")]
    public float minFontSize = 1f;

    [Tooltip("Umrechnung Ziehstrecke -> Schriftgroesse")]
    public float sizePerUnitDragged = 1f;

    [Tooltip("Nach dem Platzieren gleich das Eingabefeld oeffnen")]
    public bool openEditAfterPlacing = true;

    private bool isPlacing = false;
    private bool isDragging = false;
    private Vector3 startPos;
    private TextLabel currentLabel;
    private Camera cam;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    // An den Text-Button haengen: Werkzeug scharf schalten, platziert wird
    // danach mit Klick + Ziehen auf der Canvas.
    public void StartPlacement()
    {
        isPlacing = true;
    }

    public void CancelPlacement()
    {
        isPlacing = false;
        isDragging = false;
        currentLabel = null;
    }

    void Update()
    {
        if (!isPlacing) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && !isDragging)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            startPos = GetMouseWorldPos();
            isDragging = true;

            currentLabel = CreateLabel(startPos, minFontSize, "Text");
        }

        if (isDragging && currentLabel != null)
        {
            // Groesse waehrend des Ziehens laufend anpassen
            float dist = Vector3.Distance(startPos, GetMouseWorldPos());
            float size = Mathf.Max(minFontSize, dist * sizePerUnitDragged);
            currentLabel.SetFontSize(size);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            isPlacing = false;

            if (currentLabel != null)
            {
                UndoManager.Instance.RegisterCommand(
                    new SpawnCommand(currentLabel.gameObject, iconsContainer, null)
                );

                if (openEditAfterPlacing && TextEditUI.Instance != null)
                    TextEditUI.Instance.OpenEdit(currentLabel.labelText, currentLabel.SetText);
            }

            currentLabel = null;
        }
    }

    TextLabel CreateLabel(Vector3 position, float fontSize, string text)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(iconsContainer);
        obj.transform.position = position;

        TextLabel label = obj.AddComponent<TextLabel>();
        label.fontSize = fontSize;   // vor SetText, damit es greift
        label.SetText(text);

        return label;
    }

    // Alter Weg: Text sofort in der Bildschirmmitte einfuegen.
    // Bleibt erhalten, falls irgendwo noch so verkabelt.
    public void SpawnText(string text = "Text")
    {
        Vector3 spawnPos = Camera.main.transform.position;
        spawnPos.z = 0f;

        TextLabel label = CreateLabel(spawnPos, defaultFontSize, text);

        UndoManager.Instance.RegisterCommand(
            new SpawnCommand(label.gameObject, iconsContainer, null)
        );
    }

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
