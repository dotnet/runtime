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
        // Must be kept in sync with PalMLDsaId in native shim.
        internal enum PalMLDsaAlgorithmId
        {
            Unknown = 0,
            MLDsa44 = 1,
            MLDsa65 = 2,
            MLDsa87 = 3,
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_MLDsaGetPalId(
            SafeEvpPKeyHandle mldsa,
            out PalMLDsaAlgorithmId mldsaId,
            out int hasSeed,
            out int hasSecretKey);

        internal static PalMLDsaAlgorithmId MLDsaGetPalId(
            SafeEvpPKeyHandle key,
            out bool hasSeed,
            out bool hasSecretKey)
        {
            const int Success = 1;
            const int Yes = 1;
            const int Fail = 0;
            int result = CryptoNative_MLDsaGetPalId(
                key,
                out PalMLDsaAlgorithmId mldsaId,
                out int pKeyHasSeed,
                out int pKeyHasSecretKey);

            switch (result)
            {
                case Success:
                    hasSeed = pKeyHasSeed == Yes;
                    hasSecretKey = pKeyHasSecretKey == Yes;
                    return mldsaId;
                case Fail:
                    throw CreateOpenSslCryptographicException();
                default:
                    Debug.Fail($"Unexpected return value {result} from {nameof(CryptoNative_MLDsaGetPalId)}.");
                    throw new CryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEvpPKeyHandle CryptoNative_MLDsaGenerateKey(string keyType, ReadOnlySpan<byte> seed, int seedLength);

        internal static SafeEvpPKeyHandle MLDsaGenerateKey(string algorithmName, ReadOnlySpan<byte> seed)
        {
            SafeEvpPKeyHandle handle = CryptoNative_MLDsaGenerateKey(algorithmName, seed, seed.Length);
            Debug.Assert(handle != null, "handle != null");

            if (handle.IsInvalid)
            {
                Exception ex = Interop.Crypto.CreateOpenSslCryptographicException();
                handle.Dispose();
                throw ex;
            }

            return handle;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_MLDsaSignPure(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> msg, int msgLength,
            ReadOnlySpan<byte> context, int contextLength,
            Span<byte> destination, int destinationLength);

        internal static void MLDsaSignPure(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> msg,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            int ret = CryptoNative_MLDsaSignPure(
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
        private static partial int CryptoNative_MLDsaVerifyPure(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> msg, int msgLength,
            ReadOnlySpan<byte> context, int contextLength,
            ReadOnlySpan<byte> signature, int signatureLength);

        internal static bool MLDsaVerifyPure(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> msg,
            ReadOnlySpan<byte> context,
            ReadOnlySpan<byte> signature)
        {
            int ret = CryptoNative_MLDsaVerifyPure(
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
        private static partial int CryptoNative_MLDsaSignPreEncoded(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> msg, int msgLength,
            Span<byte> destination, int destinationLength);

        internal static void MLDsaSignPreEncoded(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> msg,
            Span<byte> destination)
        {
            int ret = CryptoNative_MLDsaSignPreEncoded(
                pkey, GetExtraHandle(pkey),
                msg, msg.Length,
                destination, destination.Length);

            if (ret != 1)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_MLDsaVerifyPreEncoded(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> msg, int msgLength,
            ReadOnlySpan<byte> signature, int signatureLength);

        internal static bool MLDsaVerifyPreEncoded(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> msg,
            ReadOnlySpan<byte> signature)
        {
            int ret = CryptoNative_MLDsaVerifyPreEncoded(
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
        private static partial int CryptoNative_MLDsaSignExternalMu(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> mu, int muLength,
            Span<byte> destination, int destinationLength);

        internal static void MLDsaSignExternalMu(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> mu,
            Span<byte> destination)
        {
            const int Success = 1;
            const int SigningFailure = 0;

            int ret = CryptoNative_MLDsaSignExternalMu(
                pkey, GetExtraHandle(pkey),
                mu, mu.Length,
                destination, destination.Length);

            if (ret != Success)
            {
                Debug.Assert(ret == SigningFailure, $"Unexpected return value {ret} from {nameof(CryptoNative_MLDsaSignExternalMu)}.");
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_MLDsaVerifyExternalMu(
            SafeEvpPKeyHandle pkey, IntPtr extraHandle,
            ReadOnlySpan<byte> mu, int muLength,
            ReadOnlySpan<byte> signature, int signatureLength);

        internal static bool MLDsaVerifyExternalMu(
            SafeEvpPKeyHandle pkey,
            ReadOnlySpan<byte> mu,
            ReadOnlySpan<byte> signature)
        {
            const int ValidSignature = 1;
            const int InvalidSignature = 0;

            int ret = CryptoNative_MLDsaVerifyExternalMu(
                pkey, GetExtraHandle(pkey),
                mu, mu.Length,
                signature, signature.Length);

            if (ret == ValidSignature)
            {
                return true;
            }
            else if (ret == InvalidSignature)
            {
                return false;
            }
            else
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_MLDsaExportSecretKey(SafeEvpPKeyHandle pkey, Span<byte> destination, int destinationLength);

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_MLDsaExportSeed(SafeEvpPKeyHandle pkey, Span<byte> destination, int destinationLength);

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_MLDsaExportPublicKey(SafeEvpPKeyHandle pkey, Span<byte> destination, int destinationLength);

        internal static void MLDsaExportSecretKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportSecretKey);

        internal static void MLDsaExportSeed(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportSeed);

        internal static void MLDsaExportPublicKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportPublicKey);
    }
}
