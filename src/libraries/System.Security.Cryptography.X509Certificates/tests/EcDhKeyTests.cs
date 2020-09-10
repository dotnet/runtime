// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class EcDhKeyTests
    {
        [Fact]
        public static void GetEcDhPublicKey()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.EcDh256Certificate))
            {
                Assert.NotNull(cert.GetECDiffieHellmanPublicKey());
            }
        }

        [Fact]
        public static void GetECDsaPublicKeyNullForEcDhKey()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.EcDh256Certificate))
            {
                Assert.Null(cert.GetECDsaPublicKey());
            }
        }
    }
}
