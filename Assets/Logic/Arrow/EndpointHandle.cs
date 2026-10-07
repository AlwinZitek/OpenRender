using UnityEngine;
using UnityEngine.InputSystem;

public class EndpointHandle : MonoBehaviour
{
    public ArrowObject arrow;
    public bool isStartPoint; // true = Startpunkt, false = Endpunkt
    private bool isDragging = false;
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
            // Mit Toleranz statt pixelgenau - siehe HitUtility
            if (HitUtility.HitsCollider(col, GetMouseWorldPos()))
                isDragging = true;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
            isDragging = false;

        if (!isDragging) return;

        Vector3 mousePos = GetMouseWorldPos();

        if (isStartPoint)
            arrow.SetPoints(mousePos, arrow.endPoint);
        else
            arrow.SetPoints(arrow.startPoint, mousePos);

        transform.position = mousePos;
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
