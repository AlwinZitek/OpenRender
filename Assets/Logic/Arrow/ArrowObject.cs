using UnityEngine;

public class ArrowObject : MonoBehaviour
{
    private LineRenderer lr;
    private GameObject arrowHead;
    private GameObject startHandleObj;
    private GameObject endHandleObj;

    public Vector3 startPoint;
    public Vector3 endPoint;
    public float lineWidth = 0.1f;
    public Color arrowColor = Color.black;

    [Header("Pfeilspitze")]
    [Tooltip("Aus = einfache Linie ohne Kopf")]
    public bool showArrowHead = true;

    [Tooltip("Größe der Spitze relativ zur Linienbreite")]
    public float arrowHeadScale = 8f;

    [Header("Endpunkt-Handles")]
    public float handleSize = 0.35f;

    // Die Linie wird in Weltkoordinaten gezeichnet, nicht relativ zum
    // Transform. Ohne Nachfuehrung bleibt sie liegen, wenn DraggableIcon das
    // Objekt verschiebt - nur der Pfeilkopf (Kind-Objekt) wandert mit.
    private Vector3 lastPos;

    [Header("Klick-Erkennung")]
    [Tooltip("Mindestbreite der Trefferzone in Weltunits, unabhaengig von lineWidth")]
    public float minClickWidth = 0.6f;

    void Awake()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.sortingOrder = 5;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = arrowColor;
        lr.endColor = arrowColor;

        arrowHead = new GameObject("ArrowHead");
        arrowHead.transform.SetParent(transform);
        var sr = arrowHead.AddComponent<SpriteRenderer>();
        sr.sprite = CreateTriangleSprite();
        sr.color = arrowColor;
        sr.sortingOrder = 6;
        arrowHead.transform.localScale = Vector3.one * (lineWidth * arrowHeadScale);
        arrowHead.SetActive(showArrowHead);

