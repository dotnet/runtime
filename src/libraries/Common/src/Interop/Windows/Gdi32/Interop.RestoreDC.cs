// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [LibraryImport(Libraries.Gdi32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool RestoreDC(IntPtr hdc, int nSavedDC);

        public static bool RestoreDC(HandleRef hdc, int nSavedDC)
        {
            bool result = RestoreDC(hdc.Handle, nSavedDC);
            GC.KeepAlive(hdc.Wrapper);
            return result;
        }
    }
}
