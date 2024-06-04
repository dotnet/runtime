// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        // Pseudo-handles, as defined in bcrypt.h
        // TODO: This really should be backed by 'nuint' (see https://github.com/dotnet/roslyn/issues/44110)
        public enum BCryptAlgPseudoHandle : uint
        {
            BCRYPT_MD5_ALG_HANDLE = 0x00000021,
            BCRYPT_SHA1_ALG_HANDLE = 0x00000031,
            BCRYPT_SHA256_ALG_HANDLE = 0x00000041,
            BCRYPT_SHA384_ALG_HANDLE = 0x00000051,
            BCRYPT_SHA512_ALG_HANDLE = 0x00000061,
            BCRYPT_HMAC_MD5_ALG_HANDLE = 0x00000091,
            BCRYPT_HMAC_SHA1_ALG_HANDLE = 0x000000a1,
            BCRYPT_HMAC_SHA256_ALG_HANDLE = 0x000000b1,
            BCRYPT_HMAC_SHA384_ALG_HANDLE = 0x000000c1,
            BCRYPT_HMAC_SHA512_ALG_HANDLE = 0x000000d1,
            BCRYPT_PBKDF2_ALG_HANDLE = 0x00000331,
            BCRYPT_SHA3_256_ALG_HANDLE = 0x000003B1,
            BCRYPT_SHA3_384_ALG_HANDLE = 0x000003C1,
            BCRYPT_SHA3_512_ALG_HANDLE = 0x000003D1,
            BCRYPT_HMAC_SHA3_256_ALG_HANDLE = 0x000003E1,
            BCRYPT_HMAC_SHA3_384_ALG_HANDLE = 0x000003F1,
            BCRYPT_HMAC_SHA3_512_ALG_HANDLE = 0x00000401,
            BCRYPT_CSHAKE128_ALG_HANDLE = 0x00000411,
            BCRYPT_CSHAKE256_ALG_HANDLE = 0x00000421,
            BCRYPT_KMAC128_ALG_HANDLE = 0x00000431,
            BCRYPT_KMAC256_ALG_HANDLE = 0x00000441,
        }

        internal static bool PseudoHandlesSupported { get; } =
#if NET
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 0);
#elif NETSTANDARD2_0_OR_GREATER
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version.Major >= 10;
#elif NETFRAMEWORK
            Environment.OSVersion.Version.Major >= 10;
#else
#error Unhandled platform targets
#endif
    }
}
