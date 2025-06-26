// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static partial bool SupportsAny() => s_algHandle is not null;

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            Interop.BCrypt.BCryptSignHashPqcPure(_key, data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.BCrypt.BCryptVerifySignaturePqcPure(_key, data, context, signature);

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

            return new MLDsaImplementation(algorithm, keyHandle, hasSeed: true, hasSecretKey: true);
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

            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasSecretKey: false);
        }

        internal static partial MLDsaImplementation ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(SupportsAny());

            const string PrivateBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeMLDsaBlob(
                    PqcBlobHelpers.GetMLDsaParameterSet(algorithm),
                    source,
                    PrivateBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PrivateBlobType, blob));

            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasSecretKey: true);
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

            return new MLDsaImplementation(algorithm, key, hasSeed: true, hasSecretKey: true);
        }

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) =>
            ExportKey(
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB,
                Algorithm.PublicKeySizeInBytes,
                destination);

        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination)
        {
            if (!_hasSecretKey)
            {
                throw new CryptographicException(SR.Cryptography_MLDsaNoSecretKey);
            }

            ExportKey(
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                Algorithm.SecretKeySizeInBytes,
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
                _hasSecretKey,
                destination,
                out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            _key?.Dispose();
            _key = null!;
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
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }
#endif

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
