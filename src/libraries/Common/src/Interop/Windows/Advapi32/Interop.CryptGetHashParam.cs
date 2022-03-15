// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal enum CryptHashProperty : int
        {
            HP_ALGID = 0x0001,  // Hash algorithm
            HP_HASHVAL = 0x0002,  // Hash value
            HP_HASHSIZE = 0x0004,  // Hash value size
            HP_HMAC_INFO = 0x0005,  // information for creating an HMAC
            HP_TLS1PRF_LABEL = 0x0006,  // label for TLS1 PRF
            HP_TLS1PRF_SEED = 0x0007,  // seed for TLS1 PRF
        }

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CryptGetHashParam(
            SafeHashHandle hHash,
            CryptHashProperty dwParam,
            out int pbData,
            ref int pdwDataLen,
            int dwFlags);

        [LibraryImport(Libraries.Advapi32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CryptSetHashParam(SafeHashHandle hHash, CryptHashProperty dwParam, byte[] buffer, int dwFlags);
    }
}
