// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed partial class SlhDsaImplementation : SlhDsa
    {
        private SafeEvpPKeyHandle _key = null!;

        private SlhDsaImplementation(SlhDsaAlgorithm algorithm, SafeEvpPKeyHandle key)
            : base(algorithm)
        {
            Debug.Assert(key is not null);
            Debug.Assert(!key.IsInvalid);
            Debug.Assert(SupportsAny());

            _key = key;
        }

        public SafeEvpPKeyHandle DuplicateHandle() => _key.DuplicateHandle();

        internal static partial bool SupportsAny()
        {
            bool supportsSlhDsaSha2_128s = Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaSha2_128s is not null;

            // Assumption: OpenSSL will either support all of the algorithms or none of them.
            // Currently all OpenSSL versions satisfy this assumption and we expect it to hold in future versions as well.
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaShake128s is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaSha2_128f is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaShake128f is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaSha2_192s is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaShake192s is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaSha2_192f is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaShake192f is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaSha2_256s is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaShake256s is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaSha2_256f is not null);
            Debug.Assert(supportsSlhDsaSha2_128s == Interop.Crypto.EvpPKeySlhDsaAlgs.SlhDsaShake256f is not null);

            return supportsSlhDsaSha2_128s;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null!;
            }
        }

        internal static partial SlhDsaImplementation GenerateKeyCore(SlhDsaAlgorithm algorithm)
        {
            SafeEvpPKeyHandle key = Interop.Crypto.SlhDsaGenerateKey(algorithm.Name);
            return new SlhDsaImplementation(algorithm, key);
        }

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            Interop.Crypto.SlhDsaSignPure(_key, data, context, destination);

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.Crypto.SlhDsaVerifyPure(_key, data, context, signature);

        protected override void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination) =>
            Helpers.SlhDsaPreHash(
                hash,
                context,
                hashAlgorithmOid,
                _key,
                destination,
                static (key, encodedMessage, destination) =>
                {
                    Interop.Crypto.SlhDsaSignPreEncoded(key, encodedMessage, destination);
                    return true;
                });

        protected override bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature) =>
            Helpers.SlhDsaPreHash(
                hash,
                context,
                hashAlgorithmOid,
                _key,
                signature,
                static (key, encodedMessage, signature) => Interop.Crypto.SlhDsaVerifyPreEncoded(key, encodedMessage, signature));

        protected override void ExportSlhDsaPublicKeyCore(Span<byte> destination) =>
            Interop.Crypto.SlhDsaExportPublicKey(_key, destination);

        protected override void ExportSlhDsaSecretKeyCore(Span<byte> destination) =>
            Interop.Crypto.SlhDsaExportSecretKey(_key, destination);

        internal static partial SlhDsaImplementation ImportPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == algorithm.PublicKeySizeInBytes, $"Public key was expected to be {algorithm.PublicKeySizeInBytes} bytes, but was {source.Length} bytes.");
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: false);
            return new SlhDsaImplementation(algorithm, key);
        }

        internal static partial SlhDsaImplementation ImportPkcs8PrivateKeyValue(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial SlhDsaImplementation ImportSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(source.Length == algorithm.SecretKeySizeInBytes, $"Secret key was expected to be {algorithm.SecretKeySizeInBytes} bytes, but was {source.Length} bytes.");
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new SlhDsaImplementation(algorithm, key);
        }
    }
}
