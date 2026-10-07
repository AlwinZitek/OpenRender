using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using SFB;

public class ExportManager : MonoBehaviour
{
    public CanvasBoard canvasBoard;  // Board reinziehen im Inspector
    public int pixelsPerUnit = 100;  // Export-Aufloesung

    [Tooltip("Hintergrund-Grid beim Export ausblenden")]
    public InfiniteGridBackground gridBackground;

    public enum ExportMode
    {
        Blatt,              // nur das Board
        BlattUndObjekte,    // Board plus alles, was ausserhalb liegt
        Kameraausschnitt    // genau das, was gerade im Bild ist
    }

    [Header("Ausschnitt")]
    public ExportMode exportMode = ExportMode.BlattUndObjekte;

    [Tooltip("Container mit den platzierten Objekten (derselbe wie bei IconSpawner)")]
    public Transform iconsContainer;

    [Tooltip("Rand um die Objekte herum, in Weltunits")]
    public float outsideMargin = 1f;

    [Header("Hintergrund")]
    [Tooltip("PNG mit transparentem Hintergrund. Blendet auch das weisse Blatt aus.")]
    public bool transparentBackground = false;

    [Tooltip("Maximale Kantenlaenge in Pixeln. Grafikkarten koennen hoechstens 16384; " +
             "wird das Bild groesser, wird die Aufloesung automatisch heruntergerechnet.")]
    public int maxPixelSize = 8192;

    void Update()
    {
        if (Keyboard.current.ctrlKey.isPressed &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Export();
        }
    }

    public void Export()
    {
        StartCoroutine(ExportRoutine());
    }

    // Erweitert die Blatt-Bounds um alle sichtbaren Objekte im Container.
    Bounds ExpandToObjects(Bounds bounds)
    {
        bool any = false;
        Bounds result = bounds;

        foreach (Transform child in iconsContainer)
        {
            if (!child.gameObject.activeSelf) continue;

            foreach (var r in child.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;

                // Outline und Handles gehoeren nicht ins Bild
                if (r.gameObject.name == "Outline") continue;
                if (r.GetComponent<ScaleHandle>() != null) continue;
                if (r.GetComponent<RotateHandle>() != null) continue;
                if (r.GetComponent<EndpointHandle>() != null) continue;

                result.Encapsulate(r.bounds);
                any = true;
            }
        }

        if (any)
            result.Expand(new Vector3(outsideMargin * 2f, outsideMargin * 2f, 0f));

        result.center = new Vector3(result.center.x, result.center.y, 0f);
        return result;
    }

    IEnumerator ExportRoutine()
    {
        SelectionManager.Instance.Deselect();

        // canvasSize statt SpriteRenderer.bounds: die bounds haengen davon ab,
        // welcher Sprite gerade am Board sitzt und wie er skaliert ist - das
        // fuehrte zu voellig falschen Export-Groessen. canvasSize ist der Wert,
        // der die Blattgroesse definiert, also die verlaessliche Quelle.
        Bounds bounds;

        if (exportMode == ExportMode.Kameraausschnitt)
        {
            // Genau der sichtbare Bereich - was du siehst, kommt ins Bild
            Camera c = Camera.main;
            float h = c.orthographicSize * 2f;
            float w = h * c.aspect;
            Vector3 camPos = c.transform.position;
            bounds = new Bounds(new Vector3(camPos.x, camPos.y, 0f), new Vector3(w, h, 0f));
        }
        else
        {
            Vector2 boardSize = canvasBoard.canvasSize;
            Vector3 center = canvasBoard.transform.position;
            bounds = new Bounds(new Vector3(center.x, center.y, 0f),
                                new Vector3(boardSize.x, boardSize.y, 0f));

            if (exportMode == ExportMode.BlattUndObjekte && iconsContainer != null)
                bounds = ExpandToObjects(bounds);
        }

        Vector2 size = bounds.size;

        float ppu = pixelsPerUnit;

        // Aufloesung begrenzen: beim Kameraausschnitt haengt die Groesse am
        // Zoom, und eine zu grosse RenderTexture schlaegt fehl - frueher brach
        // der Export dann mitten drin ab und liess die Kamera auf der Textur
        // haengen (Spielfenster blieb schwarz).
        float longest = Mathf.Max(size.x, size.y) * ppu;
        if (longest > maxPixelSize)
        {
            ppu *= maxPixelSize / longest;
            Debug.Log("Export-Aufloesung reduziert auf " + ppu.ToString("F1") + " px/Unit");
        }

        int width = Mathf.RoundToInt(size.x * ppu);
        int height = Mathf.RoundToInt(size.y * ppu);

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Ungueltige Export-Groesse: " + width + "x" + height);
            yield break;
        }

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        if (!rt.Create())
        {
            Debug.LogError("RenderTexture konnte nicht erstellt werden: " + width + "x" + height);
            Destroy(rt);
            yield break;
        }

