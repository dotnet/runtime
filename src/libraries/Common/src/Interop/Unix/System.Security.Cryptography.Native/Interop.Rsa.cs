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
        internal static SafeEvpPKeyHandle DecodeRsaSpki(ReadOnlySpan<byte> buf)
        {
            SafeEvpPKeyHandle handle = CryptoNative_DecodeRsaSpki(ref MemoryMarshal.GetReference(buf), buf.Length);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return handle;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeEvpPKeyHandle CryptoNative_DecodeRsaSpki(ref byte buf, int length);

        internal static SafeEvpPKeyHandle DecodeRsaPkcs8(ReadOnlySpan<byte> buf)
        {
            SafeEvpPKeyHandle handle = CryptoNative_DecodeRsaPkcs8(ref MemoryMarshal.GetReference(buf), buf.Length);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return handle;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeEvpPKeyHandle CryptoNative_DecodeRsaPkcs8(ref byte buf, int length);

        internal static int RsaEncrypt(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> data,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            Span<byte> destination) =>
            CryptoNative_RsaEncrypt(
                pkey,
                ref MemoryMarshal.GetReference(data),
                data.Length,
                paddingMode,
                digestAlgorithm,
                ref MemoryMarshal.GetReference(destination));

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaEncrypt(
            SafeEvpPKeyHandle pkey,
            ref byte data,
            int dataLength,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ref byte destination);

        internal static int RsaDecrypt(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> data,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            Span<byte> destination) =>
            CryptoNative_RsaDecrypt(
                pkey,
                ref MemoryMarshal.GetReference(data),
                data.Length,
                paddingMode,
                digestAlgorithm,
                ref MemoryMarshal.GetReference(destination));

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaDecrypt(
            SafeEvpPKeyHandle pkey,
            ref byte data,
            int dataLength,
            RSAEncryptionPaddingMode paddingMode,
            IntPtr digestAlgorithm,
            ref byte destination);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_RsaGenerateKey")]
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

        internal static int RsaSignHash(
            SafeEvpPKeyHandle pkey,
            RSASignaturePaddingMode paddingMode,
            IntPtr digest,
            ReadOnlySpan<byte> hash,
            Span<byte> destination)
        {
            int ret = CryptoNative_RsaSignHash(
                pkey,
                paddingMode,
                digest,
                ref MemoryMarshal.GetReference(hash),
                hash.Length,
                ref MemoryMarshal.GetReference(destination),
                out int bytesWritten);

            if (ret != 1)
            {
                Debug.Assert(ret == 0);
                throw CreateOpenSslCryptographicException();
            }

            return bytesWritten;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaSignHash(
            SafeEvpPKeyHandle pkey,
            RSASignaturePaddingMode paddingMode,
            IntPtr digest,
            ref byte hash,
            int hashLen,
            ref byte dest,
            out int sigLen);

        internal static bool RsaVerifyHash(
            SafeEvpPKeyHandle pkey,
            RSASignaturePaddingMode paddingMode,
            IntPtr digest,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature)
        {
            int ret = CryptoNative_RsaVerifyHash(
                pkey,
                paddingMode,
                digest,
                ref MemoryMarshal.GetReference(hash),
                hash.Length,
                ref MemoryMarshal.GetReference(signature),
                signature.Length);

            if (ret == int.MinValue)
            {
                Debug.Fail("Shim reports API usage error");
                throw new CryptographicException();
            }

            if (ret < 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            Debug.Assert(ret < 2);
            return ret == 1;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_RsaVerifyHash(
            SafeEvpPKeyHandle pkey,
            RSASignaturePaddingMode paddingMode,
            IntPtr digest,
            ref byte hash,
            int hashLen,
            ref byte signature,
            int sigLen);

        internal static SafeBioHandle ExportRSAPublicKey(SafeEvpPKeyHandle pkey)
        {
            SafeBioHandle bio = CryptoNative_ExportRSAPublicKey(pkey);

            if (bio.IsInvalid)
            {
                bio.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return bio;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeBioHandle CryptoNative_ExportRSAPublicKey(SafeEvpPKeyHandle pkey);

        internal static SafeBioHandle ExportRSAPrivateKey(SafeEvpPKeyHandle pkey)
        {
            SafeBioHandle bio = CryptoNative_ExportRSAPrivateKey(pkey);

            if (bio.IsInvalid)
            {
                bio.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return bio;
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern SafeBioHandle CryptoNative_ExportRSAPrivateKey(SafeEvpPKeyHandle pkey);
    }
}
