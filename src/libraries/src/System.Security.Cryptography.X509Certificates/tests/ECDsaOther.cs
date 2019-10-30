// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal class ECDsaOther : ECDsa
    {
        private readonly ECDsa _impl;

        internal ECDsaOther()
        {
            _impl = ECDsa.Create();
        }

        public override KeySizes[] LegalKeySizes => _impl.LegalKeySizes;

        public override void GenerateKey(ECCurve curve) => _impl.GenerateKey(curve);
        public override void ImportParameters(ECParameters parameters) => _impl.ImportParameters(parameters);
        public override byte[] SignHash(byte[] hash) => _impl.SignHash(hash);
        public override bool VerifyHash(byte[] hash, byte[] signature) => _impl.VerifyHash(hash, signature);

        public override ECParameters ExportExplicitParameters(bool includePrivateParameters) =>
            _impl.ExportExplicitParameters(includePrivateParameters);

        public override ECParameters ExportParameters(bool includePrivateParameters) =>
            _impl.ExportParameters(includePrivateParameters);

        public override int KeySize
        {
            get { return _impl.KeySize; }
            set { _impl.KeySize = value; }
        }

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            using (HashAlgorithm alg = RSAOther.GetHashAlgorithm(hashAlgorithm))
            {
                return alg.ComputeHash(data, offset, count);
            }
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            using (HashAlgorithm alg = RSAOther.GetHashAlgorithm(hashAlgorithm))
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
    }
}
