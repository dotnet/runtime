// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace System.Security.Cryptography
{
    public sealed class RSACng : RSA
    {
        public RSACng()
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public RSACng(int keySize)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public RSACng(CngKey key)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public CngKey Key
        {
            get { throw new NotImplementedException(SR.WorkInProgress); }
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }
    }
}

