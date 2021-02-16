// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class BCrypt
    {
        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS BCryptExportKey(SafeBCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, [Out] byte[]? pbOutput, int cbOutput, out int pcbResult, int dwFlags);
    }
}
