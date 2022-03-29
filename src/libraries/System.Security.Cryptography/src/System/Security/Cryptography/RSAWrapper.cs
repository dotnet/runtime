// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Security.Cryptography
{
    internal sealed class RSAWrapper : RSA
    {
        private readonly RSA _wrapped;

        internal RSAWrapper(RSA wrapped)
        {
            Debug.Assert(wrapped != null);
            _wrapped = wrapped;
        }

        public override int KeySize
        {
            get => _wrapped.KeySize;
            set => _wrapped.KeySize = value;
        }

        public override KeySizes[] LegalKeySizes => _wrapped.LegalKeySizes;

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

        public override void FromXmlString(string xmlString) => _wrapped.FromXmlString(xmlString);

        public override string ToXmlString(bool includePrivateParameters) =>
            _wrapped.ToXmlString(includePrivateParameters);

        public override RSAParameters ExportParameters(bool includePrivateParameters) =>
            _wrapped.ExportParameters(includePrivateParameters);

        public override void ImportParameters(RSAParameters parameters) => _wrapped.ImportParameters(parameters);

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) => _wrapped.Encrypt(data, padding);

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) => _wrapped.Decrypt(data, padding);

        public override byte[] SignHash(
            byte[] hash,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding) =>
            _wrapped.SignHash(hash, hashAlgorithm, padding);

        public override bool VerifyHash(
            byte[] hash,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding) =>
            _wrapped.VerifyHash(hash, signature, hashAlgorithm, padding);

        public override bool TryDecrypt(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten) =>
            _wrapped.TryDecrypt(data, destination, padding, out bytesWritten);

        public override bool TryEncrypt(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten) =>
            _wrapped.TryEncrypt(data, destination, padding, out bytesWritten);

        public override bool TrySignHash(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding,
            out int bytesWritten) =>
            _wrapped.TrySignHash(hash, destination, hashAlgorithm, padding, out bytesWritten);

        public override bool VerifyHash(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding) =>
            _wrapped.VerifyHash(hash, signature, hashAlgorithm, padding);

        public override byte[] DecryptValue(byte[] rgb) => _wrapped.DecryptValue(rgb);

        public override byte[] EncryptValue(byte[] rgb) => _wrapped.EncryptValue(rgb);

        public override byte[] SignData(
            byte[] data,
            int offset,
            int count,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding) =>
            _wrapped.SignData(data, offset, count, hashAlgorithm, padding);

        public override byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _wrapped.SignData(data, hashAlgorithm, padding);

        public override bool TrySignData(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding,
            out int bytesWritten) =>
            _wrapped.TrySignData(data, destination, hashAlgorithm, padding, out bytesWritten);

        public override bool VerifyData(
            byte[] data,
            int offset,
            int count,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding) =>
            _wrapped.VerifyData(data, offset, count, signature, hashAlgorithm, padding);

        public override bool VerifyData(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding) =>
            _wrapped.VerifyData(data, signature, hashAlgorithm, padding);

        public override byte[] ExportRSAPrivateKey() => _wrapped.ExportRSAPrivateKey();

        public override bool TryExportRSAPrivateKey(Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryExportRSAPrivateKey(destination, out bytesWritten);

        public override byte[] ExportRSAPublicKey() => _wrapped.ExportRSAPublicKey();

        public override bool TryExportRSAPublicKey(Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryExportRSAPublicKey(destination, out bytesWritten);

        public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten) =>
            _wrapped.TryExportPkcs8PrivateKey(destination, out bytesWritten);

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten) =>
            _wrapped.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten);

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten) =>
            _wrapped.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters, destination, out bytesWritten);

        public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportSubjectPublicKeyInfo(source, out bytesRead);

        public override void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportRSAPublicKey(source, out bytesRead);

        public override void ImportRSAPrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportRSAPrivateKey(source, out bytesRead);

        public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
            _wrapped.ImportPkcs8PrivateKey(source, out bytesRead);

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

        public override void ImportFromPem(ReadOnlySpan<char> input) =>
            _wrapped.ImportFromPem(input);

        public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password) =>
            _wrapped.ImportFromEncryptedPem(input, password);

        public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes) =>
            _wrapped.ImportFromEncryptedPem(input, passwordBytes);

        public override string? KeyExchangeAlgorithm => _wrapped.KeyExchangeAlgorithm;

        public override string SignatureAlgorithm => _wrapped.SignatureAlgorithm;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _wrapped.Dispose();
            }

            base.Dispose(disposing);
        }

        public override bool Equals(object? obj) => _wrapped.Equals(obj);

        public override int GetHashCode() => _wrapped.GetHashCode();

        public override string ToString() => _wrapped.ToString()!;
    }
}
