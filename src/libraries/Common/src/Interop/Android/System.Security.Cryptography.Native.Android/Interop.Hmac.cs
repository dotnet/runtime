// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacCreate")]
        internal static extern SafeHmacCtxHandle HmacCreate(ref byte key, int keyLen, IntPtr md);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacDestroy")]
        internal static extern void HmacDestroy(IntPtr ctx);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacReset")]
        internal static extern int HmacReset(SafeHmacCtxHandle ctx);

        internal static int HmacUpdate(SafeHmacCtxHandle ctx, ReadOnlySpan<byte> data, int len) =>
            HmacUpdate(ctx, ref MemoryMarshal.GetReference(data), len);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacUpdate")]
        private static extern int HmacUpdate(SafeHmacCtxHandle ctx, ref byte data, int len);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacFinal")]
        internal static extern int HmacFinal(SafeHmacCtxHandle ctx, ref byte data, ref int len);

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_HmacCurrent")]
        internal static extern int HmacCurrent(SafeHmacCtxHandle ctx, ref byte data, ref int len);
    }
}
