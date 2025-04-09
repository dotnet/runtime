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

        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEvpPKeyHandle CryptoNative_MLDsaImportSecretKey(string keyType, ReadOnlySpan<byte> sk, int skLength);

        internal static SafeEvpPKeyHandle MLDsaImportSecretKey(string algorithmName, ReadOnlySpan<byte> sk)
        {
            SafeEvpPKeyHandle? handle = CryptoNative_MLDsaImportSecretKey(algorithmName, sk, sk.Length);
            Debug.Assert(handle != null, "handle != null");

            if (handle.IsInvalid)
            {
                Exception ex = Interop.Crypto.CreateOpenSslCryptographicException();
                handle.Dispose();
                throw ex;
            }

            return handle;
        }

        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEvpPKeyHandle CryptoNative_MLDsaImportPublicKey(string keyType, ReadOnlySpan<byte> pk, int pkLength);

        internal static SafeEvpPKeyHandle MLDsaImportPublicKey(string algorithmName, ReadOnlySpan<byte> pk)
        {
            SafeEvpPKeyHandle handle = CryptoNative_MLDsaImportPublicKey(algorithmName, pk, pk.Length);
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

        internal static bool MLDsaVerifyPure(
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

        internal static void MLDsaExportSecretKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportSecretKey);

        internal static void MLDsaExportSeed(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportSeed);

        internal static void MLDsaExportPublicKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_MLDsaExportPublicKey);
    }
}
