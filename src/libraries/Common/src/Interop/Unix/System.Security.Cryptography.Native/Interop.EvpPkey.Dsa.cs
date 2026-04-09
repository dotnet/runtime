// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyGetDsa")]
        internal static partial SafeDsaHandle EvpPkeyGetDsa(SafeEvpPKeyHandle pkey);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeySetDsa")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvpPkeySetDsa(SafeEvpPKeyHandle pkey, SafeDsaHandle key);
    }
}
