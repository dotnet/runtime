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
        [LibraryImport(Libraries.CryptoNative)]
        private static partial SafeEvpPKeyHandle CryptoNative_EvpPKeyCreateRsa(IntPtr rsa);

        internal static SafeEvpPKeyHandle EvpPKeyCreateRsa(IntPtr rsa)
        {
            Debug.Assert(rsa != IntPtr.Zero);

            SafeEvpPKeyHandle pkey = CryptoNative_EvpPKeyCreateRsa(rsa);

            if (pkey.IsInvalid)
            {
                pkey.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return pkey;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial SafeEvpPKeyHandle CryptoNative_RsaGenerateKey(int keySize);

        internal static SafeEvpPKeyHandle RsaGenerateKey(int keySize)
        {
            SafeEvpPKeyHandle pkey = CryptoNative_RsaGenerateKey(keySize);

            if (pkey.IsInvalid)
            {
                pkey.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return pkey;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_RsaDecrypt(
            SafeEvpPKeyHandle pkey,
            IntPtr extraHandle,
            ref byte source,
            int sourceLength,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ref byte destination,
            int destinationLength);

        internal static int RsaDecrypt(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> source,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            Span<byte> destination)
        {
            int written = CryptoNative_RsaDecrypt(
                pkey,
                pkey.ExtraHandle,
                ref MemoryMarshal.GetReference(source),
                source.Length,
                paddingMode,
                digestAlgorithm,
                ref MemoryMarshal.GetReference(destination),
                destination.Length);

            if (written < 0)
            {
                Debug.Assert(written == -1);
                throw CreateOpenSslCryptographicException();
            }

            return written;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_RsaEncrypt(
            SafeEvpPKeyHandle pkey,
            IntPtr extraHandle,
            ref byte source,
            int sourceLength,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ref byte destination,
            int destinationLength);

        internal static int RsaEncrypt(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> source,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            Span<byte> destination)
        {
            int written = CryptoNative_RsaEncrypt(
                pkey,
                pkey.ExtraHandle,
                ref MemoryMarshal.GetReference(source),
                source.Length,
                paddingMode,
                digestAlgorithm,
                ref MemoryMarshal.GetReference(destination),
                destination.Length);

            if (written < 0)
            {
                Debug.Assert(written == -1);
                throw CreateOpenSslCryptographicException();
            }

            return written;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_EvpPKeyCtxConfigureForRsaSign(
            SafeEvpPKeyCtxHandle ctx,
            RSASignaturePaddingMode paddingMode,
            IntPtr digestAlgorithm);

        internal static void CryptoNative_ConfigureForRsaSign(
            SafeEvpPKeyCtxHandle ctx,
            RSASignaturePaddingMode paddingMode,
            HashAlgorithmName digestAlgorithm)
        {
            if (digestAlgorithm.Name == null)
            {
                throw new ArgumentNullException(nameof(digestAlgorithm));
            }

            IntPtr digestAlgorithmPtr = Interop.Crypto.HashAlgorithmToEvp(digestAlgorithm.Name);
            int ret = CryptoNative_EvpPKeyCtxConfigureForRsaSign(ctx, paddingMode, digestAlgorithmPtr);

            if (ret != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_EvpPKeyCtxConfigureForRsaVerify(
            SafeEvpPKeyCtxHandle ctx,
            RSASignaturePaddingMode paddingMode,
            IntPtr digestAlgorithm);

        internal static void CryptoNative_ConfigureForRsaVerify(
            SafeEvpPKeyCtxHandle ctx,
            RSASignaturePaddingMode paddingMode,
            HashAlgorithmName digestAlgorithm)
        {
            if (digestAlgorithm.Name == null)
            {
                throw new ArgumentNullException(nameof(digestAlgorithm));
            }

            IntPtr digestAlgorithmPtr = Interop.Crypto.HashAlgorithmToEvp(digestAlgorithm.Name);
            int ret = CryptoNative_EvpPKeyCtxConfigureForRsaVerify(ctx, paddingMode, digestAlgorithmPtr);

            if (ret != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }
    }
}
