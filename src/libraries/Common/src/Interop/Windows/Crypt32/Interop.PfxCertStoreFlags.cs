// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum PfxCertStoreFlags : int
        {
            CRYPT_EXPORTABLE                   = 0x00000001,
            CRYPT_USER_PROTECTED               = 0x00000002,
            CRYPT_MACHINE_KEYSET               = 0x00000020,
            CRYPT_USER_KEYSET                  = 0x00001000,
            PKCS12_PREFER_CNG_KSP              = 0x00000100,
            PKCS12_ALWAYS_CNG_KSP              = 0x00000200,
            PKCS12_ALLOW_OVERWRITE_KEY         = 0x00004000,
            PKCS12_NO_PERSIST_KEY              = 0x00008000,
            PKCS12_INCLUDE_EXTENDED_PROPERTIES = 0x00000010,
            None                               = 0x00000000,
        }
    }
}
