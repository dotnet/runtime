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
        [DllImport(Libraries.CryptoNative)]
        private static extern SafeEvpPKeyHandle CryptoNative_EvpPKeyCreateRsa(IntPtr rsa);

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

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeEvpPKeyHandle CryptoNative_RsaGenerateKey(int keySize);

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

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaDecrypt(
            SafeEvpPKeyHandle pkey,
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

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaEncrypt(
            SafeEvpPKeyHandle pkey,
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

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaSignHash(
            SafeEvpPKeyHandle pkey,
            RSASignaturePaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ref byte hash,
            int hashLength,
            ref byte destination,
            int destinationLength);

        internal static int RsaSignHash(
            SafeEvpPKeyHandle pkey,
            RSASignaturePaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ReadOnlySpan<byte> hash,
            Span<byte> destination)
        {
            int written = CryptoNative_RsaSignHash(
                pkey,
                paddingMode,
                digestAlgorithm,
                ref MemoryMarshal.GetReference(hash),
                hash.Length,
                ref MemoryMarshal.GetReference(destination),
                destination.Length);

            if (written < 0)
            {
                Debug.Assert(written == -1);
                throw CreateOpenSslCryptographicException();
            }

            return written;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaVerifyHash(
            SafeEvpPKeyHandle pkey,
            RSASignaturePaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ref byte hash,
            int hashLength,
            ref byte signature,
            int signatureLength);

        internal static bool RsaVerifyHash(
            SafeEvpPKeyHandle pkey,
            RSASignaturePaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature)
        {
            int ret = CryptoNative_RsaVerifyHash(
                pkey,
                paddingMode,
                digestAlgorithm,
                ref MemoryMarshal.GetReference(hash),
                hash.Length,
                ref MemoryMarshal.GetReference(signature),
                signature.Length);

            if (ret == 1)
            {
                return true;
            }

            if (ret == 0)
            {
                return false;
            }

            Debug.Assert(ret == -1);
            throw CreateOpenSslCryptographicException();
        }
    }
}
