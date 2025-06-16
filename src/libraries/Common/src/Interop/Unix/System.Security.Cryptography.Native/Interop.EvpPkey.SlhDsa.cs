// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEvpPKeyHandle CryptoNative_SlhDsaGenerateKey(string keyType);

        internal static SafeEvpPKeyHandle SlhDsaGenerateKey(string algorithmName)
        {
            SafeEvpPKeyHandle handle = CryptoNative_SlhDsaGenerateKey(algorithmName);
            Debug.Assert(handle != null, "handle != null");

            if (handle.IsInvalid)
            {
                Exception ex = Interop.Crypto.CreateOpenSslCryptographicException();
                handle.Dispose();
                throw ex;
            }

            return handle;
        }

        // Must be kept in sync with PalSlhDsaId in native shim.
        internal enum PalSlhDsaAlgorithmId
        {
            Unknown = 0,
            SlhDsaSha2_128s = 1,
            SlhDsaShake128s = 2,
            SlhDsaSha2_128f = 3,
            SlhDsaShake128f = 4,
            SlhDsaSha2_192s = 5,
            SlhDsaShake192s = 6,
            SlhDsaSha2_192f = 7,
            SlhDsaShake192f = 8,
            SlhDsaSha2_256s = 9,
            SlhDsaShake256s = 10,
            SlhDsaSha2_256f = 11,
            SlhDsaShake256f = 12,
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SlhDsaGetPalId")]
        private static partial int CryptoNative_SlhDsaGetPalId(
            SafeEvpPKeyHandle slhDsa,
            out PalSlhDsaAlgorithmId slhDsaId);

        internal static PalSlhDsaAlgorithmId GetSlhDsaAlgorithmId(SafeEvpPKeyHandle key)
        {
            const int Success = 1;
            const int Fail = 0;
            int result = CryptoNative_SlhDsaGetPalId(key, out PalSlhDsaAlgorithmId slhDsaId);

            return result switch
            {
                Success => slhDsaId,
                Fail => throw CreateOpenSslCryptographicException(),
                int other => throw FailThrow(other),
            };

            static Exception FailThrow(int result)
            {
                Debug.Fail($"Unexpected return value {result} from {nameof(CryptoNative_SlhDsaGetPalId)}.");
                return new CryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_SlhDsaSignPure(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> msg, int msgLength,
            ReadOnlySpan<byte> context, int contextLength,
            Span<byte> destination, int destinationLength);

        internal static void SlhDsaSignPure(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> msg,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            int ret = CryptoNative_SlhDsaSignPure(
                pkey, GetExtraHandle(pkey),
                msg, msg.Length,
                context, context.Length,
                destination, destination.Length);

            if (ret != 1)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_SlhDsaVerifyPure(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> msg, int msgLength,
            ReadOnlySpan<byte> context, int contextLength,
            ReadOnlySpan<byte> signature, int signatureLength);

        internal static bool SlhDsaVerifyPure(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> msg,
            ReadOnlySpan<byte> context,
            ReadOnlySpan<byte> signature)
        {
            int ret = CryptoNative_SlhDsaVerifyPure(
                pkey, GetExtraHandle(pkey),
                msg, msg.Length,
                context, context.Length,
                signature, signature.Length);

            if (ret == 1)
            {
                return true;
            }
            else if (ret == 0)
            {
                return false;
            }
            else
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_SlhDsaSignPreEncoded(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> msg, int msgLength,
            Span<byte> destination, int destinationLength);

        internal static void SlhDsaSignPreEncoded(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> msg,
            Span<byte> destination)
        {
            int ret = CryptoNative_SlhDsaSignPreEncoded(
                pkey, GetExtraHandle(pkey),
                msg, msg.Length,
                destination, destination.Length);

            if (ret != 1)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_SlhDsaVerifyPreEncoded(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> msg, int msgLength,
            ReadOnlySpan<byte> signature, int signatureLength);

        internal static bool SlhDsaVerifyPreEncoded(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> msg,
            ReadOnlySpan<byte> signature)
        {
            int ret = CryptoNative_SlhDsaVerifyPreEncoded(
                pkey, GetExtraHandle(pkey),
                msg, msg.Length,
                signature, signature.Length);

            if (ret == 1)
            {
                return true;
            }
            else if (ret == 0)
            {
                return false;
            }
            else
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_SlhDsaExportSecretKey(SafeEvpPKeyHandle pkey, Span<byte> destination, int destinationLength);

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_SlhDsaExportPublicKey(SafeEvpPKeyHandle pkey, Span<byte> destination, int destinationLength);

        internal static void SlhDsaExportSecretKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_SlhDsaExportSecretKey);

        internal static void SlhDsaExportPublicKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_SlhDsaExportPublicKey);
    }
}
