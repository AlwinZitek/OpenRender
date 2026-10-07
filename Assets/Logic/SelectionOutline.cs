using TMPro;
using UnityEngine;

public class SelectionOutline : MonoBehaviour
{
    private GameObject outlineObj;
    private LineRenderer lr;
    private GameObject scaleHandle;
    private GameObject rotateHandle;

    public Color outlineColor = new Color(0.3f, 0.42f, 1f);
    public float outlineWidth = 0.1f;
    private float outlinePadding = 0.1f;
    public bool showScaleHandle = true;
    public bool showRotateHandle = true;
    public float handleSize = 0.4f;

    private bool isVisible = false;

    void Awake()
    {
        outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(transform);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;

        lr = outlineObj.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.positionCount = 4;
        lr.startWidth = outlineWidth;
        lr.endWidth = outlineWidth;
        lr.useWorldSpace = true;
        lr.sortingOrder = 450;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = outlineColor;
        lr.endColor = outlineColor;

        if (showScaleHandle)
        {
            scaleHandle = CreateHandle("ScaleHandle", new Color(0.3f, 0.42f, 1f), square: true);
            scaleHandle.AddComponent<ScaleHandle>().target = transform;
        }

        if (showRotateHandle)
        {
            rotateHandle = CreateHandle("RotateHandle", new Color(0.2f, 0.8f, 0.4f), square: false);
            rotateHandle.AddComponent<RotateHandle>().target = transform;
        }

        outlineObj.SetActive(false);
        if (scaleHandle != null) scaleHandle.SetActive(false);
        if (rotateHandle != null) rotateHandle.SetActive(false);
    }

    // 2D-Sprite-Handles statt PrimitiveType.Quad/Sphere: 3D-Primitives nutzen
    // URP-Lit-Materialien, deren Farbe sich im 2D-Renderer über material.color
    // nicht zuverlässig setzen lässt (Handles blieben deshalb weiß).
    GameObject CreateHandle(string name, Color color, bool square)
    {
        GameObject handle = new GameObject(name);

        var sr = handle.AddComponent<SpriteRenderer>();
        sr.sprite = square ? CreateSquareSprite() : CreateCircleSprite();
        sr.color = color;
        sr.sortingOrder = 500;

        handle.transform.localScale = Vector3.one * handleSize;

        if (square)
        {
            var col = handle.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
        }
        else
        {
            var col = handle.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
        }

        return handle;
    }

    void Update()
    {
        if (isVisible)
            UpdateOutline();
    }

    public void Show()
    {
        isVisible = true;
        outlineObj.SetActive(true);
        if (showScaleHandle && scaleHandle != null) scaleHandle.SetActive(true);
        if (showRotateHandle && rotateHandle != null) rotateHandle.SetActive(true);
        UpdateOutline();
    }

    public void Hide()
    {
        isVisible = false;
        if (outlineObj != null) outlineObj.SetActive(false);
        if (scaleHandle != null) scaleHandle.SetActive(false);
        if (rotateHandle != null) rotateHandle.SetActive(false);
    }

    void UpdateOutline()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        TextMeshPro tmp = GetComponent<TextMeshPro>();

        float w, h;

        if (sr != null && sr.sprite != null)
        {
            w = sr.sprite.bounds.size.x * transform.localScale.x / 2f + outlinePadding;
            h = sr.sprite.bounds.size.y * transform.localScale.y / 2f + outlinePadding;
        }
        else if (tmp != null)
        {
            if (tmp.preferredWidth == 0) return;
            w = tmp.preferredWidth * transform.localScale.x / 2f + outlinePadding;
            h = tmp.preferredHeight * transform.localScale.y / 2f + outlinePadding;
        }
        else return;

        Vector3 pos = transform.position;
        float angle = transform.eulerAngles.z * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        lr.SetPosition(0, pos + RotatePoint(new Vector2(-w, -h), cos, sin));
        lr.SetPosition(1, pos + RotatePoint(new Vector2(w, -h), cos, sin));
        lr.SetPosition(2, pos + RotatePoint(new Vector2(w, h), cos, sin));
        lr.SetPosition(3, pos + RotatePoint(new Vector2(-w, h), cos, sin));

        if (scaleHandle != null)
        {
            scaleHandle.transform.position = pos + RotatePoint(new Vector2(w, -h), cos, sin);
            scaleHandle.transform.localScale = Vector3.one * handleSize;
        }

        if (rotateHandle != null)
        {
            rotateHandle.transform.position = pos + RotatePoint(new Vector2(0, h + 0.5f), cos, sin);
            rotateHandle.transform.localScale = Vector3.one * handleSize;
        }
    }

    Vector3 RotatePoint(Vector2 point, float cos, float sin)
    {
        return new Vector3(
            point.x * cos - point.y * sin,
            point.x * sin + point.y * cos,
            0
        );
    }

    public void DestroyAll()
    {
        if (scaleHandle != null) Destroy(scaleHandle);
        if (rotateHandle != null) Destroy(rotateHandle);
        if (outlineObj != null) Destroy(outlineObj);
        Destroy(this);
    }

    Sprite CreateSquareSprite()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
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
                float alpha = Mathf.Clamp01(r - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }
}
