// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [LibraryImport(Libraries.Gdi32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool OffsetViewportOrgEx(IntPtr hdc, int x, int y, ref Point lppt);

        public static bool OffsetViewportOrgEx(HandleRef hdc, int x, int y, ref Point lppt)
        {
            bool result = OffsetViewportOrgEx(hdc.Handle, x, y, ref lppt);
            GC.KeepAlive(hdc.Wrapper);
            return result;
        }
    }
}
