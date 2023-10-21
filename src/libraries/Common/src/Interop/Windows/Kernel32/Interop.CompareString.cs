// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "CompareStringW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int CompareString(
            uint locale,
            uint dwCmpFlags,
            string lpString1,
            int cchCount1,
            string lpString2,
            int cchCount2);
    }
}
