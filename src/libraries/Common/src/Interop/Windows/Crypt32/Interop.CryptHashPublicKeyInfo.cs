// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial bool CryptHashPublicKeyInfo(
            IntPtr hCryptProv,
            int algId,
            int dwFlags,
            CertEncodingType dwCertEncodingType,
            ref CERT_PUBLIC_KEY_INFO pInfo,
            byte[] pbComputedHash,
            ref int pcbComputedHash);
    }
}
