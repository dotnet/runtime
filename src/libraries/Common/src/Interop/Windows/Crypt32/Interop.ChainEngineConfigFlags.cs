// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum ChainEngineConfigFlags : int
        {
            CERT_CHAIN_CACHE_END_CERT = 0x00000001,
            CERT_CHAIN_CACHE_ONLY_URL_RETRIEVAL = 0x00000004,
            CERT_CHAIN_USE_LOCAL_MACHINE_STORE = 0x00000008,
            CERT_CHAIN_ENABLE_CACHE_AUTO_UPDATE = 0x00000010,
            CERT_CHAIN_ENABLE_SHARE_STORE = 0x00000020,
            CERT_CHAIN_DISABLE_AIA = 0x00002000,
        }
    }
}
