// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Security.Cryptography.Tests
{
    internal struct RSALease : IDisposable
    {
        private readonly IDisposable _lease;
        public RSA Key { get; private set; }

        internal RSALease(RSA key, IDisposable lease)
        {
            _lease = lease;
            Key = key;
        }

        public void Dispose()
        {
            Key = null!;
            _lease.Dispose();
        }
    }

    internal static partial class RSAKeyPool
    {
        private static readonly KeyPool<RSA, int> s_pool = new KeyPool<RSA, int>();
        
        public static RSALease Rent(int keySize = 2048)
        {
            // If a test is behaving oddly, pooling can be disabled (forcing every
            // potentially-new-key to be an actually-new-key) by defining NO_RENTING.
#if NO_RENTING
            RSA rsa = RSA.Create(keySize);
            return new RSALease(rsa, rsa);
#else
            return ShapeLease(s_pool.Rent(keySize, ks => new IdempotentRSA(ks)));
#endif
        }

        private static RSALease ShapeLease<T>(KeyPool<RSA, T>.KeyPoolLease lease)
        {
            return new RSALease(lease.Key, lease);
        }

        // An RSA wrapper type that does not support having the key replaced or disposed.
        private sealed class IdempotentRSA : RSA
        {
            private readonly RSA _impl;

            internal IdempotentRSA(int keySize)
            {
                _impl = Create(keySize);
                _impl.TryExportRSAPublicKey(Span<byte>.Empty, out _);
            }

            public IdempotentRSA(RSAParameters rsaParameters)
            {
                _impl = Create(rsaParameters);
            }

            public override void FromXmlString(string xmlString) => throw new NotSupportedException();
            public override void ImportParameters(RSAParameters parameters) => throw new NotSupportedException();
            protected override void Dispose(bool disposing) => throw new NotSupportedException();

            public override string? KeyExchangeAlgorithm => _impl.KeyExchangeAlgorithm;

            public override string SignatureAlgorithm => _impl.SignatureAlgorithm;

            public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) => _impl.Decrypt(data, padding);

            public override byte[] DecryptValue(byte[] rgb) => _impl.DecryptValue(rgb);

            public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) => _impl.Encrypt(data, padding);

            public override byte[] EncryptValue(byte[] rgb) => _impl.EncryptValue(rgb);

            public override RSAParameters ExportParameters(bool includePrivateParameters) =>
                _impl.ExportParameters(includePrivateParameters);

            public override byte[] ExportRSAPrivateKey() => _impl.ExportRSAPrivateKey();

            public override byte[] ExportRSAPublicKey() => _impl.ExportRSAPublicKey();

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
            {
                switch (hashAlgorithm.Name)
                {
                    case nameof(HashAlgorithmName.SHA256):
                        return SHA256.HashData(data.AsSpan(offset, count));
                }

                throw new NotSupportedException(hashAlgorithm.Name);
            }

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
            {
                switch (hashAlgorithm.Name)
                {
                    case nameof(HashAlgorithmName.SHA256):
                        return SHA256.HashData(data);
                }

                throw new NotSupportedException(hashAlgorithm.Name);
            }

            protected override bool TryHashData(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                HashAlgorithmName hashAlgorithm,
                out int bytesWritten)
            {
                switch (hashAlgorithm.Name)
                {
                    case nameof(HashAlgorithmName.SHA256):
                        return SHA256.TryHashData(data, destination, out bytesWritten);
                }

                throw new NotSupportedException(hashAlgorithm.Name);
            }

            public override byte[] SignData(
                byte[] data,
                int offset,
                int count,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding) =>
                _impl.SignData(
                    data,
                    offset,
                    count,
                    hashAlgorithm,
                    padding);

            public override byte[] SignData(
                Stream data,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding) =>
                _impl.SignData(
                    data,
                    hashAlgorithm,
                    padding);

            public override byte[] SignHash(
                byte[] hash,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding) =>
                _impl.SignHash(
                    hash,
                    hashAlgorithm,
                    padding);

            public override string ToXmlString(bool includePrivateParameters) =>
                _impl.ToXmlString(includePrivateParameters);

            public override bool TryDecrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten) =>
                _impl.TryDecrypt(
                    data,
                    destination,
                    padding,
                    out bytesWritten);

            public override bool TryEncrypt(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten) =>
                _impl.TryEncrypt(
                    data,
                    destination,
                    padding,
                    out bytesWritten);

            public override bool TryExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                PbeParameters pbeParameters,
                Span<byte> destination,
                out int bytesWritten) =>
                _impl.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters, destination, out bytesWritten);

            public override bool TryExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters,
                Span<byte> destination,
                out int bytesWritten) =>
                _impl.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten);

            public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten) =>
                _impl.TryExportPkcs8PrivateKey(destination, out bytesWritten);

            public override bool TryExportRSAPrivateKey(Span<byte> destination, out int bytesWritten) =>
                _impl.TryExportRSAPrivateKey(destination, out bytesWritten);

            public override bool TryExportRSAPublicKey(Span<byte> destination, out int bytesWritten) =>
                _impl.TryExportRSAPublicKey(destination, out bytesWritten);

            public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
                _impl.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);

            public override bool TrySignData(
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten) =>
                _impl.TrySignData(data, destination, hashAlgorithm, padding, out bytesWritten);

            public override bool TrySignHash(
                ReadOnlySpan<byte> hash,
                Span<byte> destination,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding,
                out int bytesWritten) =>
                _impl.TrySignHash(hash, destination, hashAlgorithm, padding, out bytesWritten);

            public override bool VerifyData(
                byte[] data,
                int offset,
                int count,
                byte[] signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding) =>
                _impl.VerifyData(data, offset, count, signature, hashAlgorithm, padding);

            public override bool VerifyData(
                ReadOnlySpan<byte> data,
                ReadOnlySpan<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding) =>
                _impl.VerifyData(data, signature, hashAlgorithm, padding);

            public override bool VerifyHash(
                byte[] hash,
                byte[] signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding) =>
                _impl.VerifyHash(
                    hash,
                    signature,
                    hashAlgorithm,
                    padding);

            public override bool VerifyHash(
                ReadOnlySpan<byte> hash,
                ReadOnlySpan<byte> signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding) =>
                _impl.VerifyHash(hash, signature, hashAlgorithm, padding);

            public override int KeySize
            {
                get => _impl.KeySize;
                set => throw new NotSupportedException();
            }

            public override KeySizes[] LegalKeySizes => _impl.LegalKeySizes;

            public override byte[] ExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                PbeParameters pbeParameters) =>
                _impl.ExportEncryptedPkcs8PrivateKey(
                    passwordBytes,
                    pbeParameters);

            public override byte[] ExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters) =>
                _impl.ExportEncryptedPkcs8PrivateKey(
                    password,
                    pbeParameters);

            public override byte[] ExportPkcs8PrivateKey() => _impl.ExportPkcs8PrivateKey();

            public override byte[] ExportSubjectPublicKeyInfo() => _impl.ExportSubjectPublicKeyInfo();

            public override bool Equals(object? obj) => _impl.Equals(obj);

            public override int GetHashCode() => _impl.GetHashCode();

            public override string ToString() => _impl.ToString();
        }
    }
}
