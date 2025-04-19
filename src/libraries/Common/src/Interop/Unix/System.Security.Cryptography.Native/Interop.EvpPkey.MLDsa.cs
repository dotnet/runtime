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
            out PalMLDsaAlgorithmId mldsaId);

        internal static PalMLDsaAlgorithmId MLDsaGetPalId(SafeEvpPKeyHandle key)
        {
            const int Success = 1;
            const int Fail = 0;
            int result = CryptoNative_MLDsaGetPalId(key, out PalMLDsaAlgorithmId mldsaId);

            return result switch
            {
                Success => mldsaId,
                Fail => throw CreateOpenSslCryptographicException(),
                int other => throw FailThrow(other),
            };

            static Exception FailThrow(int result)
            {
                Debug.Fail($"Unexpected return value {result} from {nameof(CryptoNative_MLDsaGetPalId)}.");
                return new CryptographicException();
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
