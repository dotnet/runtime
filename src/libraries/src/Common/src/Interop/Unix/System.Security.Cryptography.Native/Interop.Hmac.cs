// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacCreate")]
        internal extern static unsafe SafeHmacCtxHandle HmacCreate(byte* key, int keyLen, IntPtr md);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacDestroy")]
        internal extern static void HmacDestroy(IntPtr ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacReset")]
        internal extern static int HmacReset(SafeHmacCtxHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacUpdate")]
        internal extern static unsafe int HmacUpdate(SafeHmacCtxHandle ctx, byte* data, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacFinal")]
        internal extern static unsafe int HmacFinal(SafeHmacCtxHandle ctx, byte* data, ref int len);
    }
}
