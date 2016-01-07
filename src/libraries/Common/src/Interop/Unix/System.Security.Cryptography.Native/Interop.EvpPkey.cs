// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyCreate")]
        internal static extern SafeEvpPKeyHandle EvpPkeyCreate();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyDestroy")]
        internal static extern void EvpPkeyDestroy(IntPtr pkey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_UpRefEvpPkey")]
        internal static extern int UpRefEvpPkey(SafeEvpPKeyHandle handle);
    }
}
