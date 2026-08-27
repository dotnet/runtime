// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
#if NET
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
    internal static partial class Crypt32
    {
        [Flags]
        internal enum CryptImportPublicKeyInfoFlags
        {
            NONE = 0,
            CRYPT_OID_INFO_PUBKEY_ENCRYPT_KEY_FLAG = 0x40000000,
        }
    }
}
