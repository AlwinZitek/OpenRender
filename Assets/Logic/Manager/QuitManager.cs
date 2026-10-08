using UnityEngine;
using UnityEngine.InputSystem;

// Beendet die Anwendung.
//
// Im Editor stoppt es den Play-Modus, im Build wird die Anwendung beendet -
// Application.Quit tut im Editor naemlich nichts.
public class QuitManager : MonoBehaviour
{
    public static QuitManager Instance;

    [Tooltip("Escape beendet ebenfalls")]
    public bool quitOnEscape = true;

    void Awake() => Instance = this;

    void Update()
    {
        if (!quitOnEscape) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Quit();
    }

    // An den Beenden-Button haengen
    public void Quit()
    {
#if UNITY_EDITOR
        // Application.Quit wirkt im Editor nicht
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
