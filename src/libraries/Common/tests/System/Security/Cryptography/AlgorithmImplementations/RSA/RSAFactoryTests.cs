// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class RSAFactoryTests
    {
        [Fact]
        public static void RSACreateDefault_Equals_SameInstance()
        {
            using RSA rsa = RSAFactory.Create();
            AssertExtensions.TrueExpression(rsa.Equals(rsa));
        }

        [Fact]
        public static void RSACreateKeySize_Equals_SameInstance()
        {
            using RSA rsa = RSAFactory.Create(1024);
            AssertExtensions.TrueExpression(rsa.Equals(rsa));
        }

        [Fact]
        public static void RSACreateParameters_Equals_SameInstance()
        {
            using RSA rsa = RSAFactory.Create(TestData.RSA2048Params);
            AssertExtensions.TrueExpression(rsa.Equals(rsa));
        }
    }
}
