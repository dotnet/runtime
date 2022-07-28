// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [LibraryImport(Libraries.Gdi32, StringMarshalling = StringMarshalling.Utf16)]
        public static partial IntPtr CreateICW(string pszDriver, string pszDevice, string? pszPort, IntPtr pdm);
    }
}
