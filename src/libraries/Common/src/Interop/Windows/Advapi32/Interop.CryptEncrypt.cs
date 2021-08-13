// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptEncrypt(
            SafeKeyHandle hKey,
            SafeHashHandle hHash,
            bool Final,
            int dwFlags,
            byte[]? pbData,
            ref int pdwDataLen,
            int dwBufLen);
    }
}
