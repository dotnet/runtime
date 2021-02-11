// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMdCtxCreate")]
        internal static extern SafeEvpMdCtxHandle EvpMdCtxCreate(IntPtr type);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMdCtxDestroy")]
        internal static extern void EvpMdCtxDestroy(IntPtr ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestReset")]
        internal static extern int EvpDigestReset(SafeEvpMdCtxHandle ctx, IntPtr type);

        internal static int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, ReadOnlySpan<byte> d, int cnt) =>
            EvpDigestUpdate(ctx, ref MemoryMarshal.GetReference(d), cnt);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestUpdate")]
        private static extern int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, ref byte d, int cnt);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestFinalEx")]
        internal static extern int EvpDigestFinalEx(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestCurrent")]
        internal static extern int EvpDigestCurrent(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpDigestOneShot")]
        internal static unsafe extern int EvpDigestOneShot(IntPtr type, byte* source, int sourceSize, byte* md, ref uint mdSize);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMdSize")]
        internal static extern int EvpMdSize(IntPtr md);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpMd5")]
        internal static extern IntPtr EvpMd5();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpSha1")]
        internal static extern IntPtr EvpSha1();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpSha256")]
        internal static extern IntPtr EvpSha256();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpSha384")]
        internal static extern IntPtr EvpSha384();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpSha512")]
        internal static extern IntPtr EvpSha512();


        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetMaxMdSize")]
        private static extern int GetMaxMdSize();

        internal static readonly int EVP_MAX_MD_SIZE = GetMaxMdSize();
    }
}