        lastPos = transform.position;
    }

    void LateUpdate()
    {
        Vector3 delta = transform.position - lastPos;
        if (delta == Vector3.zero) return;

        // Objekt wurde verschoben -> Linie mitnehmen
        SetPoints(startPoint + delta, endPoint + delta);
    }

    public void SetPoints(Vector3 start, Vector3 end)
    {
        lastPos = transform.position;

        startPoint = start;
        endPoint = end;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        arrowHead.transform.position = end;
        // Spitze mitskalieren, falls lineWidth zur Laufzeit geändert wurde
        arrowHead.transform.localScale = Vector3.one * (lineWidth * arrowHeadScale);

        Vector3 dir = (end - start).normalized;
        if (dir == Vector3.zero) return;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        arrowHead.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Handles mitbewegen, falls sie gerade sichtbar sind
        if (startHandleObj != null) startHandleObj.transform.position = startPoint;
        if (endHandleObj != null) endHandleObj.transform.position = endPoint;

        FinalizeCollider();
    }

    // Zwischen Pfeil und einfacher Linie umschalten.
    public void SetArrowHead(bool visible)
    {
        showArrowHead = visible;
        if (arrowHead != null) arrowHead.SetActive(visible);
    }

    public void SetColor(Color color)
    {
        arrowColor = color;
        lr.startColor = color;
        lr.endColor = color;
        var sr = arrowHead.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    public void SetLineWidth(float width)
    {
        lineWidth = width;
        lr.startWidth = width;
        lr.endWidth = width;
        arrowHead.transform.localScale = Vector3.one * (width * arrowHeadScale);
    }

    public void FinalizeCollider()
    {
        PolygonCollider2D old = GetComponent<PolygonCollider2D>();
        if (old != null) Destroy(old);

        Vector3 dir = (endPoint - startPoint).normalized;
        if (dir == Vector3.zero) return;

        // Collider-Breite an die Linienbreite koppeln, aber mit Mindestbreite,
        // damit auch sehr duenne Pfeile gut anklickbar bleiben.
        float halfWidth = Mathf.Max(lineWidth * 1.5f, minClickWidth * 0.5f);
        Vector3 perp = new Vector3(-dir.y, dir.x, 0) * halfWidth;

        // Enden etwas verlaengern, damit auch die Spitze mit abgedeckt ist
        float overshoot = Mathf.Max(lineWidth * arrowHeadScale * 0.5f, 0.1f);

        PolygonCollider2D col = gameObject.AddComponent<PolygonCollider2D>();

        // WICHTIG: PolygonCollider2D-Pfade sind LOKAL. startPoint/endPoint sind
        // Weltkoordinaten (LineRenderer laeuft mit useWorldSpace). Solange der
        // Pfeil auf Position 0 lag, war beides dasselbe - sobald er verschoben
        // wird, landet der Collider sonst doppelt versetzt und der sichtbare
        // Pfeil ist nicht mehr anklickbar.
        col.SetPath(0, new Vector2[]
        {
            ToLocal(startPoint - (Vector3)perp - (Vector3)(dir * overshoot)),
            ToLocal(startPoint + (Vector3)perp - (Vector3)(dir * overshoot)),
            ToLocal(endPoint   + (Vector3)perp + (Vector3)(dir * overshoot)),
            ToLocal(endPoint   - (Vector3)perp + (Vector3)(dir * overshoot))
        });
    }

    Vector2 ToLocal(Vector3 worldPoint)
    {
        return transform.InverseTransformPoint(worldPoint);
    }

    public void ShowHandles()
    {
        if (startHandleObj == null) CreateHandles();
        startHandleObj.transform.position = startPoint;
        endHandleObj.transform.position = endPoint;
        startHandleObj.SetActive(true);
        endHandleObj.SetActive(true);
    }

    public void HideHandles()
    {
        if (startHandleObj != null) startHandleObj.SetActive(false);
        if (endHandleObj != null) endHandleObj.SetActive(false);
    }

    void CreateHandles()
    {
        startHandleObj = CreateHandle("StartHandle", new Color(0.2f, 0.45f, 1f), true);
        endHandleObj = CreateHandle("EndHandle", new Color(1f, 0.3f, 0.3f), false);
    }

    // 2D-Sprite-Handle statt PrimitiveType.Sphere: 3D-Primitives verwenden
    // URP-Lit-Materialien, deren Farbe sich über material.color im 2D-Renderer
    // nicht zuverlässig setzen lässt (Handles blieben deshalb weiß).
    GameObject CreateHandle(string name, Color color, bool isStart)
    {
        GameObject handle = new GameObject(name);

        var sr = handle.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = color;
        sr.sortingOrder = 15; // über der Linie (5) und der Spitze (6)

        handle.transform.localScale = Vector3.one * handleSize;

        var col = handle.AddComponent<CircleCollider2D>();
        col.radius = 0.5f; // passend zum Sprite-Radius

        var ep = handle.AddComponent<EndpointHandle>();
        ep.arrow = this;
        ep.isStartPoint = isStart;

        return handle;
    }

    // Die Endpunkt-Handles sind eigenstaendige Root-Objekte, kein Kind des
    // Pfeils - sie ueberleben ein Destroy des Pfeils sonst als sichtbare
    // Leichen. Hier aufzuraeumen deckt alle Wege ab: Undo eines Spawns,
    // Laden einer Szene, direktes Zerstoeren.
    void OnDestroy()
    {
        DestroyHandles();
    }

    public void DestroyHandles()
    {
        if (startHandleObj != null) Destroy(startHandleObj);
        if (endHandleObj != null) Destroy(endHandleObj);
    }

    Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float r = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                // weicher Rand für saubere Kanten
                float alpha = Mathf.Clamp01(r - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    Sprite CreateTriangleSprite()
    {
        Texture2D tex = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float fx = (x - 32) / 32f;
                float fy = (y - 32) / 32f;
                pixels[y * 64 + x] = (fy > -1f + Mathf.Abs(fx) * 2f)
                    ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f, 64);
    }
}
