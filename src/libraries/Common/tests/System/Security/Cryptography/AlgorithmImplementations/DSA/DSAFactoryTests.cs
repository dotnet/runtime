// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Not supported on Browser/iOS/tvOS/MacCatalyst")]
    public partial class DSAFactoryTests
    {
        [Fact]
        public static void DSACreateDefault_Equals_SameInstance()
        {
            using DSA dsa = DSAFactory.Create();
            dsa.ImportParameters(DSATestData.GetDSA1024Params());
            AssertExtensions.TrueExpression(dsa.Equals(dsa));
        }

        [Fact]
        public static void DSACreateKeySize_Equals_SameInstance()
        {
            using DSA dsa = DSAFactory.Create(1024);
            AssertExtensions.TrueExpression(dsa.Equals(dsa));
        }

        [Fact]
        public static void DsaCreate_Equals_DifferentInstance_FalseForSameKeyMaterial()
        {
            using DSA dsa1 = DSAFactory.Create();
            using DSA dsa2 = DSAFactory.Create();
            dsa1.ImportParameters(DSATestData.GetDSA1024Params());
            dsa2.ImportParameters(DSATestData.GetDSA1024Params());
            AssertExtensions.FalseExpression(dsa1.Equals(dsa2));
        }
    }
}
