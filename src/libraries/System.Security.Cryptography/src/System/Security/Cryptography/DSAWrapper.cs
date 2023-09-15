// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed class DSAWrapper : DSA
    {
        private readonly DSA _wrapped;

        internal DSAWrapper(DSA wrapped)
        {
            Debug.Assert(wrapped != null);
            _wrapped = wrapped;
        }

        public override DSAParameters ExportParameters(bool includePrivateParameters) =>
            _wrapped.ExportParameters(includePrivateParameters);

        public override void ImportParameters(DSAParameters parameters) =>
            _wrapped.ImportParameters(parameters);

        public override byte[] CreateSignature(byte[] rgbHash) =>
            _wrapped.CreateSignature(rgbHash);

        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) =>
            _wrapped.VerifySignature(rgbHash, rgbSignature);

        public override byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            _wrapped.SignData(data, offset, count, hashAlgorithm);

        public override byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm) =>
            _wrapped.SignData(data, hashAlgorithm);

        public override bool VerifyData(
            byte[] data,
            int offset,
            int count,
            byte[] signature,
            HashAlgorithmName hashAlgorithm) =>
            _wrapped.VerifyData(data, offset, count, signature, hashAlgorithm);

        public override bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm) =>
            _wrapped.VerifyData(data, signature, hashAlgorithm);

        public override bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryCreateSignature(hash, destination, out bytesWritten);

        public override bool TrySignData(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            out int bytesWritten) =>
            _wrapped.TrySignData(data, destination, hashAlgorithm, out bytesWritten);

        public override bool VerifyData(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm) =>
            _wrapped.VerifyData(data, signature, hashAlgorithm);

        public override bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
            _wrapped.VerifySignature(hash, signature);

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

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryExportPkcs8PrivateKey(destination, out bytesWritten);

        public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead) =>
            _wrapped.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead) =>
            _wrapped.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);

        public override unsafe void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportPkcs8PrivateKey(source, out bytesRead);

        public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportSubjectPublicKeyInfo(source, out bytesRead);

        public override void ImportFromPem(ReadOnlySpan<char> input) => _wrapped.ImportFromPem(input);

        public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password) =>
            _wrapped.ImportFromEncryptedPem(input, password);

        public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes) =>
            _wrapped.ImportFromEncryptedPem(input, passwordBytes);

        public override void FromXmlString(string xmlString) => _wrapped.FromXmlString(xmlString);

        public override string ToXmlString(bool includePrivateParameters) =>
            _wrapped.ToXmlString(includePrivateParameters);

        public override int KeySize
        {
            get => _wrapped.KeySize;
            set => _wrapped.KeySize = value;
        }

        public override KeySizes[] LegalKeySizes => _wrapped.LegalKeySizes;

        public override string? SignatureAlgorithm => _wrapped.SignatureAlgorithm;

        public override string? KeyExchangeAlgorithm => _wrapped.KeyExchangeAlgorithm;

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

        public override byte[] ExportPkcs8PrivateKey() => _wrapped.ExportPkcs8PrivateKey();

        public override byte[] ExportSubjectPublicKeyInfo() => _wrapped.ExportSubjectPublicKeyInfo();

        public override bool Equals(object? obj) => _wrapped.Equals(obj);

        public override int GetHashCode() => _wrapped.GetHashCode();

        public override string ToString() => _wrapped.ToString()!;

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            CryptographicOperations.HashData(hashAlgorithm, new ReadOnlySpan<byte>(data, offset, count));

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
            CryptographicOperations.HashData(hashAlgorithm, data);
    }
}
