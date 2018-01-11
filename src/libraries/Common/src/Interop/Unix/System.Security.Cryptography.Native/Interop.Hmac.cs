// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacCreate")]
        internal extern static SafeHmacCtxHandle HmacCreate(ref byte key, int keyLen, IntPtr md);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacDestroy")]
        internal extern static void HmacDestroy(IntPtr ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacReset")]
        internal extern static int HmacReset(SafeHmacCtxHandle ctx);

        internal static int HmacUpdate(SafeHmacCtxHandle ctx, ReadOnlySpan<byte> data, int len) =>
            HmacUpdate(ctx, ref MemoryMarshal.GetReference(data), len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacUpdate")]
        private extern static int HmacUpdate(SafeHmacCtxHandle ctx, ref byte data, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_HmacFinal")]
        internal extern static int HmacFinal(SafeHmacCtxHandle ctx, ref byte data, ref int len);
    }
}
