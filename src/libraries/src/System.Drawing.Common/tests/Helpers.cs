// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Tests
{
    public static class Helpers
    {
        public static string GetTestBitmapPath(string fileName) => GetTestPath("bitmaps", fileName);
        public static string GetTestFontPath(string fileName) => GetTestPath("fonts", fileName);

        private static string GetTestPath(string directoryName, string fileName) => Path.Combine(AppContext.BaseDirectory, directoryName, fileName);

        private static Rectangle GetRectangle(RECT rect)
        {
            return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        private const int MONITOR_DEFAULTTOPRIMARY = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr MonitorFromWindow(IntPtr hWnd, int dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO monitorInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        public static Rectangle GetWindowDCRect(IntPtr hdc) => GetHWndRect(WindowFromDC(hdc));

        public static Rectangle GetHWndRect(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return GetMonitorRectForWindow(hWnd);
            }

            var rect = new RECT();
            GetClientRect(hWnd, ref rect);

            return GetRectangle(rect);
        }

        private static Rectangle GetMonitorRectForWindow(IntPtr hWnd)
        {
            IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTOPRIMARY);
            Assert.NotEqual(IntPtr.Zero, hMonitor);

            var info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf(info);
            int result = GetMonitorInfo(hMonitor, ref info);
            Assert.NotEqual(0, result);

            return GetRectangle(info.rcMonitor);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetClientRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr WindowFromDC(IntPtr hdc);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
