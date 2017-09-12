// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.gdipFunctions.cs
//
// Authors: 
//    Alexandre Pigolkine (pigolkine@gmx.de)
//    Jordi Mas i Hernandez (jordi@ximian.com)
//    Sanjay Gupta (gsanjay@novell.com)
//    Ravindra (rkumar@novell.com)
//    Peter Dennis Bartok (pbartok@novell.com)
//    Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (C) 2004 - 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Security;
using System.Runtime.InteropServices.ComTypes;

namespace System.Drawing
{
    /// <summary>
    /// GDI+ API Functions
    /// </summary>
    internal static partial class GDIPlus
    {
        public const int FACESIZE = 32;
        public const int LANG_NEUTRAL = 0;
        public static IntPtr Display = IntPtr.Zero;
        public static bool UseX11Drawable = false;
        public static bool UseCarbonDrawable = false;
        public static bool UseCocoaDrawable = false;

        private const string GdiPlus = "gdiplus";

        #region gdiplus.dll functions

        internal static ulong GdiPlusToken = 0;

        static void ProcessExit(object sender, EventArgs e)
        {
            // Called all pending objects and claim any pending handle before
            // shutting down
            GC.Collect();
            GC.WaitForPendingFinalizers();
#if false
            GdiPlusToken = 0;

            // This causes crashes in because this call occurs before all
            // managed GDI+ objects are finalized. When they are finalized
            // they call into a shutdown GDI+ and we crash.
            GdiplusShutdown (ref GdiPlusToken);

            // This causes crashes in Mono libgdiplus because this call
            // occurs before all managed GDI objects are finalized
            // When they are finalized they use the closed display and
            // crash
            if (UseX11Drawable && Display != IntPtr.Zero) {
                XCloseDisplay (Display);
            }
#endif
        }

        static GDIPlus()
        {
#if NETSTANDARD1_6
            bool isUnix = !RuntimeInformation.IsOSPlatform (OSPlatform.Windows);
#else
            int platform = (int)Environment.OSVersion.Platform;
            bool isUnix = (platform == 4) || (platform == 6) || (platform == 128);
#endif

            if (isUnix)
            {
                if (Environment.GetEnvironmentVariable("not_supported_MONO_MWF_USE_NEW_X11_BACKEND") != null || Environment.GetEnvironmentVariable("MONO_MWF_MAC_FORCE_X11") != null)
                {
                    UseX11Drawable = true;
                }
                else
                {
                    IntPtr buf = Marshal.AllocHGlobal(8192);
                    // This is kind of a hack but gets us sysname from uname (struct utsname *name) on
                    // linux and darwin
                    if (uname(buf) != 0)
                    {
                        // WTH: We couldn't detect the OS; lets default to X11
                        UseX11Drawable = true;
                    }
                    else
                    {
                        string os = Marshal.PtrToStringAnsi(buf);
                        if (os == "Darwin")
                            UseCarbonDrawable = true;
                        else
                            UseX11Drawable = true;
                    }
                    Marshal.FreeHGlobal(buf);
                }
            }

            // under MS 1.x this event is raised only for the default application domain
#if !NETSTANDARD1_6
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
#endif
        }

        static public bool RunningOnWindows()
        {
            return !UseX11Drawable && !UseCarbonDrawable && !UseCocoaDrawable;
        }

        static public bool RunningOnUnix()
        {
            return UseX11Drawable || UseCarbonDrawable || UseCocoaDrawable;
        }

        // This is win32/gdi, not gdiplus, but it's easier to keep in here, also see above comment
        [DllImport("gdi32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateFontIndirect(ref LOGFONT logfont);
        [DllImport("user32.dll", EntryPoint = "GetDC", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll", EntryPoint = "ReleaseDC", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll", EntryPoint = "SelectObject", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetIconInfo(IntPtr hIcon, out IconInfo iconinfo);
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr CreateIconIndirect([In] ref IconInfo piconinfo);
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern bool DestroyIcon(IntPtr hIcon);
        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll")]
        internal static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll", EntryPoint = "GetSysColor", CallingConvention = CallingConvention.StdCall)]
        public static extern uint Win32GetSysColor(GetSysColorIndex index);

        [DllImport("libc")]
        static extern int uname(IntPtr buf);
        #endregion
    }
}
