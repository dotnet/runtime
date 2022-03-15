// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial NTSTATUS BCryptExportKey(SafeBCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, byte[]? pbOutput, int cbOutput, out int pcbResult, int dwFlags);
    }
}
