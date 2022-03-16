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
        public static partial bool DeleteObject(IntPtr ho);

        public static bool DeleteObject(HandleRef ho)
        {
            bool result = DeleteObject(ho.Handle);
            GC.KeepAlive(ho.Wrapper);
            return result;
        }
    }
}
