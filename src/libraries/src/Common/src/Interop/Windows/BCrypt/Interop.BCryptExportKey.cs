// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class BCrypt
    {
#if !NETNATIVE
        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        internal static extern NTSTATUS BCryptExportKey(SafeBCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, [Out] byte[] pbOutput, int cbOutput, out int pcbResult, int dwFlags);
#endif
    }
}

