#if UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    [InitializeOnLoad]
    public class MouseInputFetcher
    {
#if UNITY_EDITOR_WIN
        private static MousePoint _mousePoint;
        private struct MousePoint
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern int GetCursorPos(ref MousePoint lpPoint);
        public static Vector2 CurrentMousePosition => new(_mousePoint.x, _mousePoint.y);

#elif UNITY_EDITOR_OSX
        [StructLayout(LayoutKind.Sequential)]
        private struct CGPoint
        {
            public double X;
            public double Y;
        }
        private static CGPoint _mousePoint;

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGEventCreate(IntPtr source);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern CGPoint CGEventGetLocation(IntPtr theEvent);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CFRelease(IntPtr obj);

        public static Vector2 CurrentMousePosition => new((float)_mousePoint.X, (float)_mousePoint.Y);

#elif UNITY_EDITOR_LINUX
        // X11 interop to get global mouse position in Linux editor
        private static IntPtr _display = IntPtr.Zero;
        private static IntPtr _rootWindow = IntPtr.Zero;
        private static int _mouseX;
        private static int _mouseY;
        private static bool _x11Unavailable;
        private static bool _x11WarningLogged;

        [DllImport("libX11")]
        private static extern IntPtr XOpenDisplay(IntPtr displayName);

        [DllImport("libX11")]
        private static extern int XDefaultScreen(IntPtr display);

        [DllImport("libX11")]
        private static extern IntPtr XRootWindow(IntPtr display, int screenNumber);

        [DllImport("libX11")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool XQueryPointer(
            IntPtr display,
            IntPtr w,
            out IntPtr rootReturn,
            out IntPtr childReturn,
            out int rootXReturn,
            out int rootYReturn,
            out int winXReturn,
            out int winYReturn,
            out uint maskReturn);

        [DllImport("libX11")]
        private static extern int XCloseDisplay(IntPtr display);

        public static Vector2 CurrentMousePosition => new(_mouseX, _mouseY);
#endif

        static MouseInputFetcher()
        {
            EditorApplication.update += Update;
#if UNITY_EDITOR_LINUX
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
            EditorApplication.quitting += OnBeforeReload;
#endif
        }

        private static void Update()
        {
#if UNITY_EDITOR_WIN
            GetCursorPos(ref _mousePoint);
#elif UNITY_EDITOR_OSX
            var theEvent = CGEventCreate(IntPtr.Zero);
            _mousePoint = CGEventGetLocation(theEvent);
            CFRelease(theEvent);
#elif UNITY_EDITOR_LINUX
            if (_x11Unavailable)
            {
                return;
            }

            // Lazily initialize X11 display and root window
            if (_display == IntPtr.Zero)
            {
                try
                {
                    _display = XOpenDisplay(IntPtr.Zero);
                    if (_display != IntPtr.Zero)
                    {
                        int screen = XDefaultScreen(_display);
                        _rootWindow = XRootWindow(_display, screen);
                    }
                    else if (!_x11WarningLogged)
                    {
                        _x11WarningLogged = true;
                        Debug.LogWarning("MouseInputFetcher: XOpenDisplay returned null. Global mouse position will be unavailable on Linux.");
                    }
                }
                catch (DllNotFoundException)
                {
                    _x11Unavailable = true;
                    if (!_x11WarningLogged)
                    {
                        _x11WarningLogged = true;
                        Debug.LogWarning("MouseInputFetcher: libX11 not found. Install X11 libraries to enable global mouse position on Linux.");
                    }
                    return;
                }
                catch (Exception ex)
                {
                    _x11Unavailable = true;
                    if (!_x11WarningLogged)
                    {
                        _x11WarningLogged = true;
                        Debug.LogWarning($"MouseInputFetcher: Failed to initialize X11. {ex.Message}");
                    }
                    return;
                }
            }

            if (_display != IntPtr.Zero && _rootWindow != IntPtr.Zero)
            {
                int rootX, rootY;
                bool ok = XQueryPointer(_display, _rootWindow, out _, out _, out rootX, out rootY, out _, out _, out _);
                if (ok)
                {
                    _mouseX = rootX;
                    _mouseY = rootY;
                }
            }
#endif
        }

#if UNITY_EDITOR_LINUX
        private static void OnBeforeReload()
        {
            if (_display != IntPtr.Zero)
            {
                try { XCloseDisplay(_display); }
                catch { /* ignore */ }
                _display = IntPtr.Zero;
                _rootWindow = IntPtr.Zero;
            }
        }
#endif
    }
}
#endif