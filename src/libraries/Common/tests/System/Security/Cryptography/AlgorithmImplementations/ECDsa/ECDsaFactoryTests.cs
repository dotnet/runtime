// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class ECDsaFactoryTests
    {
        [Fact]
        public static void ECDsaCreateDefault_Equals_SameInstance()
        {
            using ECDsa ecdsa = ECDsaFactory.Create();
            AssertExtensions.TrueExpression(ecdsa.Equals(ecdsa));
        }

        [Fact]
        public static void ECDsaCreateKeySize_Equals_SameInstance()
        {
            using ECDsa ecdsa = ECDsaFactory.Create(256);
            AssertExtensions.TrueExpression(ecdsa.Equals(ecdsa));
        }

        [Fact]
        public static void ECDsaCreateKeySize_Equals_DifferentInstance_FalseForSameKeyMaterial()
        {
            using ECDsa ecdsa1 = ECDsaFactory.Create();
            using ECDsa ecdsa2 = ECDsaFactory.Create();
            ecdsa1.ImportParameters(EccTestData.GetNistP256ReferenceKey());
            ecdsa2.ImportParameters(EccTestData.GetNistP256ReferenceKey());
            AssertExtensions.FalseExpression(ecdsa1.Equals(ecdsa2));
        }

#if NET
        [Fact]
        public static void ECDsaCreateCurve_Equals_SameInstance()
        {
            using ECDsa ecdsa = ECDsaFactory.Create(ECCurve.NamedCurves.nistP256);
            AssertExtensions.TrueExpression(ecdsa.Equals(ecdsa));
        }
#endif
    }
}
