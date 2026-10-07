using UnityEngine;
using UnityEngine.InputSystem;

public class ScaleHandle : MonoBehaviour
{
    public Transform target;
    private bool isDragging = false;
    private Vector3 startMousePos;
    private Vector3 startScale;
    private Vector3 scaleBeforeDrag;
    private Camera cam;
    private Collider2D col;

    void Awake()
    {
        cam = Camera.main;
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && !isDragging)
        {
            Vector3 worldPos = GetMouseWorldPos();
            // Mit Toleranz statt pixelgenau - siehe HitUtility
            if (HitUtility.HitsCollider(col, worldPos))
            {
                isDragging = true;
                startMousePos = worldPos;
                startScale = target.localScale;
                scaleBeforeDrag = target.localScale;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            if (target.localScale != scaleBeforeDrag)
            {
                UndoManager.Instance.RegisterCommand(
                    new ScaleCommand(target.gameObject, scaleBeforeDrag, target.localScale)
                );
            }
        }

        if (!isDragging) return;

        Vector3 currentMousePos = GetMouseWorldPos();
        float currentDist = Vector3.Distance(target.position, currentMousePos);
        float startDist = Vector3.Distance(target.position, startMousePos);

        if (startDist == 0) return;

        float ratio = currentDist / startDist;
        float minScale = 0.1f;

        Vector3 newScale = Vector3.Max(
            startScale * ratio,
            Vector3.one * minScale
        );

        target.localScale = SnapSettings.SnapScale(newScale);
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
