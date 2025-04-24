// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public static class SlhDsaAlgorithmTests
    {
        [Fact]
        public static void AlgorithmsHaveExpectedParameters()
        {
            SlhDsaAlgorithm algorithm;

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_128s;
            Assert.Equal("SLH-DSA-SHA2-128s", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.SecretKeySizeInBytes);
            Assert.Equal(7856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake128s;
            Assert.Equal("SLH-DSA-SHAKE-128s", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.SecretKeySizeInBytes);
            Assert.Equal(7856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_128f;
            Assert.Equal("SLH-DSA-SHA2-128f", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.SecretKeySizeInBytes);
            Assert.Equal(17088, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake128f;
            Assert.Equal("SLH-DSA-SHAKE-128f", algorithm.Name);
            Assert.Equal(32, algorithm.PublicKeySizeInBytes);
            Assert.Equal(64, algorithm.SecretKeySizeInBytes);
            Assert.Equal(17088, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_192s;
            Assert.Equal("SLH-DSA-SHA2-192s", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.SecretKeySizeInBytes);
            Assert.Equal(16224, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake192s;
            Assert.Equal("SLH-DSA-SHAKE-192s", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.SecretKeySizeInBytes);
            Assert.Equal(16224, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_192f;
            Assert.Equal("SLH-DSA-SHA2-192f", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.SecretKeySizeInBytes);
            Assert.Equal(35664, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake192f;
            Assert.Equal("SLH-DSA-SHAKE-192f", algorithm.Name);
            Assert.Equal(48, algorithm.PublicKeySizeInBytes);
            Assert.Equal(96, algorithm.SecretKeySizeInBytes);
            Assert.Equal(35664, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_256s;
            Assert.Equal("SLH-DSA-SHA2-256s", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.SecretKeySizeInBytes);
            Assert.Equal(29792, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake256s;
            Assert.Equal("SLH-DSA-SHAKE-256s", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.SecretKeySizeInBytes);
            Assert.Equal(29792, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaSha2_256f;
            Assert.Equal("SLH-DSA-SHA2-256f", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.SecretKeySizeInBytes);
            Assert.Equal(49856, algorithm.SignatureSizeInBytes);

            algorithm = SlhDsaAlgorithm.SlhDsaShake256f;
            Assert.Equal("SLH-DSA-SHAKE-256f", algorithm.Name);
            Assert.Equal(64, algorithm.PublicKeySizeInBytes);
            Assert.Equal(128, algorithm.SecretKeySizeInBytes);
            Assert.Equal(49856, algorithm.SignatureSizeInBytes);
        }
    }
}
