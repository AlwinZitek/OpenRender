using System.Collections;
using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Text;
#endif

/// <summary>
/// Macht die Windows-Titelleiste des Builds dunkel.
/// Windows 11: komplett schwarz (eigene Farbe).
/// Windows 10 (ab 20H1): dunkelgrau (Dark Mode der Titelleiste).
/// Im Editor und auf anderen Plattformen passiert nichts.
///
/// Einfach auf ein GameObject in der Startszene legen.
/// </summary>
public class DarkTitleBar : MonoBehaviour
{
    [Tooltip("Farbe der Titelleiste (nur Windows 11).")]
    [SerializeField] private Color captionColor = Color.black;

    [Tooltip("Farbe des Titeltexts (nur Windows 11).")]
    [SerializeField] private Color textColor = Color.white;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19; // Windows 10 1809–1909
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE     = 20; // Windows 10 20H1+ / 11
    private const int DWMWA_CAPTION_COLOR               = 35; // Windows 11
    private const int DWMWA_TEXT_COLOR                  = 36; // Windows 11

    private const uint WM_NCACTIVATE = 0x0086;
    private const uint RDW_INVALIDATE = 0x0001, RDW_UPDATENOW = 0x0100, RDW_FRAME = 0x0400;
    private const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004,
                       SWP_NOACTIVATE = 0x0010, SWP_FRAMECHANGED = 0x0020;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder name, int maxCount);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr rect, IntPtr region, uint flags);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    private static IntPtr foundWindow;

    private IEnumerator Start()
    {
        // Einen Frame warten, bis das Fenster sicher angezeigt wird
        yield return null;

        IntPtr hwnd = FindUnityWindow();
        if (hwnd == IntPtr.Zero) yield break;

        // Dark Mode für die Titelleiste (Windows 10 + 11)
        int on = 1;
        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref on, sizeof(int));

        // Eigene Farben (nur Windows 11, auf Windows 10 wird das ignoriert)
        int caption = ToColorRef(captionColor);
        int text = ToColorRef(textColor);
        DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
        DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int));

        ForceTitleBarRedraw(hwnd);
    }

    /// <summary>
    /// Windows zeichnet die Titelleiste sonst erst beim nächsten Fokuswechsel neu.
    /// Kurz "inaktiv/aktiv" melden erzwingt das sofort.
    /// </summary>
    private static void ForceTitleBarRedraw(IntPtr hwnd)
    {
        bool isActive = GetForegroundWindow() == hwnd;
        SendMessage(hwnd, WM_NCACTIVATE, isActive ? IntPtr.Zero : (IntPtr)1, IntPtr.Zero);
        SendMessage(hwnd, WM_NCACTIVATE, isActive ? (IntPtr)1 : IntPtr.Zero, IntPtr.Zero);

        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW);
    }

    /// <summary>
    /// Sucht das Unity-Fenster dieses Programms, auch wenn es gerade nicht im Vordergrund ist.
    /// </summary>
    private static IntPtr FindUnityWindow()
    {
        foundWindow = IntPtr.Zero;
        EnumThreadWindows(GetCurrentThreadId(), OnWindow, IntPtr.Zero);
        return foundWindow;
    }

    [AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))]
    private static bool OnWindow(IntPtr hWnd, IntPtr lParam)
    {
        var name = new StringBuilder(64);
        GetClassName(hWnd, name, name.Capacity);
        if (name.ToString() == "UnityWndClass")
        {
            foundWindow = hWnd;
            return false; // gefunden, Suche beenden
        }
        return true;
    }

    // Windows erwartet die Farbe als 0x00BBGGRR
    private static int ToColorRef(Color c)
    {
        Color32 c32 = c;
        return c32.r | (c32.g << 8) | (c32.b << 16);
    }
#endif
}
