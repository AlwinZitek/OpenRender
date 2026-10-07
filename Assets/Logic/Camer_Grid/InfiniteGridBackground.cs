using UnityEngine;

// Sorgt für einen "unendlichen" Grid-Hintergrund (wie bei GeoGebra/Canva),
// der immer exakt das aktuelle Kamera-Sichtfeld abdeckt – unabhängig davon,
// wie weit man rein-/rauszoomt oder pannt. Läuft hinter dem Board (CanvasBoard),
// damit das Board weiterhin als eigene "Seite" mit Schatten hervorsticht.
[RequireComponent(typeof(SpriteRenderer))]
public class InfiniteGridBackground : MonoBehaviour
{
    public Camera cam;
    public Material gridMaterial; // dasselbe Material wie am Board

    [Tooltip("Sicherheitsrand, falls die Kamera sich in einem Frame stark bewegt")]
    public float margin = 1.3f;

    [Tooltip("Zeichenreihenfolge. Muss unter allem liegen: Board -10, Schatten -11.")]
    public int sortingOrder = -999;

    private SpriteRenderer sr;

    void Awake()
    {
        if (cam == null) cam = Camera.main;

        // RequireComponent legt den SpriteRenderer bereits an - erst holen,
        // nur zur Not anlegen. AddComponent auf ein Objekt, das schon einen
        // hat, liefert sonst je nach Fall den falschen oder gar keinen.
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWhiteSprite();
        // Aus dem Inspector, nicht fest verdrahtet - ein harter Wert hier
        // wuerde die Einstellung bei jedem Start ueberschreiben.
        sr.sortingOrder = sortingOrder;

        if (gridMaterial != null)
            sr.material = gridMaterial;
    }

    void LateUpdate()
    {
        float height = cam.orthographicSize * 2f * margin;
        float width = height * cam.aspect;

        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        transform.localScale = new Vector3(width, height, 1f);
    }

    Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        // pixelsPerUnit explizit 1, aus demselben Grund wie in CanvasBoard.cs
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    }
}
