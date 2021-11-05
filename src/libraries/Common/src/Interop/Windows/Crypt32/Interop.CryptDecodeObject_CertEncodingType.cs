// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial bool CryptDecodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, byte[]? pvStructInfo, ref int pcbStructInfo);
    }
}
