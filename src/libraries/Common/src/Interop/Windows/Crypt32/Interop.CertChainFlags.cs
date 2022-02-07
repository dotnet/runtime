// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum CertChainFlags : int
        {
            None                                           = 0x00000000,
            CERT_CHAIN_DISABLE_AUTH_ROOT_AUTO_UPDATE       = 0x00000100,
            CERT_CHAIN_DISABLE_AIA                         = 0x00002000,
            CERT_CHAIN_REVOCATION_CHECK_END_CERT           = 0x10000000,
            CERT_CHAIN_REVOCATION_CHECK_CHAIN              = 0x20000000,
            CERT_CHAIN_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x40000000,
            CERT_CHAIN_REVOCATION_CHECK_CACHE_ONLY         = unchecked((int)0x80000000),
        }
    }
}
