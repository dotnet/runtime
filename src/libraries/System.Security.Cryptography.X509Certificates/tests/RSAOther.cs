// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal class RSAOther : RSA
    {
        private readonly RSA _impl;

        internal RSAOther()
        {
            _impl = RSA.Create();
        }

        public override KeySizes[] LegalKeySizes => _impl.LegalKeySizes;

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) => _impl.Decrypt(data, padding);
        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) => _impl.Encrypt(data, padding);
        public override RSAParameters ExportParameters(bool includePrivateParameters) => _impl.ExportParameters(includePrivateParameters);
        public override void ImportParameters(RSAParameters parameters) => _impl.ImportParameters(parameters);

        public override byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.SignData(data, offset, count, hashAlgorithm, padding);

        public override byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.SignData(data, hashAlgorithm, padding);

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.SignHash(hash, hashAlgorithm, padding);

        public override bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.VerifyData(data, offset, count, signature, hashAlgorithm, padding);

        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            _impl.VerifyHash(hash, signature, hashAlgorithm, padding);

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            using (HashAlgorithm alg = GetHashAlgorithm(hashAlgorithm))
            {
                return alg.ComputeHash(data, offset, count);
            }
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            using (HashAlgorithm alg = GetHashAlgorithm(hashAlgorithm))
            {
                return alg.ComputeHash(data);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _impl.Dispose();
            }

            base.Dispose(disposing);
        }

        internal static HashAlgorithm GetHashAlgorithm(HashAlgorithmName hashAlgorithmName)
        {
            HashAlgorithm hasher;

            if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                hasher = MD5.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                hasher = SHA1.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                hasher = SHA256.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                hasher = SHA384.Create();
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                hasher = SHA512.Create();
            }
            else
            {
                throw new NotSupportedException();
            }

            return hasher;
        }
    }
}
