// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal enum CryptDecryptFlags : int
        {
            CRYPT_OAEP = 0x00000040,
            CRYPT_DECRYPT_RSA_NO_PADDING_CHECK = 0x00000020
        }

        [DllImport(Libraries.Advapi32, SetLastError = true)]
        public static extern bool CryptDecrypt(
            SafeKeyHandle hKey,
            SafeHashHandle hHash,
            bool Final,
            int dwFlags,
            byte[] pbData,
            ref int pdwDataLen);
    }
}
