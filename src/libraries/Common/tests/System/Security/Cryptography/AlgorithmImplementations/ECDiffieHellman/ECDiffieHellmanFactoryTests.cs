// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class ECDiffieHellmanFactoryTests
    {
        protected abstract ECDiffieHellmanProvider ECDiffieHellmanFactory { get; }

        [Fact]
        public void ECDiffieHellmanCreateDefault_Equals_SameInstance()
        {
            using ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create();
            AssertExtensions.TrueExpression(ecdh.Equals(ecdh));
        }

        [Fact]
        public void ECDiffieHellmanCreateKeySize_Equals_SameInstance()
        {
            using ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(256);
            AssertExtensions.TrueExpression(ecdh.Equals(ecdh));
        }

        [Fact]
        public void ECDiffieHellmanCreateKeySize_Equals_DifferentInstance_FalseForSameKeyMaterial()
        {
            using ECDiffieHellman ecdh1 = ECDiffieHellmanFactory.Create();
            using ECDiffieHellman ecdh2 = ECDiffieHellmanFactory.Create();
            ecdh1.ImportParameters(EccTestData.GetNistP256ReferenceKey());
            ecdh2.ImportParameters(EccTestData.GetNistP256ReferenceKey());
            AssertExtensions.FalseExpression(ecdh1.Equals(ecdh2));
        }

#if NET
        [Fact]
        public void ECDiffieHellmanCreateCurve_Equals_SameInstance()
        {
            using ECDiffieHellman ecdh = ECDiffieHellmanFactory.Create(ECCurve.NamedCurves.nistP256);
            AssertExtensions.TrueExpression(ecdh.Equals(ecdh));
        }
#endif
    }
}
