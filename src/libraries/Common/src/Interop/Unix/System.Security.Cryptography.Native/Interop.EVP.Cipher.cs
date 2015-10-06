// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        private static extern int GetEvpCipherCtxSize();

        internal static readonly int EVP_CIPHER_CTX_SIZE = GetEvpCipherCtxSize();

        [DllImport(Libraries.CryptoNative)]
        internal static extern void EvpCipherCtxInit(SafeEvpCipherCtxHandle ctx);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpCipherInitEx(
            SafeEvpCipherCtxHandle ctx,
            IntPtr cipher,
            IntPtr engineNull,
            byte[] key,
            byte[] iv,
            int enc);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpCipherCtxSetPadding(SafeEvpCipherCtxHandle x, int padding);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe extern bool EvpCipherUpdate(
            SafeEvpCipherCtxHandle ctx,
            byte* @out,
            out int outl,
            byte* @in,
            int inl);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool EvpCipherFinalEx(
            SafeEvpCipherCtxHandle ctx,
            byte* outm,
            out int outl);

        [DllImport(Libraries.CryptoNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EvpCipherCtxCleanup(IntPtr ctx);

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr EvpAes128Ecb();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr EvpAes128Cbc();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr EvpAes192Ecb();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr EvpAes192Cbc();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr EvpAes256Ecb();

        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr EvpAes256Cbc();
    }
}
