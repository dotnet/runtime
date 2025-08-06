// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

using NTSTATUS = Interop.BCrypt.NTSTATUS;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using KeyBlobType = Interop.BCrypt.KeyBlobType;
using BCRYPT_MLKEM_KEY_BLOB = Interop.BCrypt.BCRYPT_MLKEM_KEY_BLOB;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class MLKemImplementation : MLKem
    {
        private static readonly SafeBCryptAlgorithmHandle? s_algHandle = OpenAlgorithmHandle();
        private readonly bool _hasSeed;
        private readonly bool _hasDecapsulationKey;
        private SafeBCryptKeyHandle _key;

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static new bool IsSupported => s_algHandle is not null;

        private MLKemImplementation(MLKemAlgorithm algorithm, SafeBCryptKeyHandle key, bool hasSeed, bool hasDecapsulationKey)
            : base(algorithm)
        {
            _key = key;
            _hasSeed = hasSeed;
            _hasDecapsulationKey = hasDecapsulationKey;
        }

        internal static MLKemImplementation GenerateKeyImpl(MLKemAlgorithm algorithm)
        {
            Debug.Assert(IsSupported);
            string parameterSet = PqcBlobHelpers.GetMLKemParameterSet(algorithm);
            SafeBCryptKeyHandle keyHandle = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, keyLength: 0);

            try
            {
                Interop.BCrypt.BCryptSetSZProperty(keyHandle, Interop.BCrypt.BCryptPropertyStrings.BCRYPT_PARAMETER_SET_NAME, parameterSet);
                Interop.BCrypt.BCryptFinalizeKeyPair(keyHandle);
            }
            catch
            {
                keyHandle.Dispose();
                throw;
            }

            return new MLKemImplementation(algorithm, keyHandle, hasSeed: true, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportPrivateSeedImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.PrivateSeedSizeInBytes);
            SafeBCryptKeyHandle key = ImportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC, algorithm, source);
            return new MLKemImplementation(algorithm, key, hasSeed: true, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportDecapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.DecapsulationKeySizeInBytes);
            SafeBCryptKeyHandle key = ImportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC, algorithm, source);
            return new MLKemImplementation(algorithm, key, hasSeed: false, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportEncapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.EncapsulationKeySizeInBytes);
            SafeBCryptKeyHandle key = ImportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC, algorithm, source);
            return new MLKemImplementation(algorithm, key, hasSeed: false, hasDecapsulationKey: false);
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);

            ThrowIfNoDecapsulationKey(_hasDecapsulationKey);

            uint written = Interop.BCrypt.BCryptDecapsulate(_key, ciphertext, sharedSecret, 0);
            Debug.Assert(written == (uint)sharedSecret.Length);
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);
            Interop.BCrypt.BCryptEncapsulate(
                _key,
                sharedSecret,
                ciphertext,
                out uint sharedSecretWritten,
                out uint ciphertextWritten,
                0);
            Debug.Assert(sharedSecretWritten == (uint)sharedSecret.Length);
            Debug.Assert(ciphertextWritten == (uint)ciphertext.Length);
        }

        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == Algorithm.PrivateSeedSizeInBytes);

            ThrowIfNoSeed(_hasSeed);
            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC, destination);
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == Algorithm.DecapsulationKeySizeInBytes);

            ThrowIfNoDecapsulationKey(_hasDecapsulationKey);
            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC, destination);
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == Algorithm.EncapsulationKeySizeInBytes);
            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC, destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            return MLKemPkcs8.TryExportPkcs8PrivateKey(
                this,
                _hasSeed,
                _hasDecapsulationKey,
                destination,
                out bytesWritten);
        }

        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
            if (!Helpers.IsOSPlatformWindows)
            {
                return null;
            }

            NTSTATUS status = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                out SafeBCryptAlgorithmHandle hAlgorithm,
                BCryptNative.AlgorithmName.MLKem,
                pszImplementation: null,
                Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hAlgorithm.Dispose();
                return null;
            }
            else
            {
                return hAlgorithm;
            }
        }

        private void ExportKey(KeyBlobMagicNumber kind, Span<byte> destination)
        {
            string blobKind = PqcBlobHelpers.MLKemBlobMagicToBlobType(kind);
            ArraySegment<byte> key = Interop.BCrypt.BCryptExportKey(_key, blobKind);

            try
            {
                ReadCngMLKemBlob(kind, key, destination);
            }
            finally
            {
                // Encapsulation keys are public and don't need to be cleared.
                if (kind == KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC)
                {
                    CryptoPool.Return(key, clearSize: 0);
                }
                else
                {
                    CryptoPool.Return(key);
                }
            }
        }

        private static SafeBCryptKeyHandle ImportKey(KeyBlobMagicNumber kind, MLKemAlgorithm algorithm, ReadOnlySpan<byte> key)
        {
            Debug.Assert(IsSupported);
            return PqcBlobHelpers.EncodeMLKemBlob(
                kind,
                algorithm,
                key,
                s_algHandle,
                static (algHandle, blobKind, blob) => Interop.BCrypt.BCryptImportKeyPair(
                        algHandle,
                        blobKind,
                        blob));
        }
    }
}
