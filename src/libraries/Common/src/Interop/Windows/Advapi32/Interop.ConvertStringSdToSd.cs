// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Interop.Libraries.Advapi32, EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW",
            SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ConvertStringSdToSd(
            string stringSd,
            /* DWORD */ uint stringSdRevision,
            out IntPtr resultSd,
            ref uint resultSdLength);
    }
}
