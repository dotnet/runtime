// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptOpenKey(SafeNCryptProviderHandle hProvider, out SafeNCryptKeyHandle phKey, string pszKeyName, int dwLegacyKeySpec, CngKeyOpenOptions dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptImportKey(SafeNCryptProviderHandle hProvider, IntPtr hImportKey, string pszBlobType, IntPtr pParameterList, [Out] out SafeNCryptKeyHandle phKey, [In] byte[] pbData, int cbData, int dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptExportKey(SafeNCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, IntPtr pParameterList, [Out] byte[] pbOutput, int cbOutput, [Out] out int pcbResult, int dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptDeleteKey(SafeNCryptKeyHandle hKey, int dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptCreatePersistedKey(SafeNCryptProviderHandle hProvider, out SafeNCryptKeyHandle phKey, string pszAlgId, string pszKeyName, int dwLegacyKeySpec, CngKeyCreationOptions dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptFinalizeKey(SafeNCryptKeyHandle hKey, int dwFlags);
    }
}
