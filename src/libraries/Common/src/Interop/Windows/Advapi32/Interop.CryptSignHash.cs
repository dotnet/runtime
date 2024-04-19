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

        [LibraryImport(Libraries.Advapi32, EntryPoint = "CryptSignHashW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CryptSignHash(
            SafeHashHandle hHash,
            KeySpec dwKeySpec,
            string? szDescription,
            CryptSignAndVerifyHashFlags dwFlags,
            byte[]? pbSignature,
            ref int pdwSigLen);

        [LibraryImport(Libraries.Advapi32, EntryPoint = "CryptVerifySignatureW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CryptVerifySignature(
            SafeHashHandle hHash,
            byte[] pbSignature,
            int dwSigLen,
            SafeCapiKeyHandle hPubKey,
            string? szDescription,
            CryptSignAndVerifyHashFlags dwFlags);
    }
}
