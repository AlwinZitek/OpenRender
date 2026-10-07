using UnityEngine;
using UnityEngine.InputSystem;

// Zentrale Snapping-Einstellungen fuer Bewegen, Skalieren und Rotieren.
//
// Shift gedrueckt halten kehrt die Einstellung kurzzeitig um: bei aktivem
// Snapping frei bewegen, bei deaktiviertem einrasten. So wie in den meisten
// Design-Programmen.
public class SnapSettings : MonoBehaviour
{
    public static SnapSettings Instance;

    [Header("Bewegen")]
    public bool snapMove = true;
    [Tooltip("Rasterweite in Weltunits")]
    public float moveStep = 0.5f;

    [Header("Skalieren")]
    public bool snapScale = true;
    [Tooltip("Stufen der Skalierung, z.B. 0.25 = 0.25 / 0.5 / 0.75 ...")]
    public float scaleStep = 0.25f;

    [Header("Rotieren")]
    public bool snapRotate = true;
    [Tooltip("Winkelschritte in Grad")]
    public float rotateStep = 15f;

    [Header("Bedienung")]
    [Tooltip("Shift kehrt das Snapping kurzzeitig um")]
    public bool shiftInverts = true;

    void Awake() => Instance = this;

    static bool ShiftHeld()
    {
        return Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
    }

    bool Active(bool setting)
    {
        if (shiftInverts && ShiftHeld()) return !setting;
        return setting;
    }

    // Statische Helfer: funktionieren auch, wenn kein SnapSettings in der
    // Szene liegt - dann ohne Snapping.
    public static Vector3 SnapPosition(Vector3 pos)
    {
        if (Instance == null || !Instance.Active(Instance.snapMove)) return pos;

        float s = Instance.moveStep;
        if (s <= 0f) return pos;

        return new Vector3(
            Mathf.Round(pos.x / s) * s,
            Mathf.Round(pos.y / s) * s,
            pos.z
        );
    }

    public static Vector3 SnapScale(Vector3 scale)
    {
        if (Instance == null || !Instance.Active(Instance.snapScale)) return scale;

        float s = Instance.scaleStep;
        if (s <= 0f) return scale;

        return new Vector3(
            Mathf.Max(s, Mathf.Round(scale.x / s) * s),
            Mathf.Max(s, Mathf.Round(scale.y / s) * s),
            scale.z
        );
    }

    public static float SnapAngle(float degrees)
    {
        if (Instance == null || !Instance.Active(Instance.snapRotate)) return degrees;

        float s = Instance.rotateStep;
        if (s <= 0f) return degrees;

        return Mathf.Round(degrees / s) * s;
    }

    // Zum Verkabeln an Toolbar-Schalter
    public void ToggleMove() => snapMove = !snapMove;
    public void ToggleScale() => snapScale = !snapScale;
    public void ToggleRotate() => snapRotate = !snapRotate;

    public void SetSnapMove(bool v) => snapMove = v;
    public void SetSnapScale(bool v) => snapScale = v;
    public void SetSnapRotate(bool v) => snapRotate = v;
}
