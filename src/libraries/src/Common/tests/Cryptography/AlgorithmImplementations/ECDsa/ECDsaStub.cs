// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Cryptography;

namespace System.Security.Cryptography.EcDsa.Tests
{
    // Stub out the last remaining abstract members to throw NotImplementedException
    internal class ECDsaStub : ECDsa
    {
        public override byte[] SignHash(byte[] hash)
        {
            return Array.Empty<byte>();
        }

        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            return false;
        }

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            throw new NotImplementedException();
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            throw new NotImplementedException();
        }
    }
}
