// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        public enum CombineMode : int
        {
            RGN_AND = 1,
            RGN_XOR = 3,
            RGN_DIFF = 4,
        }

        [LibraryImport(Libraries.Gdi32, SetLastError = true)]
        public static partial RegionType CombineRgn(IntPtr hrgnDst, IntPtr hrgnSrc1, IntPtr hrgnSrc2, CombineMode iMode);

        public static RegionType CombineRgn(HandleRef hrgnDst, HandleRef hrgnSrc1, HandleRef hrgnSrc2, CombineMode iMode)
        {
            RegionType result = CombineRgn(hrgnDst.Handle, hrgnSrc1.Handle, hrgnSrc2.Handle, iMode);
            GC.KeepAlive(hrgnDst.Wrapper);
            GC.KeepAlive(hrgnSrc1.Wrapper);
            GC.KeepAlive(hrgnSrc2.Wrapper);
            return result;
        }
    }
}
