using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextEditUI : MonoBehaviour
{
    private static TextEditUI instance;

    // Liegt das Script auf einem Objekt, das beim Start deaktiviert ist,
    // laeuft Awake() nie und Instance bliebe null. Deshalb hier zur Not
    // selbst suchen - auch unter inaktiven Objekten.
    public static TextEditUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TextEditUI>(FindObjectsInactive.Include);
                if (instance != null) instance.EnsureInit();
            }
            return instance;
        }
    }

    public GameObject panel;
    public TMP_InputField inputField;
    public Button confirmButton;

    private Action<string> onConfirm;
    private bool initialized = false;

    void Awake()
    {
        instance = this;
        EnsureInit();
    }

    void EnsureInit()
    {
        if (initialized) return;
        initialized = true;

        if (inputField != null)
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmEdit);
            confirmButton.onClick.AddListener(ConfirmEdit);
        }

        if (panel != null) panel.SetActive(false);
    }

    // Generisch: funktioniert fuer TextLabel.SetText, GroupObject.SetTitle, etc.
    public void OpenEdit(string currentText, Action<string> onConfirmCallback)
    {
        EnsureInit();

        if (panel == null || inputField == null)
        {
            Debug.LogError("TextEditUI: panel oder inputField nicht zugewiesen.");
            return;
        }

        onConfirm = onConfirmCallback;
        inputField.text = currentText;

        // Falls das Script selbst auf einem deaktivierten Objekt liegt
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        panel.SetActive(true);

        inputField.Select();
        inputField.ActivateInputField();
    }

    void ConfirmEdit()
    {
        onConfirm?.Invoke(inputField.text);
        if (panel != null) panel.SetActive(false);
        onConfirm = null;
    }
}
