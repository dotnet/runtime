// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Libraries.Advapi32, SetLastError = true)]
        public static partial bool CryptEncrypt(
            SafeCapiKeyHandle hKey,
            SafeHashHandle hHash,
            bool Final,
            int dwFlags,
            byte[]? pbData,
            ref int pdwDataLen,
            int dwBufLen);
    }
}
