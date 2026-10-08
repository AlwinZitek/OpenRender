using UnityEngine;

public class CanvasBoard : MonoBehaviour
{
    public Vector2 canvasSize = new Vector2(48f, 34f);
    public Material gridMaterial; // ← GridMaterial hier reinziehen im Inspector

    [Header("Schatten")]
    public Sprite shadowSprite;      // board_shadow.png hier reinziehen
    public float shadowPadding = 1.5f; // wie viel größer der Schatten als das Board ist
    public float shadowAlpha = 0.35f;

    void Start()
    {

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("CanvasBoard: SpriteRenderer konnte nicht hinzugefügt werden – " +
                           "vermutlich sitzt ein MeshRenderer/MeshFilter auf dem Objekt.", this);
            return;
        }
        sr.sprite = CreateWhiteSprite();
            sr.sortingOrder = -10;
            transform.localScale = new Vector3(canvasSize.x, canvasSize.y, 1);
            transform.position = Vector3.zero;
        

        // Grid Material anwenden
        if (gridMaterial != null)
            sr.material = gridMaterial;

        SpawnShadow();
    }

    // Damit der ExportManager den Schatten ausblenden kann - er gehoert
    // zur Darstellung auf dem Bildschirm, nicht ins exportierte Bild.
    public GameObject ShadowObject { get; private set; }

    void SpawnShadow()
    {
        if (shadowSprite == null) return;

        GameObject shadowObj = new GameObject("BoardShadow");
        ShadowObject = shadowObj;
        shadowObj.transform.SetParent(transform.parent); // gleiche Ebene wie das Board
        shadowObj.transform.position = new Vector3(0, 0, 0.01f); // minimal hinter dem Board
        shadowObj.transform.localScale = Vector3.one; // WICHTIG: im Sliced-Modus nicht über Scale skalieren

        SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sprite = shadowSprite;
        shadowSr.drawMode = SpriteDrawMode.Sliced; // aktiviert 9-Slice, sonst wird nur gestretcht
        shadowSr.sortingOrder = -11; // unter dem Board (-10)
        shadowSr.color = new Color(0f, 0f, 0f, shadowAlpha);

        // Im Sliced-Modus bestimmt "size" (Weltkoordinaten) die Ausdehnung,
        // nicht die Transform-Scale.
        shadowSr.size = new Vector2(
            canvasSize.x + shadowPadding * 2f,
            canvasSize.y + shadowPadding * 2f
        );
    }

    Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        // WICHTIG: pixelsPerUnit explizit auf 1 setzen. Der Default (100)
        // sorgte dafür, dass das 1x1-Pixel-Sprite bei Scale 1 nur 0.01x0.01
        // Weltunits groß war, statt 1x1 wie beabsichtigt — dadurch wurde das
        // Board immer 100x kleiner als canvasSize tatsächlich vorgab.
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    }
}
