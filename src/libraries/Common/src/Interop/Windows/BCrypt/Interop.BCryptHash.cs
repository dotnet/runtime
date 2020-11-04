// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class BCrypt
    {
        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        internal static unsafe extern NTSTATUS BCryptHash(nuint hAlgorithm, byte* pbSecret, int cbSecret, byte* pbInput, int cbInput, byte* pbOutput, int cbOutput);

        // Pseudo-handles, as defined in bcrypt.h
        // TODO: This really should be backed by 'nuint' (see https://github.com/dotnet/roslyn/issues/44651)
        public enum BCryptAlgPseudoHandle : uint
        {
            BCRYPT_MD5_ALG_HANDLE = 0x00000021,
            BCRYPT_SHA1_ALG_HANDLE = 0x00000031,
            BCRYPT_SHA256_ALG_HANDLE = 0x00000041,
            BCRYPT_SHA384_ALG_HANDLE = 0x00000051,
            BCRYPT_SHA512_ALG_HANDLE = 0x00000061,
        }
    }
}
