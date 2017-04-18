// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal class DSAOther : DSA
    {
        private readonly DSA _impl;

        internal DSAOther()
        {
            _impl = DSA.Create(1024);
        }

        public override byte[] CreateSignature(byte[] rgbHash) => _impl.CreateSignature(rgbHash);
        public override void ImportParameters(DSAParameters parameters) => _impl.ImportParameters(parameters);

        public override DSAParameters ExportParameters(bool includePrivateParameters) =>
            _impl.ExportParameters(includePrivateParameters);

        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature) =>
            _impl.VerifySignature(rgbHash, rgbSignature);

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm != HashAlgorithmName.SHA1)
                throw new NotSupportedException();

            using (HashAlgorithm alg = SHA1.Create())
            {
                return alg.ComputeHash(data, offset, count);
            }
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            if (hashAlgorithm != HashAlgorithmName.SHA1)
                throw new NotSupportedException();

            using (HashAlgorithm alg = SHA1.Create())
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
