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
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemDecapsulate")]
        private static partial int CryptoNative_EvpKemDecapsulate(
            SafeEvpPKeyHandle kem,
            ReadOnlySpan<byte> ciphertext,
            int ciphertextLength,
            Span<byte> sharedSecret,
            int sharedSecretLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemGeneratePkey", StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEvpPKeyHandle CryptoNative_EvpKemGeneratePkey(
            string kemName,
            ReadOnlySpan<byte> seed,
            int seedLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemExportPrivateSeed")]
        private static partial int CryptoNative_EvpKemExportPrivateSeed(
            SafeEvpPKeyHandle key,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemExportDecapsulationKey")]
        private static partial int CryptoNative_EvpKemExportDecapsulationKey(
            SafeEvpPKeyHandle key,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemExportEncapsulationKey")]
        private static partial int CryptoNative_EvpKemExportEncapsulationKey(
            SafeEvpPKeyHandle key,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemEncapsulate")]
        private static partial int CryptoNative_EvpKemEncapsulate(
            SafeEvpPKeyHandle kem,
            Span<byte> ciphertext,
            int ciphertextLength,
            Span<byte> sharedSecret,
            int sharedSecretLength);

        internal static SafeEvpPKeyHandle EvpKemGeneratePkey(string kemName)
        {
            SafeEvpPKeyHandle handle = CryptoNative_EvpKemGeneratePkey(kemName, ReadOnlySpan<byte>.Empty, 0);

            if (handle.IsInvalid)
            {
                Exception ex = CreateOpenSslCryptographicException();
                handle.Dispose();
                throw ex;
            }

            return handle;
        }

        internal static SafeEvpPKeyHandle EvpKemGeneratePkey(string kemName, ReadOnlySpan<byte> seed)
        {
            if (seed.IsEmpty)
            {
                Debug.Fail("Generating a key with a seed requires a non-empty seed.");
                throw new CryptographicException();
            }

            SafeEvpPKeyHandle handle = CryptoNative_EvpKemGeneratePkey(kemName, seed, seed.Length);

            if (handle.IsInvalid)
            {
                Exception ex = CreateOpenSslCryptographicException();
                handle.Dispose();
                throw ex;
            }

            return handle;
        }

        internal static void EvpKemDecapsulate(SafeEvpPKeyHandle key, ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            const int Success = 1;
            const int Fail = 0;

            int ret = CryptoNative_EvpKemDecapsulate(key, ciphertext, ciphertext.Length, sharedSecret, sharedSecret.Length);

            switch (ret)
            {
                case Success:
                    return;
                case Fail:
                    sharedSecret.Clear();
                    throw CreateOpenSslCryptographicException();
                default:
                    sharedSecret.Clear();
                    Debug.Fail($"Unexpected return value {ret} from {nameof(CryptoNative_EvpKemDecapsulate)}.");
                    throw new CryptographicException();
            }
        }

        internal static void EvpKemExportPrivateSeed(SafeEvpPKeyHandle key, Span<byte> destination) =>
            ExportKeyContents(key, destination, CryptoNative_EvpKemExportPrivateSeed);

        internal static void EvpKemExportDecapsulationKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            ExportKeyContents(key, destination, CryptoNative_EvpKemExportDecapsulationKey);

        internal static void EvpKemExportEncapsulationKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            ExportKeyContents(key, destination, CryptoNative_EvpKemExportEncapsulationKey);

        internal static void EvpKemEncapsulate(SafeEvpPKeyHandle key, Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            const int Success = 1;
            const int Fail = 0;

            int ret = CryptoNative_EvpKemEncapsulate(key, ciphertext, ciphertext.Length, sharedSecret, sharedSecret.Length);

            switch (ret)
            {
                case Success:
                    return;
                case Fail:
                    ciphertext.Clear();
                    sharedSecret.Clear();
                    throw CreateOpenSslCryptographicException();
                default:
                    ciphertext.Clear();
                    sharedSecret.Clear();
                    Debug.Fail($"Unexpected return value {ret} from {nameof(CryptoNative_EvpKemEncapsulate)}.");
                    throw new CryptographicException();
            }
        }

        private static void ExportKeyContents(
            SafeEvpPKeyHandle key,
            Span<byte> destination,
            Func<SafeEvpPKeyHandle, Span<byte>, int, int> action)
        {
            const int Success = 1;
            const int Fail = 0;
            const int NotRetrievable = -1;

            int ret = action(key, destination, destination.Length);

            switch (ret)
            {
                case Success:
                    return;
                case NotRetrievable:
                    destination.Clear();
                    throw new CryptographicException(SR.Cryptography_NotRetrievable);
                case Fail:
                    destination.Clear();
                    throw CreateOpenSslCryptographicException();
                default:
                    destination.Clear();
                    Debug.Fail($"Unexpected return value {ret}.");
                    throw new CryptographicException();
            }
        }
    }
}
