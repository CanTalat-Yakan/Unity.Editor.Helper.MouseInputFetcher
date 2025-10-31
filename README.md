# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Mouse Input Fetcher

> Quick overview: Editor-only helper that exposes the OS-level mouse cursor position from native APIs on Windows, macOS, and Linux. One static property: `MouseInputFetcher.CurrentMousePosition`.

A tiny, zero-setup helper that keeps track of the current global mouse cursor position while you work in the Unity Editor. It runs automatically on load and updates every editor frame using lightweight, platform-native calls.

![screenshot](Documentation/Screenshot.png)

## Features
- One-liner access: `Vector2 MouseInputFetcher.CurrentMousePosition`
- Cross-platform native implementations
  - Windows: `user32.dll` → `GetCursorPos`
  - macOS: CoreGraphics → `CGEventGetLocation`
  - Linux: X11 → `XQueryPointer` (best effort; logs warnings if X11 is unavailable)
- Editor-only and InitializeOnLoad: no setup required
- Safe teardown on Linux before reload/quit
- No external dependencies; minimal overhead

## Requirements
- Unity Editor 6000.0+ (Editor-only; no runtime code)
- Platform-specific notes
  - Windows: `user32.dll` is available by default
  - macOS: CoreGraphics is available by default
  - Linux: Requires X11 at runtime (`libX11`). If running under Wayland or without X11, the tool will log a warning and global mouse position will be unavailable

Tip: If you’re on Linux and see warnings about X11, install X11 libraries or run the Editor under an X11 session.

## Usage

Read the current OS-level cursor position anywhere in editor code:

```csharp
using UnityEngine;
using UnityEssentials;

public class ExampleUsage
{
    [UnityEditor.InitializeOnLoadMethod]
    static void LogMouse() {
        UnityEditor.EditorApplication.update += () => {
            Vector2 pos = MouseInputFetcher.CurrentMousePosition;
            // OS desktop pixels; see Coordinate systems below
            // Debug.Log(pos);
        };
    }
}
```

### Mapping to EditorWindow or GUI coordinates
`CurrentMousePosition` is in OS desktop pixels (global screen space). To relate this to a specific EditorWindow:

- Get the window bounds via `EditorWindow.position` (desktop coordinates)
- Compare/convert as needed for the platform’s axis origin
- For IMGUI, GUI coordinates have origin at the top-left of the window

Example: check if the cursor is over a custom EditorWindow and draw an indicator:

```csharp
using UnityEditor;
using UnityEngine;
using UnityEssentials;

public class MouseOverlayWindow : EditorWindow
{
    [MenuItem("Window/Examples/Mouse Overlay Window")] private static void Open() => GetWindow<MouseOverlayWindow>("Mouse Overlay");

    private void OnGUI()
    {
        // Global cursor position (desktop space)
        Vector2 global = MouseInputFetcher.CurrentMousePosition;

        // This window’s desktop rect
        Rect win = position; // x,y are desktop-space; width,height in pixels

        // On Windows/Linux: desktop origin is top-left
        // On macOS: desktop origin is bottom-left
#if UNITY_EDITOR_OSX
        // Flip Y into a top-left origin for comparison with win.y
        float desktopHeight = Display.main.systemHeight; // Alternatively, store your own maximum Y if needed
        global.y = desktopHeight - global.y;
#endif
        bool over = win.Contains(global);

        GUILayout.Label($"Global: {global}");
        GUILayout.Label($"Over window: {over}");

        if (over)
        {
            var r = new Rect(10, 10, 16, 16);
            EditorGUI.DrawRect(r, new Color(1, 0.5f, 0, 0.8f));
            GUI.Label(new Rect(30, 8, 200, 20), "Mouse over window");
        }
    }
}
```

### Coordinate systems
- Output is OS desktop coordinates in physical pixels
- Platform origins
  - Windows, Linux (X11): origin is top-left; Y increases downward
  - macOS (CoreGraphics): origin is bottom-left; Y increases upward
- Multi-monitor: Values are in the virtual desktop space
- To map to a specific Unity view, compare against the view’s desktop rect (`EditorWindow.position`) and adjust axes/origin as needed

## Notes and Limitations
- Editor-only: not included in player builds
- Linux/Wayland: Requires X11; under Wayland, results may be unavailable (a warning is logged)
- Availability: If the native API isn’t available, the property returns the last known value (initially 0,0) and logs a warning once
- No input capture: This utility only reads cursor position; it does not hook or block events

## Files in This Package
- `Editor/MouseInputFetcher.cs` – Core implementation (Windows/macOS/Linux with native calls)
- `Editor/UnityEssentials.MouseInputFetcher.asmdef` – Editor assembly definition
- `package.json` – Package manifest metadata

## Tags
unity, unity-editor, mouse, cursor, input, position, native, user32, coregraphics, x11, helper, utility
