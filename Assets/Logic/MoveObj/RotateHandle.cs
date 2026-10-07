using UnityEngine;
using UnityEngine.InputSystem;

public class RotateHandle : MonoBehaviour
{
    public Transform target;
    private bool isDragging = false;
    private float startAngle;
    private float rotationBeforeDrag;
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
            {
                isDragging = true;
                startAngle = GetMouseAngle();
                rotationBeforeDrag = target.eulerAngles.z;
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;

            if (target.eulerAngles.z != rotationBeforeDrag)
            {
                UndoManager.Instance.RegisterCommand(
                    new RotateCommand(target.gameObject, rotationBeforeDrag, target.eulerAngles.z)
                );
            }
        }

        if (!isDragging) return;

        float currentAngle = GetMouseAngle();
        float delta = currentAngle - startAngle;

        float angle = SnapSettings.SnapAngle(rotationBeforeDrag + delta);
        target.eulerAngles = new Vector3(0, 0, angle);
    }

    float GetMouseAngle()
    {
        Vector3 mouseWorld = GetMouseWorldPos();
        Vector2 dir = mouseWorld - target.position;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
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
