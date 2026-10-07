using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 2f;
    public float panSpeed = 1f;

    [Header("Freier Zoom (wie GeoGebra/Canva)")]
    public float minZoom = 0.5f;   // sehr nah ranzoomen erlaubt
    public float maxZoom = 200f;   // sehr weit rauszoomen erlaubt

    [Header("Optional: Start-Ansicht aufs Board einpassen")]
    [Tooltip("Falls gesetzt, zoomt die Kamera beim Start einmalig passend aufs Board. " +
             "Schränkt das Zoomen danach NICHT weiter ein.")]
    public CanvasBoard canvasBoard;

    [Header("Optional: Zoom hart an Board-Größe koppeln")]
    [Tooltip("Wenn aktiviert, kann man nicht weiter rauszoomen als bis das Board bildschirmfüllend ist " +
             "(altes, restriktives Verhalten). Standardmäßig aus für freies Zoomen.")]
    public bool restrictZoomToBoard = false;
    public float maxZoomMargin = 1.15f;

    private Vector3 lastMousePos;
    private bool isPanning = false;

    void Start()
    {
        // Beim Start einmalig passend aufs Board zoomen (angenehmer Einstieg),
        // ohne das spätere freie Zoomen einzuschränken.
        if (canvasBoard != null)
            Camera.main.orthographicSize = GetBoardFitZoom();
    }

    void Update()
    {
        HandleZoom();
        HandlePan();
    }

    float GetBoardFitZoom()
    {
        Camera cam = Camera.main;
        float aspect = cam.aspect;

        float fitByHeight = canvasBoard.canvasSize.y / 2f;
        float fitByWidth = canvasBoard.canvasSize.x / (2f * aspect);

        return Mathf.Max(fitByHeight, fitByWidth) * maxZoomMargin;
    }

    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y / 120f;
        if (Mathf.Approximately(scroll, 0f)) return;

        float currentMin = minZoom;
        float currentMax = maxZoom;

        // Nur wenn explizit gewünscht: Zoom hart an die Board-Größe koppeln
        if (restrictZoomToBoard && canvasBoard != null)
            currentMax = GetBoardFitZoom();

        Camera.main.orthographicSize -= scroll * zoomSpeed;
        Camera.main.orthographicSize = Mathf.Clamp(
            Camera.main.orthographicSize, currentMin, currentMax
        );
    }

    void HandlePan()
    {
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            isPanning = true;
            lastMousePos = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.middleButton.wasReleasedThisFrame)
            isPanning = false;

        if (isPanning)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePos - lastMousePos;
            transform.position -= new Vector3(delta.x, delta.y, 0)
                                  * panSpeed * Time.deltaTime;
            lastMousePos = currentMousePos;
        }
    }
}
