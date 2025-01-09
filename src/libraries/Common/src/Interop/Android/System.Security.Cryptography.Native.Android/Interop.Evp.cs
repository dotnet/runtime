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

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_Pbkdf2", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int Pbkdf2(
            string algorithmName,
            ReadOnlySpan<byte> pPassword,
            int passwordLength,
            ReadOnlySpan<byte> pSalt,
            int saltLength,
            int iterations,
            Span<byte> pDestination,
            int destinationLength);

        internal static void Pbkdf2(
            string algorithmName,
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            Span<byte> destination)
        {
            const int Success = 1;
            const int UnsupportedAlgorithm = -1;
            const int Failed = 0;

            int result = Pbkdf2(
                algorithmName,
                password,
                password.Length,
                salt,
                salt.Length,
                iterations,
                destination,
                destination.Length);

            switch (result)
            {
                case Success:
                    return;
                case UnsupportedAlgorithm:
                    throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, algorithmName));
                case Failed:
                    throw new CryptographicException();
                default:
                    Debug.Fail($"Unexpected result {result}");
                    throw new CryptographicException();
            }
        }

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

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EvpMdCtxCopyEx")]
        internal static partial SafeEvpMdCtxHandle EvpMdCtxCopyEx(SafeEvpMdCtxHandle ctx);

        internal static int EvpDigestSqueeze(SafeEvpMdCtxHandle ctx, Span<byte> destination)
        {
            _ = ctx;
            _ = destination;
            Debug.Fail("Should have validated that XOF is not supported before getting here.");
            throw new UnreachableException();
        }

        internal static readonly int EVP_MAX_MD_SIZE = GetMaxMdSize();
    }
}
