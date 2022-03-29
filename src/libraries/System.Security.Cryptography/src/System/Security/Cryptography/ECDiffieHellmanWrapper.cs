// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed class ECDiffieHellmanWrapper : ECDiffieHellman
    {
        private readonly ECDiffieHellman _wrapped;

        internal ECDiffieHellmanWrapper(ECDiffieHellman wrapped)
        {
            Debug.Assert(wrapped != null);
            _wrapped = wrapped;
        }

        public override string KeyExchangeAlgorithm => _wrapped.KeyExchangeAlgorithm;

        public override string? SignatureAlgorithm => _wrapped.SignatureAlgorithm;

        public override ECDiffieHellmanPublicKey PublicKey =>
            new ECDiffieHellmanPublicKeyWrapper(_wrapped.PublicKey);

        public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey) =>
            _wrapped.DeriveKeyMaterial(Unwrap(otherPartyPublicKey));

        public override byte[] DeriveKeyFromHash(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            byte[]? secretPrepend,
            byte[]? secretAppend) =>
            _wrapped.DeriveKeyFromHash(Unwrap(otherPartyPublicKey), hashAlgorithm, secretPrepend, secretAppend);

        public override byte[] DeriveKeyFromHmac(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            byte[]? hmacKey,
            byte[]? secretPrepend,
            byte[]? secretAppend) =>
            _wrapped.DeriveKeyFromHmac(Unwrap(otherPartyPublicKey), hashAlgorithm, hmacKey, secretPrepend, secretAppend);

        public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed) =>
            _wrapped.DeriveKeyTls(Unwrap(otherPartyPublicKey), prfLabel, prfSeed);

        public override void FromXmlString(string xmlString) => _wrapped.FromXmlString(xmlString);

        public override string ToXmlString(bool includePrivateParameters) =>
            _wrapped.ToXmlString(includePrivateParameters);

        public override ECParameters ExportParameters(bool includePrivateParameters) =>
            _wrapped.ExportParameters(includePrivateParameters);

        public override ECParameters ExportExplicitParameters(bool includePrivateParameters) =>
            _wrapped.ExportExplicitParameters(includePrivateParameters);

        public override void ImportParameters(ECParameters parameters) =>
            _wrapped.ImportParameters(parameters);

        public override void GenerateKey(ECCurve curve) => _wrapped.GenerateKey(curve);

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten) =>
            _wrapped.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters, destination, out bytesWritten);

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten) =>
            _wrapped.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten);

        // Do not wrap ExportPkcs8PrivateKey, let it fall back to reconstructing it from parameters
        // so that the ECDiffieHellman.Create()-returned object uses the same set of attributes on all platforms.
        // (CNG adds the key usage attribute to distinguish ECDSA from ECDH)
        //public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten) =>
        //    _wrapped.TryExportPkcs8PrivateKey(destination, out bytesWritten);

        public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead) =>
            _wrapped.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead) =>
            _wrapped.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);

        public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportPkcs8PrivateKey(source, out bytesRead);

        public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportSubjectPublicKeyInfo(source, out bytesRead);

        public override void ImportECPrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportECPrivateKey(source, out bytesRead);

        public override byte[] ExportECPrivateKey() => _wrapped.ExportECPrivateKey();

        public override bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryExportECPrivateKey(destination, out bytesWritten);

        public override void ImportFromPem(ReadOnlySpan<char> input) => _wrapped.ImportFromPem(input);

        public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password) =>
            _wrapped.ImportFromEncryptedPem(input, password);

        public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes) =>
            _wrapped.ImportFromEncryptedPem(input, passwordBytes);

        public override int KeySize
        {
            get => _wrapped.KeySize;
            set => _wrapped.KeySize = value;
        }

        public override KeySizes[] LegalKeySizes => _wrapped.LegalKeySizes;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wrapped.Dispose();
            }
        }

        public override byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters) =>
            _wrapped.ExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters);

        public override byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters) =>
            _wrapped.ExportEncryptedPkcs8PrivateKey(password, pbeParameters);

        // Do not wrap ExportPkcs8PrivateKey, let it fall back to reconstructing it from parameters
        // so that the ECDiffieHellman.Create()-returned object uses the same set of attributes on all platforms.
        // (CNG adds the key usage attribute to distinguish ECDSA from ECDH)
        //public override byte[] ExportPkcs8PrivateKey() => _wrapped.ExportPkcs8PrivateKey();

        public override byte[] ExportSubjectPublicKeyInfo() => _wrapped.ExportSubjectPublicKeyInfo();

        public override bool Equals(object? obj) => _wrapped.Equals(obj);

        public override int GetHashCode() => _wrapped.GetHashCode();

        public override string ToString() => _wrapped.ToString()!;

        private static ECDiffieHellmanPublicKey Unwrap(ECDiffieHellmanPublicKey otherPartyPublicKey)
        {
            if (otherPartyPublicKey is ECDiffieHellmanPublicKeyWrapper wrapper)
            {
                return wrapper.WrappedKey;
            }

            return otherPartyPublicKey;
        }

        private sealed class ECDiffieHellmanPublicKeyWrapper : ECDiffieHellmanPublicKey
        {
            private readonly ECDiffieHellmanPublicKey _wrapped;

            internal ECDiffieHellmanPublicKey WrappedKey => _wrapped;

            internal ECDiffieHellmanPublicKeyWrapper(ECDiffieHellmanPublicKey wrapped)
            {
                Debug.Assert(wrapped != null);
                _wrapped = wrapped;
            }

            public override ECParameters ExportParameters() => _wrapped.ExportParameters();

            public override ECParameters ExportExplicitParameters() => _wrapped.ExportExplicitParameters();

            public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
                _wrapped.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);

            public override byte[] ExportSubjectPublicKeyInfo() =>
                _wrapped.ExportSubjectPublicKeyInfo();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _wrapped.Dispose();
                }
            }

            public override byte[] ToByteArray() => _wrapped.ToByteArray();

#pragma warning disable 0672, SYSLIB0042 // Member overrides an obsolete member, ToXmlString is obsolete.
            public override string ToXmlString() => _wrapped.ToXmlString();
#pragma warning restore 0672, SYSLIB0042

            public override bool Equals(object? obj) => _wrapped.Equals(obj);

            public override int GetHashCode() => _wrapped.GetHashCode();

            public override string ToString() => _wrapped.ToString()!;
        }
    }
}
