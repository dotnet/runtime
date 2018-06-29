// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing.Internal
{
    /// <summary>
    /// This is an extract of the System.Drawing IntNativeMethods in the CommonUI tree.
    /// This is done to be able to compile the GDI code in both assemblies System.Drawing and System.Windows.Forms.
    /// </summary>
    internal static partial class IntSafeNativeMethods
    {
        public sealed class CommonHandles
        {
            static CommonHandles() { }

            /// <summary>
            /// Handle type for GDI objects.
            /// </summary>
            public static readonly int GDI = System.Internal.HandleCollector.RegisterType("GDI", 90, 50);

            /// <summary>
            /// Handle type for HDC's that count against the Win98 limit of five DC's. HDC's which are not scarce,
            /// such as HDC's for bitmaps, are counted as GDIHANDLE's.
            /// </summary>
            public static readonly int HDC = System.Internal.HandleCollector.RegisterType("HDC", 100, 2); // wait for 2 dc's before collecting
        }

        [DllImport(ExternDll.Gdi32, SetLastError = true, ExactSpelling = true, EntryPoint = "CreateRectRgn", CharSet = CharSet.Auto)]
        public static extern IntPtr IntCreateRectRgn(int x1, int y1, int x2, int y2);

        public static IntPtr CreateRectRgn(int x1, int y1, int x2, int y2)
        {
            IntPtr hRgn = System.Internal.HandleCollector.Add(IntCreateRectRgn(x1, y1, x2, y2), CommonHandles.GDI);
            DbgUtil.AssertWin32(hRgn != IntPtr.Zero, "IntCreateRectRgn([x1={0}, y1={1}, x2={2}, y2={3}]) failed.", x1, y1, x2, y2);
            return hRgn;
        }
    }
}
