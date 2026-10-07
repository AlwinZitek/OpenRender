using System.Collections.Generic;
using UnityEngine;

// Zentrale Klick-Erkennung mit Toleranz.
//
// Frueher: Physics2D.Raycast(worldPos, Vector2.zero) - ein exakter Punkt-Test
// ohne jede Toleranz. Man musste pixelgenau treffen, und je weiter man
// rauszoomte, desto fummeliger wurde es.
//
// Jetzt: Kreis-Test, dessen Radius mit dem Zoom mitskaliert. Dadurch fuehlt
// sich die Trefferzone auf dem Bildschirm immer gleich gross an.
public static class HitUtility
{
    // Trefferzone in Bildschirm-Pixeln (wird in Weltkoordinaten umgerechnet).
    // Groesser = leichter zu treffen, aber ungenauer bei eng beieinander
    // liegenden Objekten.
    public const float HitToleranceInPixels = 10f;

    public static float GetWorldTolerance()
    {
        Camera cam = Camera.main;
        if (cam == null) return 0.1f;

        // Wie viele Weltunits entspricht ein Bildschirmpixel beim aktuellen Zoom?
        float worldUnitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
        return HitToleranceInPixels * worldUnitsPerPixel;
    }

    static Collider2D[] OverlapWithTolerance(Vector3 worldPos)
    {
        // Erst exakt testen: liegt der Klick direkt in einem Collider, hat der
        // Vorrang. Sonst wuerde bei ueberlappenden Objekten die Toleranz
        // manchmal das falsche (naeher am Rand liegende) Objekt gewinnen lassen.
        Collider2D[] exact = Physics2D.OverlapPointAll(worldPos);
        if (exact.Length > 0) return exact;

        return Physics2D.OverlapCircleAll(worldPos, GetWorldTolerance());
    }

    public static GameObject GetPreferredTarget(Vector3 worldPos)
    {
        Collider2D[] hits = OverlapWithTolerance(worldPos);
        if (hits.Length == 0) return null;

        GameObject groupHit = null;
        GameObject memberHit = null;
        GameObject plainHit = null;

        foreach (var col in hits)
        {
            if (col == null) continue;
            GameObject go = col.gameObject;

            // Handles nie als Selektions-/Drag-Ziel werten
            if (col.GetComponent<ScaleHandle>() != null) continue;
            if (col.GetComponent<RotateHandle>() != null) continue;
            if (col.GetComponent<EndpointHandle>() != null) continue;

            if (go.GetComponent<GroupObject>() != null)
            {
                groupHit = go;
                continue;
            }

            bool isMember = false;
            foreach (GroupObject g in GroupObject.AllGroups)
            {
                if (g != null && g.members.Contains(go))
                {
                    isMember = true;
                    break;
                }
            }

            if (isMember)
                memberHit = go;
            else if (plainHit == null)
                plainHit = go;
        }

        // Prioritaet: Gruppen-Mitglied > freies Objekt > Gruppen-Rahmen
        if (memberHit != null) return memberHit;
        if (plainHit != null) return plainHit;
        return groupHit;
    }

    static bool IsHandle(Collider2D col)
    {
        if (col == null) return false;
        return col.GetComponent<ScaleHandle>() != null
            || col.GetComponent<RotateHandle>() != null
            || col.GetComponent<EndpointHandle>() != null;
    }

    // Prueft, ob an dieser Stelle ein Scale-/Rotate-/Endpoint-Handle liegt.
    //
    // Wichtig: die Toleranz gilt hier nur, wenn NICHTS direkt unter dem Cursor
    // liegt. Sonst schluckt ein Handle in der Naehe jeden Klick auf ein kleines
    // Objekt - unter anderem den zweiten Klick eines Doppelklicks.
    public static bool IsHandleHit(Vector3 worldPos)
    {
        Collider2D[] exact = Physics2D.OverlapPointAll(worldPos);

        if (exact.Length > 0)
        {
            // Direkter Treffer: nur ein echt getroffenes Handle zaehlt
            foreach (var col in exact)
                if (IsHandle(col)) return true;
            return false;
        }

        // Nichts direkt getroffen: Handles in Reichweite duerfen greifen
        foreach (var col in Physics2D.OverlapCircleAll(worldPos, GetWorldTolerance()))
            if (IsHandle(col)) return true;

        return false;
    }

    // Fuer die Handles selbst: trifft der Klick diesen konkreten Collider
    // (mit derselben Toleranz wie oben)?
    public static bool HitsCollider(Collider2D col, Vector3 worldPos)
    {
        if (col == null) return false;
        if (col.OverlapPoint(worldPos)) return true;

        float tolerance = GetWorldTolerance();
        // ClosestPoint liefert den naechstgelegenen Punkt auf dem Collider
        Vector2 closest = col.ClosestPoint(worldPos);
        return Vector2.Distance(closest, worldPos) <= tolerance;
    }
}
