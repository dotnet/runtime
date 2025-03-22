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
        [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
        internal static partial class EvpPKeyMLDsa
        {
            internal static string? MLDsa44 { get; }
            internal static string? MLDsa65 { get; }
            internal static string? MLDsa87 { get; }

            static EvpPKeyMLDsa()
            {
                CryptoInitializer.Initialize();

                // Do not use property initializers for these because we need to ensure CryptoInitializer.Initialize
                // is called first. Property initializers happen before cctors, so instead set the property after the
                // initializer is run.
                MLDsa44 = IsSignatureAlgorithmAvailable(MLDsaAlgorithm.MLDsa44.Name);
                MLDsa65 = IsSignatureAlgorithmAvailable(MLDsaAlgorithm.MLDsa65.Name);
                MLDsa87 = IsSignatureAlgorithmAvailable(MLDsaAlgorithm.MLDsa87.Name);
            }

            [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
            private static partial int CryptoNative_IsSignatureAlgorithmAvailable(string algorithm);

            private static string? IsSignatureAlgorithmAvailable(string algorithm)
            {
                const int Available = 1;
                const int NotAvailable = 0;

                int ret = CryptoNative_IsSignatureAlgorithmAvailable(algorithm);
                return ret switch
                {
                    Available => algorithm,
                    NotAvailable => null,
                    int other => throw Fail(other),
                };

                static CryptographicException Fail(int result)
                {
                    Debug.Fail($"Unexpected result {result} from {nameof(CryptoNative_IsSignatureAlgorithmAvailable)}");
                    return new CryptographicException();
                }
            }

            [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
            private static partial SafeEvpPKeyHandle? CryptoNative_MLDsaGenerateKey(string keyType, ReadOnlySpan<byte> seed, int seedLength);

            public static SafeEvpPKeyHandle MLDsaGenerateKey(string algorithmName, ReadOnlySpan<byte> seed)
            {
                SafeEvpPKeyHandle? handle = CryptoNative_MLDsaGenerateKey(algorithmName, seed, seed.Length);

                if (handle == null || handle.IsInvalid)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return handle;
            }

            [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
            private static partial SafeEvpPKeyHandle? CryptoNative_MLDsaImportSecretKey(string keyType, ReadOnlySpan<byte> sk, int skLength);

            public static SafeEvpPKeyHandle MLDsaImportSecretKey(string algorithmName, ReadOnlySpan<byte> sk)
            {
                SafeEvpPKeyHandle? handle = CryptoNative_MLDsaImportSecretKey(algorithmName, sk, sk.Length);

                if (handle == null || handle.IsInvalid)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return handle;
            }

            [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
            private static partial SafeEvpPKeyHandle? CryptoNative_MLDsaImportPublicKey(string keyType, ReadOnlySpan<byte> pk, int pkLength);

            public static SafeEvpPKeyHandle MLDsaImportPublicKey(string algorithmName, ReadOnlySpan<byte> pk)
            {
                SafeEvpPKeyHandle? handle = CryptoNative_MLDsaImportPublicKey(algorithmName, pk, pk.Length);

                if (handle == null || handle.IsInvalid)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return handle;
            }

            [LibraryImport(Libraries.CryptoNative)]
            private static partial int CryptoNative_MLDsaSignPure(
                SafeEvpPKeyHandle pkey, IntPtr extraHandle,
                ReadOnlySpan<byte> msg, int msgLength,
                ReadOnlySpan<byte> context, int contextLength,
                Span<byte> destination, int destinationLength);

            public static void MLDsaSignPure(
                SafeEvpPKeyHandle pkey,
                ReadOnlySpan<byte> msg,
                ReadOnlySpan<byte> context,
                Span<byte> destination)
            {
                int ret = CryptoNative_MLDsaSignPure(
                    pkey, pkey.ExtraHandle,
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

            public static bool MLDsaVerifyPure(
                SafeEvpPKeyHandle pkey,
                ReadOnlySpan<byte> msg,
                ReadOnlySpan<byte> context,
                ReadOnlySpan<byte> signature)
            {
                int ret = CryptoNative_MLDsaVerifyPure(
                    pkey, pkey.ExtraHandle,
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

            public static void MLDsaExportSecretKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
                Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportSecretKey);

            public static void MLDsaExportSeed(SafeEvpPKeyHandle key, Span<byte> destination) =>
                Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportSeed);

            public static void MLDsaExportPublicKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
                Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportPublicKey);
        }
    }
}
