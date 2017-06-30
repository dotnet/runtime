// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#if netcoreapp
        public override void ImportParameters(ECParameters parameters)
        {
            throw new NotImplementedException();
        }

        public override ECParameters ExportParameters(bool includePrivateParameters)
        {
            throw new NotImplementedException();
        }

        public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
        {
            throw new NotImplementedException();
        }

        public override void GenerateKey(ECCurve curve)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
