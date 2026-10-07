using UnityEngine;

// Sorgt dafür, dass das Grid sich beim Zoomen wie bei BioRender/Figma verhält:
// - Linien bleiben eine konstant dünne "Haarlinie" auf dem Bildschirm
// - Die Zellgröße springt beim Rauszoomen auf gröbere 1-2-5-Stufen,
//   statt unendlich zu verdichten (verhindert Moiré/Flimmern)
public class GridZoomAdapter : MonoBehaviour
{
    public Material gridMaterial;
    public Camera cam;

    [Header("Zell-Dichte")]
    [Tooltip("Ungefähr so viele Grid-Zellen sollen horizontal sichtbar sein")]
    public int targetCellsAcross = 24;

    [Header("Linienbreite")]
    [Tooltip("Linienbreite in Weltkoordinaten pro Einheit orthographicSize")]
    public float lineWidthPerOrthoUnit = 0.003f;

    private float lastOrthoSize = -1f;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (gridMaterial == null || cam == null) return;

        // Nur neu berechnen wenn sich der Zoom tatsächlich geändert hat
        if (Mathf.Approximately(cam.orthographicSize, lastOrthoSize)) return;
        lastOrthoSize = cam.orthographicSize;

        float visibleWidth = cam.orthographicSize * 2f * cam.aspect;
        float desiredCellSize = visibleWidth / targetCellsAcross;

        float gridSize = SnapToNiceStep(desiredCellSize);
        float lineWidth = lineWidthPerOrthoUnit * cam.orthographicSize;

        gridMaterial.SetFloat("_GridSize", gridSize);
        gridMaterial.SetFloat("_LineWidth", lineWidth);
    }

    // Rundet auf die nächste "1-2-5-10..."-Stufe auf, wie bei
    // Design-Tool-Grids üblich (statt stufenlos zu skalieren).
    float SnapToNiceStep(float value)
    {
        float[] steps = { 1f, 2f, 5f };
        float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(Mathf.Max(value, 0.0001f))));

        foreach (float step in steps)
        {
            float candidate = step * magnitude;
            if (candidate >= value) return candidate;
        }

        return 10f * magnitude;
    }
}
