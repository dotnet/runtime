// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaOpenSslConstructionTests : SlhDsaConstructionTestsBase
    {
        [Fact]
        public static void SlhDsaOpenSsl_NotSupportedOnNonUnixPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new SlhDsaOpenSsl(null));
            Assert.Throws<PlatformNotSupportedException>(() => new SlhDsaOpenSsl(new SafeEvpPKeyHandle()));
        }

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm)
        {
            Assert.Fail();
            throw new PlatformNotSupportedException();
        }

        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Assert.Fail();
            throw new PlatformNotSupportedException();
        }

        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Assert.Fail();
            throw new PlatformNotSupportedException();
        }
    }
}
