// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public static partial class ShimHelpers
    {
        public static void TestSymmetricAlgorithmProperties(SymmetricAlgorithm alg, int blockSize, int keySize, byte[] key = null)
        {
            alg.BlockSize = blockSize;
            Assert.Equal(blockSize, alg.BlockSize);

            var emptyIV = new byte[alg.BlockSize / 8];
            alg.IV = emptyIV;
            Assert.Equal(emptyIV, alg.IV);
            alg.GenerateIV();
            Assert.NotEqual(emptyIV, alg.IV);

            if (key == null)
            {
                key = new byte[alg.KeySize / 8];
            }
            alg.Key = key;
            Assert.Equal(key, alg.Key);
            Assert.NotSame(key, alg.Key);
            alg.GenerateKey();
            Assert.NotEqual(key, alg.Key);

            alg.KeySize = keySize;
            Assert.Equal(keySize, alg.KeySize);

            alg.Mode = CipherMode.ECB;
            Assert.Equal(CipherMode.ECB, alg.Mode);

            alg.Padding = PaddingMode.PKCS7;
            Assert.Equal(PaddingMode.PKCS7, alg.Padding);
        }
    }
}