        Camera cam = Camera.main;

        Vector3 originalPos = cam.transform.position;
        float originalSize = cam.orthographicSize;
        RenderTexture originalRT = cam.targetTexture;
        Color originalBg = cam.backgroundColor;
        CameraClearFlags originalFlags = cam.clearFlags;
        bool gridWasActive = gridBackground != null && gridBackground.gameObject.activeSelf;

        // Schatten gehoert zur Bildschirmdarstellung, nicht ins Bild - sonst
        // liegt ein grauer Verlauf unter dem Blatt.
        GameObject shadow = canvasBoard != null ? canvasBoard.ShadowObject : null;
        bool shadowWasActive = shadow != null && shadow.activeSelf;

        if (gridBackground != null) gridBackground.gameObject.SetActive(false);
        if (shadow != null) shadow.SetActive(false);

        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        cam.orthographicSize = size.y / 2f;
        // Seitenverhaeltnis explizit auf die RenderTexture setzen, sonst
        // schneidet die Kamera links/rechts ab oder laesst Raender stehen.
        cam.aspect = (float)width / height;
        cam.backgroundColor = transparentBackground ? new Color(0, 0, 0, 0) : Color.white;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // Das Blatt ist eine weisse Flaeche - bei transparentem Export muss es
        // mit raus, sonst ist der Hintergrund trotzdem weiss.
        SpriteRenderer boardRenderer = canvasBoard != null
            ? canvasBoard.GetComponent<SpriteRenderer>() : null;
        bool boardWasEnabled = boardRenderer != null && boardRenderer.enabled;
        if (transparentBackground && boardRenderer != null) boardRenderer.enabled = false;

        bool rendered = false;

        // Bevorzugter Weg unter URP/Unity 6: synchroner Render-Request.
        // cam.Render() stammt aus der Built-in Pipeline und rendert unter URP
        // oft ins Leere.
        var request = new RenderPipeline.StandardRequest { destination = rt };

        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            cam.SubmitRenderRequest(request);
            rendered = true;
        }
        else
        {
            // Fallback: targetTexture setzen und die Pipeline im normalen
            // Frame rendern lassen.
            cam.targetTexture = rt;
            yield return new WaitForEndOfFrame();
            rendered = true;
        }

        Texture2D tex = null;

        if (rendered)
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = rt;

            tex = new Texture2D(width, height,
                transparentBackground ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = previousActive;
        }

        // Kamera und Szene zuruecksetzen
        cam.transform.position = originalPos;
        cam.orthographicSize = originalSize;
        cam.targetTexture = originalRT;
        cam.backgroundColor = originalBg;
        cam.clearFlags = originalFlags;
        cam.ResetAspect();

        if (gridBackground != null) gridBackground.gameObject.SetActive(gridWasActive);
        if (shadow != null) shadow.SetActive(shadowWasActive);
        if (boardRenderer != null) boardRenderer.enabled = boardWasEnabled;

        rt.Release();
        Destroy(rt);

        if (tex == null)
        {
            Debug.LogError("Export fehlgeschlagen: Kamera konnte nicht gerendert werden.");
            yield break;
        }

        byte[] png = tex.EncodeToPNG();
        Destroy(tex);

        string defaultName = "BioRender_Export_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

        var extensions = new[] { new ExtensionFilter("PNG Bild", "png") };
        string path = StandaloneFileBrowser.SaveFilePanel(
            "Grafik exportieren", "", defaultName, extensions
        );

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Export abgebrochen.");
            yield break;
        }

        File.WriteAllBytes(path, png);
        Debug.Log("Exportiert nach: " + path + " (" + width + "x" + height + ")");
    }
}
