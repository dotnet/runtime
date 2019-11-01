// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
