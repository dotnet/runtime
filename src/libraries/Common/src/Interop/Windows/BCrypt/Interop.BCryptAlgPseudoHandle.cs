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
            BCRYPT_PBKDF2_ALG_HANDLE = 0x00000331,
        }

        internal static bool PseudoHandlesSupported { get; } = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 0);
    }
}
