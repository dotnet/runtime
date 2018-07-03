// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Primitives.Tests
{
    public static class PbeParametersTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-1000)]
        public static void PositiveIterationsRequired(int iterationCount)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(
                nameof(iterationCount),
                () => new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, iterationCount));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(19)]
        public static void EncryptionAlgorithm_NotVerified(int algId)
        {
            new PbeParameters((PbeEncryptionAlgorithm)algId, HashAlgorithmName.SHA256, 1);
        }

        [Theory]
        [InlineData("Potato")]
        [InlineData("")]
        [InlineData(null)]
        public static void HashAlgorithm_NotVerified(string algId)
        {
            HashAlgorithmName hashName = new HashAlgorithmName(algId);

            // Assert.NoThrows.
            var pbeParams = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, hashName, 1);
            Assert.Equal(hashName, pbeParams.HashAlgorithm);
        }

        [Fact]
        public static void HashAlgorithm_NotVerified_DefaultValue()
        {
            // Assert.NoThrows.
            var pbeParams = new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, default(HashAlgorithmName), 1);

            Assert.Equal(default(HashAlgorithmName), pbeParams.HashAlgorithm);
        }

        [Theory]
        [InlineData("MD5")]
        [InlineData("SHA256")]
        [InlineData("Potato")]
        [InlineData(default)]
        public static void Pkcs12_NotVerifed_InCtor(string hashAlgName)
        {
            HashAlgorithmName hashName = new HashAlgorithmName(hashAlgName);

            // Assert.NoThrows.
            var pbeParams = new PbeParameters(
                PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                hashName,
                1);

            Assert.Equal(hashName, pbeParams.HashAlgorithm);
        }
    }
}
