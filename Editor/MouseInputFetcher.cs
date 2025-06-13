#if UNITY_EDITOR
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
        public static Vector2 CurrentMousePosition => new Vector2(_mousePoint.x, _mousePoint.y);

#elif UNITY_EDITOR_OSX
        private static CGPoint _mousePoint;
        private struct CGPoint{
            public double X { get; set; }
            public double Y { get; set; }
        }

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGEventCreate(IntPtr source);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern CGPoint CGEventGetLocation(IntPtr theEvent);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CFRelease(IntPtr obj);
     
        public static Vector2 CurrentMousePosition => new Vector2((float)_mousePoint.X, (float)_mousePoint.Y);
#endif

        static MouseInputFetcher() =>
            EditorApplication.update += Update;

        private static void Update()
        {
#if UNITY_EDITOR_WIN
            GetCursorPos(ref _mousePoint);
#elif UNITY_EDITOR_OSX
            var theEvent = CGEventCreate(IntPtr.Zero);
            var point = CGEventGetLocation(theEvent);
            CFRelease(theEvent);
#endif
        }
    }
}
#endif