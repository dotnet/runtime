// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdCtxCreate")]
        internal static partial SafeEvpMdCtxHandle EvpMdCtxCreate(IntPtr type);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdCtxDestroy")]
        internal static partial void EvpMdCtxDestroy(IntPtr ctx);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestReset")]
        internal static partial int EvpDigestReset(SafeEvpMdCtxHandle ctx, IntPtr type);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestUpdate")]
        internal static partial int EvpDigestUpdate(SafeEvpMdCtxHandle ctx, ReadOnlySpan<byte> d, int cnt);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestFinalEx")]
        internal static partial int EvpDigestFinalEx(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestCurrent")]
        internal static partial int EvpDigestCurrent(SafeEvpMdCtxHandle ctx, ref byte md, ref uint s);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpDigestOneShot")]
        internal static unsafe partial int EvpDigestOneShot(IntPtr type, byte* source, int sourceSize, byte* md, uint* mdSize);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdSize")]
        internal static partial int EvpMdSize(IntPtr md);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_GetMaxMdSize")]
        private static partial int GetMaxMdSize();

        internal static unsafe int EvpDigestFinalXOF(SafeEvpMdCtxHandle ctx, Span<byte> destination)
        {
            // The partial needs to match the OpenSSL parameters.
            _ = ctx;
            _ = destination;
            Debug.Fail("Should have validated that XOF is not supported before getting here.");
            throw new UnreachableException();
        }

        internal static unsafe int EvpDigestCurrentXOF(SafeEvpMdCtxHandle ctx, Span<byte> destination)
        {
            // The partial needs to match the OpenSSL parameters.
            _ = ctx;
            _ = destination;
            Debug.Fail("Should have validated that XOF is not supported before getting here.");
            throw new UnreachableException();
        }

        internal static readonly int EVP_MAX_MD_SIZE = GetMaxMdSize();
    }
}
