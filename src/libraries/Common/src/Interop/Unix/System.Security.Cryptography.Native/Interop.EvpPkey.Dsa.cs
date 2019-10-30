// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyGetDsa")]
        internal static extern SafeDsaHandle EvpPkeyGetDsa(SafeEvpPKeyHandle pkey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeySetDsa")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpPkeySetDsa(SafeEvpPKeyHandle pkey, SafeDsaHandle key);
    }
}
