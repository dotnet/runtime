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
        /// <summary>
        /// Gets the extra handle associated with the EVP_PKEY. Some tests need to access
        /// the interop layer and achieve this by adding the relevant classes to the test
        /// project as links. However, accesses to internal members like <see cref="SafeEvpPKeyHandle.ExtraHandle"/>
        /// in the product project will not work in the test project. In this particular case,
        /// the test project does not need the value of the handle, so it can implement this
        /// method to return a null pointer.
        /// </summary>
        /// <param name="handle">
        ///  The extra handle associated with the EVP_PKEY.</param>
        /// <returns>
        ///  The extra handle associated with the EVP_PKEY.
        /// </returns>
        private static partial IntPtr GetExtraHandle(SafeEvpPKeyHandle handle);

        // Must be kept in sync with PalKemId in native shim.
        internal enum PalKemAlgorithmId
        {
            Unknown = 0,
            MLKem512 = 1,
            MLKem768 = 2,
            MLKem1024 = 3,
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemDecapsulate")]
        private static partial int CryptoNative_EvpKemDecapsulate(
            SafeEvpPKeyHandle kem,
            IntPtr extraHandle,
            ReadOnlySpan<byte> ciphertext,
            int ciphertextLength,
            Span<byte> sharedSecret,
            int sharedSecretLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpKemGetPalId")]
        private static partial int CryptoNative_EvpKemGetPalId(
            SafeEvpPKeyHandle kem,
            out PalKemAlgorithmId kemId,
            out int hasSeed,
            out int hasDecapsulationKey);

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
            IntPtr extraHandle,
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

        internal static PalKemAlgorithmId EvpKemGetKemIdentifier(
            SafeEvpPKeyHandle key,
            out bool hasSeed,
            out bool hasDecapsulationKey)
        {
            const int Success = 1;
            const int Yes = 1;
            const int Fail = 0;
            int result = CryptoNative_EvpKemGetPalId(
                key,
                out PalKemAlgorithmId kemId,
                out int pKeyHasSeed,
                out int pKeyHasDecapsulationKey);

            switch (result)
            {
                case Success:
                    hasSeed = pKeyHasSeed == Yes;
                    hasDecapsulationKey = pKeyHasDecapsulationKey == Yes;
                    return kemId;
                case Fail:
                    throw CreateOpenSslCryptographicException();
                default:
                    Debug.Fail($"Unexpected return value {result} from {nameof(CryptoNative_EvpKemGetPalId)}.");
                    throw new CryptographicException();
            }
        }

        internal static void EvpKemDecapsulate(SafeEvpPKeyHandle key, ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            const int Success = 1;
            const int Fail = 0;

            int ret = CryptoNative_EvpKemDecapsulate(key, GetExtraHandle(key), ciphertext, ciphertext.Length, sharedSecret, sharedSecret.Length);

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
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_EvpKemExportPrivateSeed);

        internal static void EvpKemExportDecapsulationKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_EvpKemExportDecapsulationKey);

        internal static void EvpKemExportEncapsulationKey(SafeEvpPKeyHandle key, Span<byte> destination) =>
            Interop.Crypto.ExportKeyContents(key, destination, CryptoNative_EvpKemExportEncapsulationKey);

        internal static void EvpKemEncapsulate(SafeEvpPKeyHandle key, Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            const int Success = 1;
            const int Fail = 0;

            int ret = CryptoNative_EvpKemEncapsulate(key, GetExtraHandle(key), ciphertext, ciphertext.Length, sharedSecret, sharedSecret.Length);

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
    }
}
