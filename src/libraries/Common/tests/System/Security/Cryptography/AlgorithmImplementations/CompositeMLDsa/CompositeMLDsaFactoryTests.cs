// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLDsaFactoryTests
    {
        [Fact]
        public static void NoSupportYet()
        {
            AssertExtensions.FalseExpression(CompositeMLDsa.IsSupported);
            AssertExtensions.FalseExpression(CompositeMLDsa.IsAlgorithmSupported(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss));

            Assert.Throws<PlatformNotSupportedException>(() => CompositeMLDsa.GenerateKey(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss));
            Assert.Throws<PlatformNotSupportedException>(() => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss, new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes]));
            Assert.Throws<PlatformNotSupportedException>(() => CompositeMLDsa.ImportCompositeMLDsaPublicKey(CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss, new byte[MLDsaAlgorithm.MLDsa44.PublicKeySizeInBytes]));
        }
    }
}
