using TMPro;
using UnityEngine;

// Liest und setzt die Zeichenreihenfolge eines Objekts. Muss mehrere
// Renderer-Typen abdecken: Icons haben einen SpriteRenderer, Text einen
// TextMeshPro, Pfeile einen LineRenderer plus die Spitze als Kind.
public static class ZOrderUtility
{
    // Untergrenze 0: darunter liegen Board (-10), Board-Schatten (-11) und
    // der unendliche Grid-Hintergrund. Duerften Objekte negativ werden,
    // verschwaenden sie hinter dem Blatt.
    public const int Min = 0;
    public const int Max = 99;

    public static int GetOrder(GameObject obj)
    {
        if (obj == null) return 0;

        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.sortingOrder;

        var tmp = obj.GetComponent<TextMeshPro>();
        if (tmp != null) return tmp.sortingOrder;

        var lr = obj.GetComponent<LineRenderer>();
        if (lr != null) return lr.sortingOrder;

        return 0;
    }

    public static void SetOrder(GameObject obj, int order)
    {
        if (obj == null) return;
        order = Mathf.Clamp(order, Min, Max);

        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = order;

        var tmp = obj.GetComponent<TextMeshPro>();
        if (tmp != null) tmp.sortingOrder = order;

        var lr = obj.GetComponent<LineRenderer>();
        if (lr != null) lr.sortingOrder = order;

        // Kinder mitziehen, aber Outline und Handles auslassen -
        // die muessen immer oben bleiben.
        foreach (Transform child in obj.transform)
        {
            if (child.name == "Outline") continue;
            if (child.GetComponent<ScaleHandle>() != null) continue;
            if (child.GetComponent<RotateHandle>() != null) continue;
            if (child.GetComponent<EndpointHandle>() != null) continue;

            var childSr = child.GetComponent<SpriteRenderer>();
            if (childSr != null) childSr.sortingOrder = order + 1;
        }

        // Gruppen: alle Mitglieder mitnehmen
        var grp = obj.GetComponent<GroupObject>();
        if (grp != null)
        {
            foreach (GameObject member in grp.members)
                if (member != null && member != obj) SetOrder(member, order);
        }
    }
}
