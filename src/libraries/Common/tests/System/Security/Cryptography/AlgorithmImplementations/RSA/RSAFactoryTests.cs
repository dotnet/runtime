// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class RSAFactoryTests<TProvider> where TProvider : IRSAProvider, new()
    {
        private static readonly TProvider s_provider = new TProvider();

        private static RSA CreateRSA(RSAParameters rsaParameters)
        {
            RSA rsa = s_provider.Create();
            rsa.ImportParameters(rsaParameters);
            return rsa;
        }

        [Fact]
        public static void RSACreateDefault_Equals_SameInstance()
        {
            using RSA rsa = s_provider.Create();
            AssertExtensions.TrueExpression(rsa.Equals(rsa));
        }

        [Fact]
        public static void RSACreateKeySize_Equals_SameInstance()
        {
            using RSA rsa = s_provider.Create(2048);
            AssertExtensions.TrueExpression(rsa.Equals(rsa));
        }

        [Fact]
        public static void RSACreateParameters_Equals_SameInstance()
        {
            using RSA rsa = CreateRSA(TestData.RSA2048Params);
            AssertExtensions.TrueExpression(rsa.Equals(rsa));
        }

        [Fact]
        public static void RSACreateParameters_Equals_DifferentInstance_FalseForSameKeyMaterial()
        {
            using RSA rsa1 = CreateRSA(TestData.RSA2048Params);
            using RSA rsa2 = CreateRSA(TestData.RSA2048Params);
            AssertExtensions.FalseExpression(rsa1.Equals(rsa2));
        }
    }
}
