// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [DllImport(Libraries.Gdi32, ExactSpelling = true)]
        public static extern bool DeleteDC(IntPtr hdc);

        public static bool DeleteDC(HandleRef hdc)
        {
            bool result = DeleteDC(hdc.Handle);
            GC.KeepAlive(hdc.Wrapper);
            return result;
        }
    }
}
