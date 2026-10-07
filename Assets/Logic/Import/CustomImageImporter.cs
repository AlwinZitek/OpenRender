using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SFB; // StandaloneFileBrowser

public class CustomImageImporter : MonoBehaviour
{
    public static CustomImageImporter Instance;

    [Header("Spawn")]
    public Transform iconsContainer;

    [Header("Sidebar")]
    [Tooltip("Wird nach dem Import aktualisiert, damit das neue Bild sofort auftaucht")]
    public SidebarManager sidebar;

    void Awake()
    {
        Instance = this;
    }

    // An den Import-Button haengen:
    // importButton.onClick.AddListener(() => CustomImageImporter.Instance.OpenNativeDialog());
    public void OpenNativeDialog()
    {
        var extensions = new[] { new ExtensionFilter("Bilder", "png", "jpg", "jpeg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Grafik importieren", "", extensions, false
        );

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            ImportImage(paths[0]);
    }

    public void ImportImage(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning("Datei nicht gefunden: " + sourcePath);
            return;
        }

        if (CustomImageLibrary.Instance == null)
        {
            Debug.LogError("CustomImageLibrary fehlt in der Szene!");
            return;
        }

        // Datei in den App-Ordner kopieren, damit die Bibliothek unabhaengig
        // vom urspruenglichen Speicherort funktioniert.
        string uniqueName = Guid.NewGuid().ToString("N") + Path.GetExtension(sourcePath).ToLower();
        string destPath = Path.Combine(CustomImageLibrary.Instance.GetFolderPath(), uniqueName);
        File.Copy(sourcePath, destPath, true);

        Sprite preview = CustomImageLibrary.Instance.LoadSprite(uniqueName);

        // Dialog fuer Name + Tags; ohne Dialog direkt mit Dateinamen uebernehmen
        if (ImportDialogUI.Instance != null)
        {
            ImportDialogUI.Instance.Open(uniqueName, sourcePath, preview,
                (name, tagsRaw, spawn) => FinishImport(uniqueName, name, tagsRaw, spawn));
        }
        else
        {
            FinishImport(uniqueName, Path.GetFileNameWithoutExtension(sourcePath), "", true);
        }
    }

    void FinishImport(string storedFileName, string displayName, string tagsRaw, bool spawn)
    {
        List<string> tags = CustomImageLibrary.ParseTags(tagsRaw);
        IconData icon = CustomImageLibrary.Instance.AddEntry(storedFileName, displayName, tags);
        if (icon == null) return;

        if (sidebar != null) sidebar.RefreshIcons();

        if (spawn && IconSpawner.Instance != null)
            IconSpawner.Instance.SpawnIcon(icon);
    }

    // Wird von SaveManager beim Laden genutzt: Bild aus der Bibliothek
    // wiederherstellen, ohne Undo-Eintrag.
    public GameObject SpawnImportedSprite(string storedFileName, bool registerUndo = true)
    {
        if (CustomImageLibrary.Instance == null) return null;

        Sprite sprite = CustomImageLibrary.Instance.LoadSprite(storedFileName);
        if (sprite == null) return null;

        GameObject obj = new GameObject("Custom_" + storedFileName);
        obj.transform.SetParent(iconsContainer);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        obj.AddComponent<DraggableIcon>();
        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<SelectionOutline>();

        obj.AddComponent<CustomImageMarker>().sourceFileName = storedFileName;

        Vector3 spawnPos = Camera.main.transform.position;
        spawnPos.z = 0f;
        obj.transform.position = spawnPos;
        obj.transform.localScale = Vector3.one * 2f;

        if (registerUndo)
        {
            UndoManager.Instance.RegisterCommand(
                new SpawnCommand(obj, iconsContainer, null)
            );
        }

        return obj;
    }
}
