// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        private static readonly SafeBCryptAlgorithmHandle? s_algHandle = OpenAlgorithmHandle();

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

        internal static partial bool SupportsAny() => s_algHandle is not null;

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            Interop.BCrypt.BCryptSignHashPqcPure(_key, data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.BCrypt.BCryptVerifySignaturePqcPure(_key, data, context, signature);

        internal static partial MLDsaImplementation GenerateKeyImpl(MLDsaAlgorithm algorithm)
        {
            Debug.Assert(s_algHandle is not null, $"Check {nameof(SupportsAny)}() before calling.");

            string parameterSet = PqcBlobHelpers.GetParameterSet(algorithm);
            SafeBCryptKeyHandle keyHandle = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, keyLength: 0);

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
            Debug.Assert(s_algHandle is not null, $"Check {nameof(SupportsAny)}() before calling.");

            const string PublicBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetParameterSet(algorithm),
                    source,
                    PublicBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PublicBlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasSecretKey: false);
        }

        internal static partial MLDsaImplementation ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(s_algHandle is not null, $"Check {nameof(SupportsAny)}() before calling.");

            const string PrivateBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetParameterSet(algorithm),
                    source,
                    PrivateBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PrivateBlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasSecretKey: true);
        }

        internal static partial MLDsaImplementation ImportSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(s_algHandle is not null, $"Check {nameof(SupportsAny)}() before calling.");

            const string PrivateSeedBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetParameterSet(algorithm),
                    source,
                    PrivateSeedBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PrivateSeedBlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: true, hasSecretKey: true);
        }

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination)
        {
            const string PublicBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB;
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, PublicBlobType);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                MLDsaAlgorithm algorithm = PqcBlobHelpers.GetMLDsaAlgorithmFromParameterSet(parameterSet);

                if (blobType != PublicBlobType || keyBytes.Length != algorithm.PublicKeySizeInBytes)
                {
                    Debug.Fail($"blobType: {blobType}, keyBytes.Length: {keyBytes.Length} / {algorithm.PublicKeySizeInBytes}");
                    throw new CryptographicException();
                }

                keyBytes.CopyTo(destination);
            }
            finally
            {
                // Public key doesn't need to be cleared
                CryptoPool.Return(keyBlob, clearSize: 0);
            }
        }

        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination)
        {
            const string PrivateBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB;
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, PrivateBlobType);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                MLDsaAlgorithm algorithm = PqcBlobHelpers.GetMLDsaAlgorithmFromParameterSet(parameterSet);

                if (blobType != PrivateBlobType || keyBytes.Length != algorithm.SecretKeySizeInBytes)
                {
                    Debug.Fail($"blobType: {blobType}, keyBytes.Length: {keyBytes.Length} / {algorithm.SecretKeySizeInBytes}");
                    throw new CryptographicException();
                }

                keyBytes.CopyTo(destination);
            }
            finally
            {
                CryptoPool.Return(keyBlob);
            }
        }

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            const string PrivateSeedBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB;
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, PrivateSeedBlobType);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                MLDsaAlgorithm algorithm = PqcBlobHelpers.GetMLDsaAlgorithmFromParameterSet(parameterSet);

                if (blobType != PrivateSeedBlobType || keyBytes.Length != algorithm.PrivateSeedSizeInBytes)
                {
                    Debug.Fail($"blobType: {blobType}, keyBytes.Length: {keyBytes.Length} / {algorithm.PrivateSeedSizeInBytes}");
                    throw new CryptographicException();
                }

                keyBytes.CopyTo(destination);
            }
            finally
            {
                CryptoPool.Return(keyBlob);
            }
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            return MLDsaPkcs8.TryExportPkcs8PrivateKey(
                this,
                _hasSeed,
                _hasSecretKey,
                destination,
                out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            _key?.Dispose();
            _key = null!;
        }

        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
            NTSTATUS status = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                out SafeBCryptAlgorithmHandle hAlgorithm,
                BCryptNative.AlgorithmName.MLDsa,
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
    }
}
