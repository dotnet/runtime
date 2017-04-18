// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public static class CertificateRequestApiTests
    {
        [Fact]
        public static void ConstructorDefaults()
        {
            const string TestCN = "CN=Test";

            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp256r1Data.KeyParameters))
            {
                CertificateRequest request = new CertificateRequest(TestCN, ecdsa, HashAlgorithmName.SHA256);

                Assert.NotNull(request.PublicKey);
                Assert.NotNull(request.CertificateExtensions);
                Assert.Empty(request.CertificateExtensions);
                Assert.Equal(TestCN, request.SubjectName.Name);
            }
        }

        [Fact]
        public static void ToPkcs10_ArgumentExceptions()
        {
            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp256r1Data.KeyParameters))
            {
                CertificateRequest request = new CertificateRequest("", ecdsa, HashAlgorithmName.SHA256);

                Assert.Throws<ArgumentNullException>("signatureGenerator", () => request.CreateSigningRequest(null));
            }
        }

        [Fact]
        public static void SelfSign_ArgumentValidation()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(TestData.RsaBigExponentParams);

                CertificateRequest request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256);

                Assert.Throws<ArgumentException>(
                    null,
                    () => request.CreateSelfSigned(DateTimeOffset.MaxValue, DateTimeOffset.MinValue));
            }
        }

        [Fact]
        public static void Sign_ArgumentValidation()
        {
            using (X509Certificate2 testRoot = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            {
                CertificateRequest request = new CertificateRequest("CN=Test", testRoot.GetRSAPublicKey(), HashAlgorithmName.SHA256);

                Assert.Throws<ArgumentNullException>(
                    "generator",
                    () => request.Create(testRoot.SubjectName, null, DateTimeOffset.MinValue, DateTimeOffset.MinValue, null));

                Assert.Throws<ArgumentException>(
                    null,
                    () => request.Create(testRoot, DateTimeOffset.MaxValue, DateTimeOffset.MinValue, null));

                Assert.Throws<ArgumentException>(
                    "serialNumber",
                    () => request.Create(testRoot, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, null));

                Assert.Throws<ArgumentException>(
                    "serialNumber",
                    () => request.Create(testRoot, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, Array.Empty<byte>()));
            }
        }
    }
}
