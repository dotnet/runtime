// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLKemImplementation : CompositeMLKem
    {
        private static readonly SafeBCryptAlgorithmHandle? s_algHandle = OpenAlgorithmHandle();

        private readonly bool _hasDecapsulationKey;
        private SafeBCryptKeyHandle _key;

        private CompositeMLKemImplementation(
            CompositeMLKemAlgorithm algorithm,
            SafeBCryptKeyHandle key,
            bool hasDecapsulationKey)
            : base(algorithm)
        {
            _key = key;
            _hasDecapsulationKey = hasDecapsulationKey;
        }

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static bool IsSupported => s_algHandle is not null;

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static partial bool IsAlgorithmSupportedImpl(CompositeMLKemAlgorithm algorithm) =>
            IsSupported && PqcBlobHelpers.TryGetCompositeMLKemParameterSet(algorithm, out _);

        internal static partial CompositeMLKem GenerateKeyImpl(CompositeMLKemAlgorithm algorithm)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

            if (!PqcBlobHelpers.TryGetCompositeMLKemParameterSet(algorithm, out string? parameterSet))
            {
                Debug.Fail("Base class should have validated algorithm support.");
                throw new CryptographicException();
            }

            SafeBCryptKeyHandle keyHandle = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, keyLength: 0);

            try
            {
                Interop.BCrypt.BCryptSetSZProperty(
                    keyHandle,
                    Interop.BCrypt.BCryptPropertyStrings.BCRYPT_PARAMETER_SET_NAME,
                    parameterSet);
                Interop.BCrypt.BCryptFinalizeKeyPair(keyHandle);
            }
            catch
            {
                keyHandle.Dispose();
                throw;
            }

            return new CompositeMLKemImplementation(algorithm, keyHandle, hasDecapsulationKey: true);
        }

        internal static partial CompositeMLKem ImportEncapsulationKeyImpl(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));
            Debug.Assert(source.Length == algorithm.MaxEncapsulationKeySizeInBytes);

            return ImportKey(algorithm, source, Interop.BCrypt.KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PUBLIC_BLOB, hasDecapsulationKey: false);
        }

        internal static partial CompositeMLKem ImportDecapsulationKeyImpl(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));
            Debug.Assert(source.Length == algorithm.MaxDecapsulationKeySizeInBytes);

            return ImportKey(algorithm, source, Interop.BCrypt.KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_BLOB, hasDecapsulationKey: true);
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);

            if (!_hasDecapsulationKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            uint written = Interop.BCrypt.BCryptDecapsulate(_key, ciphertext, sharedSecret, 0);

            if (written != (uint)sharedSecret.Length)
            {
                Debug.Fail($"Unexpected number of bytes written by BCryptDecapsulate: {written}.");
                throw new CryptographicException();
            }
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);

            Interop.BCrypt.BCryptEncapsulate(
                _key,
                sharedSecret,
                ciphertext,
                out uint sharedSecretWritten,
                out uint ciphertextWritten,
                0);

            if (sharedSecretWritten != (uint)sharedSecret.Length || ciphertextWritten != (uint)ciphertext.Length)
            {
                Debug.Fail($"Unexpected number of bytes written by BCryptEncapsulate: {sharedSecretWritten} shared secret bytes, {ciphertextWritten} ciphertext bytes.");
                throw new CryptographicException();
            }
        }

        protected override int ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == Algorithm.MaxEncapsulationKeySizeInBytes);

            return ExportKey(Interop.BCrypt.KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PUBLIC_BLOB, destination);
        }

        protected override int ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == Algorithm.MaxDecapsulationKeySizeInBytes);

            if (!_hasDecapsulationKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            return ExportKey(Interop.BCrypt.KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_BLOB, destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            if (!_hasDecapsulationKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            return TryExportPkcs8FromExportedDecapsulationKey(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null!;
            }

            base.Dispose(disposing);
        }

        private static CompositeMLKemImplementation ImportKey(
            CompositeMLKemAlgorithm algorithm,
            ReadOnlySpan<byte> source,
            string blobType,
            bool hasDecapsulationKey)
        {
            Debug.Assert(IsSupported);

            if (!PqcBlobHelpers.TryGetCompositeMLKemParameterSet(algorithm, out string? parameterSet))
            {
                Debug.Fail("Base class should have validated algorithm support.");
                throw new CryptographicException();
            }

            SafeBCryptKeyHandle key = PqcBlobHelpers.EncodeCompositeMLKemBlob(
                parameterSet,
                source,
                blobType,
                s_algHandle,
                static (algHandle, blobKind, blob) => Interop.BCrypt.BCryptImportKeyPair(algHandle, blobKind, blob));

            return new CompositeMLKemImplementation(algorithm, key, hasDecapsulationKey);
        }

        private int ExportKey(string blobType, Span<byte> destination)
        {
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, blobType);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeCompositeMLKemBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string actualBlobType);

                if (!PqcBlobHelpers.TryGetCompositeMLKemParameterSet(Algorithm, out string? expectedParameterSet))
                {
                    Debug.Fail("Unsupported algorithm.");
                    throw new CryptographicException();
                }

                if (actualBlobType != blobType ||
                    keyBytes.Length > destination.Length ||
                    !parameterSet.SequenceEqual(expectedParameterSet))
                {
                    Debug.Fail(
                        $"{nameof(blobType)}: {blobType}, " +
                        $"{nameof(parameterSet)}: {parameterSet.ToString()}, " +
                        $"{nameof(keyBytes)}.Length: {keyBytes.Length} / {destination.Length}");

                    throw new CryptographicException();
                }

                keyBytes.CopyTo(destination);
                return keyBytes.Length;
            }
            finally
            {
                CryptoPool.Return(keyBlob);
            }
        }

        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
            NTSTATUS status = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                out SafeBCryptAlgorithmHandle hAlgorithm,
                BCryptNative.AlgorithmName.CompositeMLKem,
                pszImplementation: null,
                Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hAlgorithm.Dispose();
                return null;
            }

            return hAlgorithm;
        }
    }
}
