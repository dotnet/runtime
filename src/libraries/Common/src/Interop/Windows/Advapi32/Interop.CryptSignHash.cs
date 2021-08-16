// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal enum KeySpec : int
        {
            AT_KEYEXCHANGE = 1,
            AT_SIGNATURE = 2,
        }

        [Flags]
        internal enum CryptSignAndVerifyHashFlags : int
        {
            None = 0x00000000,
            CRYPT_NOHASHOID = 0x00000001,
            CRYPT_TYPE2_FORMAT = 0x00000002,  // Not supported
            CRYPT_X931_FORMAT = 0x00000004,  // Not supported
        }

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CryptSignHashW")]
        public static extern bool CryptSignHash(
            SafeHashHandle hHash,
            KeySpec dwKeySpec,
            string? szDescription,
            CryptSignAndVerifyHashFlags dwFlags,
            [Out] byte[]? pbSignature,
            [In, Out] ref int pdwSigLen);

        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CryptVerifySignatureW")]
        public static extern bool CryptVerifySignature(
            SafeHashHandle hHash,
            byte[] pbSignature,
            int dwSigLen,
            SafeKeyHandle hPubKey,
            string? szDescription,
            CryptSignAndVerifyHashFlags dwFlags);
    }
}
