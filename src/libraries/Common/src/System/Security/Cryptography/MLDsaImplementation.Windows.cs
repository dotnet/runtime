// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        private static readonly SafeBCryptAlgorithmHandle s_algHandle =
            Interop.BCrypt.BCryptOpenAlgorithmProvider(BCryptNative.AlgorithmName.MLDsa);

        private readonly bool _hasSeed;
        private readonly bool _hasSecretKey;
        private SafeBCryptKeyHandle _key;

        private MLDsaImplementation(
            MLDsaAlgorithm algorithm,
            SafeBCryptKeyHandle key,
            bool hasSeed,
            bool hasSecretKey)
            : base(algorithm)
        {
            _key = key;
            _hasSeed = hasSeed;
            _hasSecretKey = hasSecretKey;
        }

        internal static partial bool SupportsAny() => !s_algHandle.IsInvalid;

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            Interop.BCrypt.BCryptSignHashPure(_key, data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.BCrypt.BCryptVerifySignaturePure(_key, data, context, signature);

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            return MLDsaPkcs8.TryExportPkcs8PrivateKey(
                this,
                _hasSeed,
                _hasSecretKey,
                destination,
                out bytesWritten);
        }

        internal static partial MLDsaImplementation GenerateKeyImpl(MLDsaAlgorithm algorithm) //=>
        {
            string parameterSet = PqcBlobHelpers.GetParameterSet(algorithm);
            SafeBCryptKeyHandle keyHandle = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle);

            try
            {
                Interop.BCrypt.BCryptSetSZProperty(keyHandle, Interop.BCrypt.BCryptPropertyStrings.BCRYPT_PARAMETER_SET_NAME, parameterSet);
                Interop.BCrypt.BCryptFinalizeKeyPair(keyHandle);
            }
            catch
            {
                keyHandle?.Dispose();
                throw;
            }

            return new MLDsaImplementation(algorithm, keyHandle, hasSeed: true, hasSecretKey: true);
        }

        internal static partial MLDsaImplementation ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            const string BlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetParameterSet(algorithm),
                    source,
                    BlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, BlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasSecretKey: false);
        }

        internal static partial MLDsaImplementation ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            const string BlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetParameterSet(algorithm),
                    source,
                    BlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, BlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasSecretKey: true);
        }

        internal static partial MLDsaImplementation ImportSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            const string BlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetParameterSet(algorithm),
                    source,
                    BlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, BlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: true, hasSecretKey: true);
        }

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination)
        {
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                Debug.Assert(blobType == Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB);

                // Length is known, but we'll slice just in case
                MLDsaAlgorithm algorithm = PqcBlobHelpers.GetMLDsaAlgorithmFromParameterSet(parameterSet);
                Debug.Assert(keyBytes.Length == algorithm.PublicKeySizeInBytes);
                keyBytes.Slice(0, algorithm.PublicKeySizeInBytes).CopyTo(destination);
            }
            finally
            {
                // Public key doesn't need to be cleared
                CryptoPool.Return(keyBlob, clearSize: 0);
            }
        }

        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination)
        {
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                Debug.Assert(blobType == Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB);

                // Length is known, but we'll slice just in case
                MLDsaAlgorithm algorithm = PqcBlobHelpers.GetMLDsaAlgorithmFromParameterSet(parameterSet);
                Debug.Assert(keyBytes.Length == algorithm.SecretKeySizeInBytes);
                keyBytes.Slice(0, algorithm.SecretKeySizeInBytes).CopyTo(destination);
            }
            finally
            {
                CryptoPool.Return(keyBlob, clearSize: keyBlob.Count);
            }
        }

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                Debug.Assert(blobType == Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB);

                // Length is known, but we'll slice just in case
                MLDsaAlgorithm algorithm = PqcBlobHelpers.GetMLDsaAlgorithmFromParameterSet(parameterSet);
                Debug.Assert(keyBytes.Length == algorithm.PrivateSeedSizeInBytes);
                keyBytes.Slice(0, algorithm.PrivateSeedSizeInBytes).CopyTo(destination);
            }
            finally
            {
                CryptoPool.Return(keyBlob, clearSize: keyBlob.Count);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _key?.Dispose();
            _key = null!;
        }
    }
}
