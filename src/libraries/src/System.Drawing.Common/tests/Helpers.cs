// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace System.Drawing.Tests
{
    public static class Helpers
    {
        public static string GetTestBitmapPath(string fileName) => GetTestPath("bitmaps", fileName);
        public static string GetTestFontPath(string fileName) => GetTestPath("fonts", fileName);

        private static string GetTestPath(string directoryName, string fileName) => Path.Combine(AppContext.BaseDirectory, directoryName, fileName);

        public static void VerifyBitmap(Bitmap bitmap, Color[][] colors)
        {
            try
            {
                for (int y = 0; y < colors.Length; y++)
                {
                    for (int x = 0; x < colors[y].Length; x++)
                    {
                        Color expectedColor = Color.FromArgb(colors[y][x].ToArgb());
                        Color actualColor = bitmap.GetPixel(x, y);

                        if (expectedColor != actualColor)
                        {
                            throw new AssertActualExpectedException(expectedColor, actualColor, $"{x},{y}");
                        }
                    }
                }
            }
            catch (AssertActualExpectedException ex)
            {
                var actualStringBuilder = new StringBuilder();
                var expectedStringBuilder = new StringBuilder();

                actualStringBuilder.AppendLine();
                expectedStringBuilder.AppendLine();

                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        PrintColor(actualStringBuilder, bitmap.GetPixel(x, y));
                        PrintColor(expectedStringBuilder, colors[y][x]);
                        if (x != bitmap.Width - 1)
                        {
                            actualStringBuilder.Append(", ");
                            expectedStringBuilder.Append(", ");
                        }
                    }
                    actualStringBuilder.AppendLine();
                    expectedStringBuilder.AppendLine();
                }
                throw new AssertActualExpectedException(expectedStringBuilder.ToString(), actualStringBuilder.ToString(), $"Bitmaps were different at {ex.UserMessage}.");
            }
        }

        private static void PrintColor(StringBuilder stringBuilder, Color color)
        {
            stringBuilder.Append($"Color.FromArgb({color.A}, {color.R}, {color.G}, {color.B})");
        }

        public static Color EmptyColor => Color.FromArgb(0, 0, 0, 0);

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
