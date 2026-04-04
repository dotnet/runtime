// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        private static readonly SafeBCryptAlgorithmHandle? s_algHandle = OpenAlgorithmHandle();

        private readonly bool _hasSeed;
        private readonly bool _hasPrivateKey;
        private SafeBCryptKeyHandle _key;

        private MLDsaImplementation(
            MLDsaAlgorithm algorithm,
            SafeBCryptKeyHandle key,
            bool hasSeed,
            bool hasPrivateKey)
            : base(algorithm)
        {
            _key = key;
            _hasSeed = hasSeed;
            _hasPrivateKey = hasPrivateKey;
        }

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static partial bool SupportsAny() => s_algHandle is not null;

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static partial bool IsAlgorithmSupported(MLDsaAlgorithm algorithm) => SupportsAny();

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            if (!_hasPrivateKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            Interop.BCrypt.BCryptSignHashPqcPure(_key, data, context, destination);
        }

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.BCrypt.BCryptVerifySignaturePqcPure(_key, data, context, signature);

        protected override void SignPreHashCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> context,
            string hashAlgorithmOid,
            Span<byte> destination)
        {
            if (!_hasPrivateKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            string? hashAlgorithmIdentifier = MapHashOidToAlgorithm(
                hashAlgorithmOid,
                out int hashLengthInBytes,
                out bool insufficientCollisionResistance);

            Debug.Assert(hashAlgorithmIdentifier is not null);
            Debug.Assert(!insufficientCollisionResistance);
            Debug.Assert(hashLengthInBytes == hash.Length);

            Interop.BCrypt.BCryptSignHashPqcPreHash(_key, hash, hashAlgorithmIdentifier, context, destination);
        }

        protected override bool VerifyPreHashCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> context,
            string hashAlgorithmOid,
            ReadOnlySpan<byte> signature)
        {
            string? hashAlgorithmIdentifier = MapHashOidToAlgorithm(
                hashAlgorithmOid,
                out int hashLengthInBytes,
                out bool insufficientCollisionResistance);

            Debug.Assert(hashAlgorithmIdentifier is not null);
            Debug.Assert(!insufficientCollisionResistance);
            Debug.Assert(hashLengthInBytes == hash.Length);

            return Interop.BCrypt.BCryptVerifySignaturePqcPreHash(_key, hash, hashAlgorithmIdentifier, context, signature);
        }

        protected override void SignMuCore(ReadOnlySpan<byte> externalMu, Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override bool VerifyMuCore(ReadOnlySpan<byte> externalMu, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        internal static partial MLDsaImplementation GenerateKeyImpl(MLDsaAlgorithm algorithm)
        {
            Debug.Assert(SupportsAny());

            string parameterSet = PqcBlobHelpers.GetMLDsaParameterSet(algorithm);
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

            return new MLDsaImplementation(algorithm, keyHandle, hasSeed: true, hasPrivateKey: true);
        }

        internal static partial MLDsaImplementation ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(SupportsAny());

            const string PublicBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetMLDsaParameterSet(algorithm),
                    source,
                    PublicBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PublicBlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasPrivateKey: false);
        }

        internal static partial MLDsaImplementation ImportPrivateKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(SupportsAny());

            const string PrivateBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetMLDsaParameterSet(algorithm),
                    source,
                    PrivateBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PrivateBlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasPrivateKey: true);
        }

        internal static partial MLDsaImplementation ImportSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(SupportsAny());

            const string PrivateSeedBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetMLDsaParameterSet(algorithm),
                    source,
                    PrivateSeedBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PrivateSeedBlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: true, hasPrivateKey: true);
        }

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) =>
            ExportKey(
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB,
                Algorithm.PublicKeySizeInBytes,
                destination);

        protected override void ExportMLDsaPrivateKeyCore(Span<byte> destination)
        {
            if (!_hasPrivateKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            ExportKey(
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                Algorithm.PrivateKeySizeInBytes,
                destination);
        }

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            if (!_hasSeed)
            {
                throw new CryptographicException(SR.Cryptography_PqcNoSeed);
            }

            ExportKey(
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB,
                Algorithm.PrivateSeedSizeInBytes,
                destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            return MLDsaPkcs8.TryExportPkcs8PrivateKey(
                this,
                _hasSeed,
                _hasPrivateKey,
                destination,
                out bytesWritten);
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

        private void ExportKey(string keyBlobType, int expectedKeySize, Span<byte> destination)
        {
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, keyBlobType);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                string expectedParameterSet = PqcBlobHelpers.GetMLDsaParameterSet(Algorithm);

                if (blobType != keyBlobType ||
                    keyBytes.Length != expectedKeySize ||
                    !parameterSet.SequenceEqual(expectedParameterSet))
                {
                    Debug.Fail(
                        $"{nameof(blobType)}: {blobType}, " +
                        $"{nameof(parameterSet)}: {parameterSet.ToString()}, " +
                        $"{nameof(keyBytes)}.Length: {keyBytes.Length} / {expectedKeySize}");

                    throw new CryptographicException();
                }

                keyBytes.CopyTo(destination);
            }
            finally
            {
                CryptoPool.Return(keyBlob);
            }
        }

        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
            if (!Helpers.IsOSPlatformWindows)
            {
                return null;
            }

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
