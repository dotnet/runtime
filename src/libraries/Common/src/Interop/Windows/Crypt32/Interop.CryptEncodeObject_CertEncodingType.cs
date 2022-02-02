// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial bool CryptEncodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, void* pvStructInfo, byte[]? pbEncoded, ref int pcbEncoded);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static unsafe partial bool CryptEncodeObject(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] string lpszStructType, void* pvStructInfo, byte[]? pbEncoded, ref int pcbEncoded);
    }
}
