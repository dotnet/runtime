// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CertStrToNameW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial bool CertStrToName(CertEncodingType dwCertEncodingType, string pszX500, CertNameStrTypeAndFlags dwStrType, IntPtr pvReserved, byte[]? pbEncoded, ref int pcbEncoded, IntPtr ppszError);
    }
}
