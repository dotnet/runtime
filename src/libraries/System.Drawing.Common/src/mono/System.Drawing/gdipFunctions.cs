// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.gdipFunctions.cs
//
// Authors: 
//	Alexandre Pigolkine (pigolkine@gmx.de)
//	Jordi Mas i Hernandez (jordi@ximian.com)
//	Sanjay Gupta (gsanjay@novell.com)
//	Ravindra (rkumar@novell.com)
//	Peter Dennis Bartok (pbartok@novell.com)
//	Sebastien Pouliot <sebastien@ximian.com>
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
    internal static class GDIPlus
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

        // Copies a Ptr to an array of Points and releases the memory
        static public void FromUnManagedMemoryToPointI(IntPtr prt, Point[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr pos = prt;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                pts[i] = (Point)Marshal.PtrToStructure(pos, typeof(Point));

            Marshal.FreeHGlobal(prt);
        }

        // Copies a Ptr to an array of Points and releases the memory
        static public void FromUnManagedMemoryToPoint(IntPtr prt, PointF[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr pos = prt;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                pts[i] = (PointF)Marshal.PtrToStructure(pos, typeof(PointF));

            Marshal.FreeHGlobal(prt);
        }

        // Copies an array of Points to unmanaged memory
        static public IntPtr FromPointToUnManagedMemoryI(Point[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr dest = Marshal.AllocHGlobal(nPointSize * pts.Length);
            IntPtr pos = dest;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                Marshal.StructureToPtr(pts[i], pos, false);

            return dest;
        }

        // Copies a Ptr to an array of v and releases the memory
        static public void FromUnManagedMemoryToRectangles(IntPtr prt, RectangleF[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr pos = prt;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                pts[i] = (RectangleF)Marshal.PtrToStructure(pos, typeof(RectangleF));

            Marshal.FreeHGlobal(prt);
        }

        // Copies an array of Points to unmanaged memory
        static public IntPtr FromPointToUnManagedMemory(PointF[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr dest = Marshal.AllocHGlobal(nPointSize * pts.Length);
            IntPtr pos = dest;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                Marshal.StructureToPtr(pts[i], pos, false);

            return dest;
        }

        // Converts a status into exception
        // TODO: Add more status code mappings here
        static internal void CheckStatus(Status status)
        {
            string msg;
            switch (status)
            {
                case Status.Ok:
                    return;
                case Status.GenericError:
                    msg = string.Format("Generic Error [GDI+ status: {0}]", status);
                    throw new Exception(msg);
                case Status.InvalidParameter:
                    msg = string.Format("A null reference or invalid value was found [GDI+ status: {0}]", status);
                    throw new ArgumentException(msg);
                case Status.OutOfMemory:
                    msg = string.Format("Not enough memory to complete operation [GDI+ status: {0}]", status);
                    throw new OutOfMemoryException(msg);
                case Status.ObjectBusy:
                    msg = string.Format("Object is busy and cannot state allow this operation [GDI+ status: {0}]", status);
                    throw new MemberAccessException(msg);
                case Status.InsufficientBuffer:
                    msg = string.Format("Insufficient buffer provided to complete operation [GDI+ status: {0}]", status);
#if NETCORE
                    throw new Exception(msg);
#else
				throw new InternalBufferOverflowException (msg);
#endif
                case Status.PropertyNotSupported:
                    msg = string.Format("Property not supported [GDI+ status: {0}]", status);
                    throw new NotSupportedException(msg);
                case Status.FileNotFound:
                    msg = string.Format("Requested file was not found [GDI+ status: {0}]", status);
                    throw new FileNotFoundException(msg);
                case Status.AccessDenied:
                    msg = string.Format("Access to resource was denied [GDI+ status: {0}]", status);
                    throw new UnauthorizedAccessException(msg);
                case Status.UnknownImageFormat:
                    msg = string.Format("Either the image format is unknown or you don't have the required libraries to decode this format [GDI+ status: {0}]", status);
                    throw new NotSupportedException(msg);
                case Status.NotImplemented:
                    msg = string.Format("The requested feature is not implemented [GDI+ status: {0}]", status);
                    throw new NotImplementedException(msg);
                case Status.WrongState:
                    msg = string.Format("Object is not in a state that can allow this operation [GDI+ status: {0}]", status);
                    throw new ArgumentException(msg);
                case Status.FontFamilyNotFound:
                    msg = string.Format("The requested FontFamily could not be found [GDI+ status: {0}]", status);
                    throw new ArgumentException(msg);
                case Status.ValueOverflow:
                    msg = string.Format("Argument is out of range [GDI+ status: {0}]", status);
                    throw new OverflowException(msg);
                case Status.Win32Error:
                    msg = string.Format("The operation is invalid [GDI+ status: {0}]", status);
                    throw new InvalidOperationException(msg);
                default:
                    msg = string.Format("Unknown Error [GDI+ status: {0}]", status);
                    throw new Exception(msg);
            }
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


        // Some special X11 stuff
        [DllImport("libX11", EntryPoint = "XOpenDisplay")]
        internal extern static IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11", EntryPoint = "XCloseDisplay")]
        internal extern static int XCloseDisplay(IntPtr display);

        [DllImport("libX11", EntryPoint = "XRootWindow")]
        internal extern static IntPtr XRootWindow(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XDefaultScreen")]
        internal extern static int XDefaultScreen(IntPtr display);

        [DllImport("libX11", EntryPoint = "XDefaultDepth")]
        internal extern static uint XDefaultDepth(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XGetImage")]
        internal extern static IntPtr XGetImage(IntPtr display, IntPtr drawable, int src_x, int src_y, int width, int height, int pane, int format);

        [DllImport("libX11", EntryPoint = "XGetPixel")]
        internal extern static int XGetPixel(IntPtr image, int x, int y);

        [DllImport("libX11", EntryPoint = "XDestroyImage")]
        internal extern static int XDestroyImage(IntPtr image);

        [DllImport("libX11", EntryPoint = "XDefaultVisual")]
        internal extern static IntPtr XDefaultVisual(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XGetVisualInfo")]
        internal extern static IntPtr XGetVisualInfo(IntPtr display, int vinfo_mask, ref XVisualInfo vinfo_template, ref int nitems);

        [DllImport("libX11", EntryPoint = "XVisualIDFromVisual")]
        internal extern static IntPtr XVisualIDFromVisual(IntPtr visual);

        [DllImport("libX11", EntryPoint = "XFree")]
        internal extern static void XFree(IntPtr data);

        internal sealed class GdiPlusStreamHelper
        {
            public Stream stream;

            private StreamGetHeaderDelegate sghd = null;
            private StreamGetBytesDelegate sgbd = null;
            private StreamSeekDelegate skd = null;
            private StreamPutBytesDelegate spbd = null;
            private StreamCloseDelegate scd = null;
            private StreamSizeDelegate ssd = null;
            private byte[] start_buf;
            private int start_buf_pos;
            private int start_buf_len;
            private byte[] managedBuf;
            private const int default_bufsize = 4096;

            public GdiPlusStreamHelper(Stream s, bool seekToOrigin)
            {
                managedBuf = new byte[default_bufsize];

                stream = s;
                if (stream != null && stream.CanSeek && seekToOrigin)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }

            public int StreamGetHeaderImpl(IntPtr buf, int bufsz)
            {
                int bytesRead;

                start_buf = new byte[bufsz];

                try
                {
                    bytesRead = stream.Read(start_buf, 0, bufsz);
                }
                catch (IOException)
                {
                    return -1;
                }

                if (bytesRead > 0 && buf != IntPtr.Zero)
                {
                    Marshal.Copy(start_buf, 0, (IntPtr)(buf.ToInt64()), bytesRead);
                }

                start_buf_pos = 0;
                start_buf_len = bytesRead;

                return bytesRead;
            }

            public StreamGetHeaderDelegate GetHeaderDelegate
            {
                get
                {
                    if (stream != null && stream.CanRead)
                    {
                        if (sghd == null)
                        {
                            sghd = new StreamGetHeaderDelegate(StreamGetHeaderImpl);
                        }
                        return sghd;
                    }
                    return null;
                }
            }

            public int StreamGetBytesImpl(IntPtr buf, int bufsz, bool peek)
            {
                if (buf == IntPtr.Zero && peek)
                {
                    return -1;
                }

                if (bufsz > managedBuf.Length)
                    managedBuf = new byte[bufsz];
                int bytesRead = 0;
                long streamPosition = 0;

                if (bufsz > 0)
                {
                    if (stream.CanSeek)
                    {
                        streamPosition = stream.Position;
                    }
                    if (start_buf_len > 0)
                    {
                        if (start_buf_len > bufsz)
                        {
                            Array.Copy(start_buf, start_buf_pos, managedBuf, 0, bufsz);
                            start_buf_pos += bufsz;
                            start_buf_len -= bufsz;
                            bytesRead = bufsz;
                            bufsz = 0;
                        }
                        else
                        {
                            // this is easy
                            Array.Copy(start_buf, start_buf_pos, managedBuf, 0, start_buf_len);
                            bufsz -= start_buf_len;
                            bytesRead = start_buf_len;
                            start_buf_len = 0;
                        }
                    }

                    if (bufsz > 0)
                    {
                        try
                        {
                            bytesRead += stream.Read(managedBuf, bytesRead, bufsz);
                        }
                        catch (IOException)
                        {
                            return -1;
                        }
                    }

                    if (bytesRead > 0 && buf != IntPtr.Zero)
                    {
                        Marshal.Copy(managedBuf, 0, (IntPtr)(buf.ToInt64()), bytesRead);
                    }

                    if (!stream.CanSeek && (bufsz == 10) && peek)
                    {
                        // Special 'hack' to support peeking of the type for gdi+ on non-seekable streams
                    }

                    if (peek)
                    {
                        if (stream.CanSeek)
                        {
                            // If we are peeking bytes, then go back to original position before peeking
                            stream.Seek(streamPosition, SeekOrigin.Begin);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }

                return bytesRead;
            }

            public StreamGetBytesDelegate GetBytesDelegate
            {
                get
                {
                    if (stream != null && stream.CanRead)
                    {
                        if (sgbd == null)
                        {
                            sgbd = new StreamGetBytesDelegate(StreamGetBytesImpl);
                        }
                        return sgbd;
                    }
                    return null;
                }
            }

            public long StreamSeekImpl(int offset, int whence)
            {
                // Make sure we have a valid 'whence'.
                if ((whence < 0) || (whence > 2))
                    return -1;

                // Invalidate the start_buf if we're actually going to call a Seek method.
                start_buf_pos += start_buf_len;
                start_buf_len = 0;

                SeekOrigin origin;

                // Translate 'whence' into a SeekOrigin enum member.
                switch (whence)
                {
                    case 0:
                        origin = SeekOrigin.Begin;
                        break;
                    case 1:
                        origin = SeekOrigin.Current;
                        break;
                    case 2:
                        origin = SeekOrigin.End;
                        break;

                    // The following line is redundant but necessary to avoid a
                    // "Use of unassigned local variable" error without actually
                    // initializing 'origin' to a dummy value.
                    default:
                        return -1;
                }

                // Do the actual seek operation and return its result.
                return stream.Seek((long)offset, origin);
            }

            public StreamSeekDelegate SeekDelegate
            {
                get
                {
                    if (stream != null && stream.CanSeek)
                    {
                        if (skd == null)
                        {
                            skd = new StreamSeekDelegate(StreamSeekImpl);
                        }
                        return skd;
                    }
                    return null;
                }
            }

            public int StreamPutBytesImpl(IntPtr buf, int bufsz)
            {
                if (bufsz > managedBuf.Length)
                    managedBuf = new byte[bufsz];
                Marshal.Copy(buf, managedBuf, 0, bufsz);
                stream.Write(managedBuf, 0, bufsz);
                return bufsz;
            }

            public StreamPutBytesDelegate PutBytesDelegate
            {
                get
                {
                    if (stream != null && stream.CanWrite)
                    {
                        if (spbd == null)
                        {
                            spbd = new StreamPutBytesDelegate(StreamPutBytesImpl);
                        }
                        return spbd;
                    }
                    return null;
                }
            }

            public void StreamCloseImpl()
            {
                stream.Dispose();
            }

            public StreamCloseDelegate CloseDelegate
            {
                get
                {
                    if (stream != null)
                    {
                        if (scd == null)
                        {
                            scd = new StreamCloseDelegate(StreamCloseImpl);
                        }
                        return scd;
                    }
                    return null;
                }
            }

            public long StreamSizeImpl()
            {
                try
                {
                    return stream.Length;
                }
                catch
                {
                    return -1;
                }
            }

            public StreamSizeDelegate SizeDelegate
            {
                get
                {
                    if (stream != null)
                    {
                        if (ssd == null)
                        {
                            ssd = new StreamSizeDelegate(StreamSizeImpl);
                        }
                        return ssd;
                    }
                    return null;
                }
            }

        }

        [DllImport("libc")]
        static extern int uname(IntPtr buf);
        #endregion
    }
}
