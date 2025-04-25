// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class MLKemAsymmetricAlgorithm : AsymmetricAlgorithm
    {
        private MLKem? _key;

        public MLKemAsymmetricAlgorithm()
        {
        }

        public override int KeySize
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override KeySizes[] LegalKeySizes => throw new NotSupportedException();

        internal MLKem Key
        {
            get
            {
                if (_key is null)
                {
                    Debug.Fail("Should have imported a key before retrieving the key.");
                    throw new CryptographicException();
                }

                return _key;
            }
            set
            {
                MLKem? old = _key;
                _key = value;
                old?.Dispose();
            }
        }

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(passwordBytes, source);
            bytesRead = source.Length;
            Key = kem;
        }

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            MLKem kem = MLKem.ImportEncryptedPkcs8PrivateKey(password, source);
            bytesRead = source.Length;
            Key = kem;
        }

        public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            MLKem kem = MLKem.ImportPkcs8PrivateKey(source);
            bytesRead = source.Length;
            Key = kem;
        }

        public override void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
        {
            MLKem kem = MLKem.ImportSubjectPublicKeyInfo(source);
            bytesRead = source.Length;
            Key = kem;
        }

        public override byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            return Key.ExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters);
        }

        public override byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters)
        {
            return Key.ExportEncryptedPkcs8PrivateKey(password, pbeParameters);
        }

        public override byte[] ExportPkcs8PrivateKey() => Key.ExportPkcs8PrivateKey();
        public override byte[] ExportSubjectPublicKeyInfo() => Key.ExportSubjectPublicKeyInfo();

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            return Key.TryExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters, destination, out bytesWritten);
        }

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            return Key.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten);
        }

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            return Key.TryExportPkcs8PrivateKey(destination, out bytesWritten);
        }

        public override bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            return Key.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _key is not null)
            {
                _key.Dispose();
                _key = null;
            }
        }
    }
}
