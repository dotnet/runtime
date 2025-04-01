// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public abstract class SlhDsaConstructionTestsBase : SlhDsaTestsBase
    {
        [ConditionalTheory(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void AlgorithmMatches(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = SlhDsa.GenerateKey(algorithm);
            Assert.Equal(algorithm, slhDsa.Algorithm);

            // TODO add remaining imports
        }
        
        // TODO: Validate that constructed keys have expected data (e.g. export public key and compare to expected)
    }
}
