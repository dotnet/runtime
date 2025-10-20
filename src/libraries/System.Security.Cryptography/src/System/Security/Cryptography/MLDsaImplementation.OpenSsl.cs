// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        private SafeEvpPKeyHandle _key = null!;

        private readonly bool _hasSeed;
        private readonly bool _hasPrivateKey;

        private MLDsaImplementation(
            MLDsaAlgorithm algorithm,
            SafeEvpPKeyHandle key,
            bool hasSeed,
            bool hasPrivateKey)
            : base(algorithm)
        {
            Debug.Assert(key is not null);
            Debug.Assert(!key.IsInvalid);
            Debug.Assert(SupportsAny());

            _key = key;
            _hasSeed = hasSeed;
            _hasPrivateKey = hasPrivateKey;
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

        internal static partial bool SupportsAny() =>
            Interop.Crypto.EvpPKeyMLDsaAlgs.MLDsa44 != null ||
            Interop.Crypto.EvpPKeyMLDsaAlgs.MLDsa65 != null ||
            Interop.Crypto.EvpPKeyMLDsaAlgs.MLDsa87 != null;

        internal static partial bool IsAlgorithmSupported(MLDsaAlgorithm algorithm)
        {
            if (algorithm == MLDsaAlgorithm.MLDsa44)
            {
                return Interop.Crypto.EvpPKeyMLDsaAlgs.MLDsa44 != null;
            }
            else if (algorithm == MLDsaAlgorithm.MLDsa65)
            {
                return Interop.Crypto.EvpPKeyMLDsaAlgs.MLDsa65 != null;
            }
            else if (algorithm == MLDsaAlgorithm.MLDsa87)
            {
                return Interop.Crypto.EvpPKeyMLDsaAlgs.MLDsa87 != null;
            }
            else
            {
                Debug.Fail($"Unexpected algorithm: {algorithm}");
                return false;
            }
        }

        internal SafeEvpPKeyHandle DuplicateHandle()
        {
            return _key.DuplicateHandle();
        }

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            Interop.Crypto.MLDsaSignPure(_key, data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.Crypto.MLDsaVerifyPure(_key, data, context, signature);

        protected override void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination) =>
            Helpers.MLDsaPreHash(
                hash,
                context,
                hashAlgorithmOid,
                _key,
                destination,
                static (key, encodedMessage, destination) =>
                {
                    Interop.Crypto.MLDsaSignPreEncoded(key, encodedMessage, destination);
                    return true;
                });

        protected override bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature) =>
            Helpers.MLDsaPreHash(
                hash,
                context,
                hashAlgorithmOid,
                _key,
                signature,
                static (key, encodedMessage, signature) => Interop.Crypto.MLDsaVerifyPreEncoded(key, encodedMessage, signature));

        protected override void SignMuCore(ReadOnlySpan<byte> externalMu, Span<byte> destination) =>
            Interop.Crypto.MLDsaSignExternalMu(_key, externalMu, destination);

        protected override bool VerifyMuCore(ReadOnlySpan<byte> externalMu, ReadOnlySpan<byte> signature) =>
            Interop.Crypto.MLDsaVerifyExternalMu(_key, externalMu, signature);

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) =>
            Interop.Crypto.MLDsaExportPublicKey(_key, destination);

        protected override void ExportMLDsaPrivateKeyCore(Span<byte> destination)
        {
            if (!_hasPrivateKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            Interop.Crypto.MLDsaExportSecretKey(_key, destination);
        }

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            if (!_hasSeed)
            {
                throw new CryptographicException(SR.Cryptography_PqcNoSeed);
            }

            Interop.Crypto.MLDsaExportSeed(_key, destination);
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

        internal static partial MLDsaImplementation GenerateKeyImpl(MLDsaAlgorithm algorithm)
        {
            SafeEvpPKeyHandle key = Interop.Crypto.MLDsaGenerateKey(algorithm.Name, ReadOnlySpan<byte>.Empty);
            return new MLDsaImplementation(algorithm, key, hasSeed: true, hasPrivateKey: true);
        }

        internal static partial MLDsaImplementation ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == algorithm.PublicKeySizeInBytes, $"Public key was expected to be {algorithm.PublicKeySizeInBytes} bytes, but was {source.Length} bytes.");
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: false);
            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasPrivateKey: false);
        }

        internal static partial MLDsaImplementation ImportPrivateKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == algorithm.PrivateKeySizeInBytes, $"Private key was expected to be {algorithm.PrivateKeySizeInBytes} bytes, but was {source.Length} bytes.");
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new MLDsaImplementation(algorithm, key, hasSeed: false, hasPrivateKey: true);
        }

        internal static partial MLDsaImplementation ImportSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == algorithm.PrivateSeedSizeInBytes, $"Seed was expected to be {algorithm.PrivateSeedSizeInBytes} bytes, but was {source.Length} bytes.");
            SafeEvpPKeyHandle key = Interop.Crypto.MLDsaGenerateKey(algorithm.Name, source);
            return new MLDsaImplementation(algorithm, key, hasSeed: true, hasPrivateKey: true);
        }
    }
}
