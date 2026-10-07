using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SceneData
{
    public List<ObjectData> objects = new List<ObjectData>();
}

[System.Serializable]
public class ObjectData
{
    public string type;        // "Icon", "Text", "Arrow", "Group"
    public string name;        // Icon Name, Text Inhalt oder Gruppen-Titel
    public float posX, posY;
    public float scaleX, scaleY;
    public float rotation;
    public float colorR, colorG, colorB, colorA;

    // Zeichenreihenfolge (ZOrderUtility). Alte Dateien ohne dieses Feld
    // lesen sich als 0; CreateObjectFromData faellt dann auf den
    // Standardwert des jeweiligen Typs zurueck.
    public int sortingOrder;
    public bool hasSortingOrder;

    // Nur für Pfeile
    public float startX, startY;
    public float endX, endY;

    // Linie statt Pfeil. Bewusst invertiert gespeichert: alte Dateien ohne
    // dieses Feld lesen sich als false und bleiben damit Pfeile.
    public bool hideArrowHead;

    // Nur fuer Text: Name des TMP-Font-Assets. Leer = Standardschrift.
    public string fontName;

    // Nur für Gruppen: Indizes der Mitglieder in der objects-Liste
    public List<int> memberIndices = new List<int>();
}
