// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        private SafeEvpPKeyHandle? _key;

        private SafeEvpPKeyHandle Key
        {
            get
            {
                ObjectDisposedException.ThrowIf(_key is null, this);

                if (_key.IsInvalid)
                {
                    throw new CryptographicException(SR.Cryptography_InvalidHandle);
                }

                return _key;
            }
        }

        private MLDsaImplementation(MLDsaAlgorithm algorithm, SafeEvpPKeyHandle key)
            : base(algorithm)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_InvalidHandle);
            }

            _key = key;
            ThrowIfNotSupported();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null;
            }

            base.Dispose(disposing);
        }

        internal static partial bool SupportsAny() =>
            Interop.Crypto.EvpPKeyMLDsa.MLDsa44 != null ||
            Interop.Crypto.EvpPKeyMLDsa.MLDsa65 != null ||
            Interop.Crypto.EvpPKeyMLDsa.MLDsa87 != null;

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            Interop.Crypto.EvpPKeyMLDsa.MLDsaSignPure(Key, data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.Crypto.EvpPKeyMLDsa.MLDsaVerifyPure(Key, data, context, signature);

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) =>
            Interop.Crypto.EvpPKeyMLDsa.MLDsaExportPublicKey(Key, destination);

        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination) =>
            Interop.Crypto.EvpPKeyMLDsa.MLDsaExportSecretKey(Key, destination);

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination) =>
            Interop.Crypto.EvpPKeyMLDsa.MLDsaExportSeed(Key, destination);

        internal static partial MLDsa GenerateKey(MLDsaAlgorithm algorithm)
        {
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyMLDsa.MLDsaGenerateKey(algorithm.Name, ReadOnlySpan<byte>.Empty);
            return new MLDsaImplementation(algorithm, key);
        }

        internal static partial MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: false);
            return new MLDsaImplementation(algorithm, key);
        }

        internal static partial MLDsa ImportPkcs8PrivateKeyValue(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial MLDsa ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new MLDsaImplementation(algorithm, key);
        }

        internal static partial MLDsa ImportSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length != 0, "Seed should not be empty, this will cause key generation");
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyMLDsa.MLDsaGenerateKey(algorithm.Name, source);
            return new MLDsaImplementation(algorithm, key);
        }
    }
}
