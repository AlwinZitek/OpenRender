using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Info-Button öffnet ein großes Panel mit Credits (wer mitgearbeitet hat)
/// und klickbaren Links, z. B. zur BOKU.
///
/// Setup im Editor (alles manuell in der Hierarchy):
///   Canvas
///   ├─ InfoButton              (Button, Icon z. B. "info-circle")
///   └─ CreditsPanel            (leeres GameObject, dieses Script drauf, AKTIV lassen – es versteckt seine Kinder selbst)
///      ├─ Backdrop             (Image, ganzer Bildschirm, schwarz ~60 % Alpha, + Button-Komponente)
///      └─ Window               (Image, z. B. 700 x 500, zentriert)
///         ├─ CloseButton       (Button, "X" oben rechts)
///         └─ Scroll View       (optional, falls der Text lang wird)
///            └─ CreditsText    (TextMeshPro - Text (UI), Rich Text an, Raycast Target an)
///
/// Dann im Inspector die Felder unten zuweisen.
/// </summary>
public class CreditsPanel : MonoBehaviour
{
    [Header("Referenzen")]
    [SerializeField] private Button infoButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backdropButton;   // optional: Klick neben das Fenster schließt
    [SerializeField] private TMP_Text creditsText;

    [Header("Inhalt")]
    [TextArea(10, 30)]
    [SerializeField] private string credits =
        "<size=150%><b>Über dieses Projekt</b></size>\n\n" +
        "<b>Entwicklung</b>\n" +
        "Vorname Nachname\n\n" +
        "<b>Mitarbeit</b>\n" +
        "Vorname Nachname – Icons\n" +
        "Vorname Nachname – Testing\n\n" +
        "<b>In Zusammenarbeit mit</b>\n" +
        "<link=\"https://boku.ac.at\"><color=#3A7BD5><u>BOKU – Universität für Bodenkultur Wien</u></color></link>\n\n" +
        "<b>Bildquellen</b>\n" +
        "Icons teilweise aus Bioicons (CC0) und Servier Medical Art (CC BY).";

    /// <summary>
    /// Andere Scripts (z. B. Auswahl/Drag auf dem Canvas) können das abfragen,
    /// damit Klicks im offenen Panel nicht durch aufs Board gehen.
    /// </summary>
    public static bool IsOpen { get; private set; }

    private Canvas parentCanvas;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (infoButton != null) infoButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (backdropButton != null) backdropButton.onClick.AddListener(Close);

        if (creditsText != null)
        {
            creditsText.richText = true;
            creditsText.text = credits;
        }

        // Nur die Kinder (Backdrop, Window) werden versteckt. Das CreditsPanel-
        // GameObject selbst bleibt aktiv, damit Update() (Escape, Link-Klicks) läuft.
        SetVisible(false);
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryOpenLinkUnderMouse();
    }

    public void Open()  => SetVisible(true);
    public void Close() => SetVisible(false);
    public void Toggle() => SetVisible(!IsOpen);

    private void SetVisible(bool visible)
    {
        IsOpen = visible;
        // Alle Kinder an/aus, das Script selbst bleibt aktiv
        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);
    }

    private void TryOpenLinkUnderMouse()
    {
        if (creditsText == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? parentCanvas.worldCamera
            : null;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(creditsText, mousePos, cam);
        if (linkIndex < 0) return;

        string url = creditsText.textInfo.linkInfo[linkIndex].GetLinkID();
        if (!string.IsNullOrEmpty(url))
            Application.OpenURL(url);
    }

    private void OnDisable()
    {
        IsOpen = false;
    }
}
