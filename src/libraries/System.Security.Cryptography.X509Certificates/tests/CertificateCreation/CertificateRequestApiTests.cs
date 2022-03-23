// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

                AssertExtensions.Throws<ArgumentNullException>("signatureGenerator", () => request.CreateSigningRequest(null));
            }
        }

        [Fact]
        public static void SelfSign_ArgumentValidation()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(TestData.RsaBigExponentParams);

                var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                AssertExtensions.Throws<ArgumentException>(
                    null,
                    () => request.CreateSelfSigned(DateTimeOffset.MaxValue, DateTimeOffset.MinValue));
            }
        }

        [Fact]
        public static void Sign_ArgumentValidation()
        {
            using (X509Certificate2 testRoot = new X509Certificate2(TestData.PfxData, TestData.PfxDataPassword))
            using (RSA publicKey = testRoot.GetRSAPublicKey())
            {
                var request = new CertificateRequest(
                    "CN=Test",
                    publicKey,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                AssertExtensions.Throws<ArgumentNullException>(
                    "generator",
                    () => request.Create(testRoot.SubjectName, null, DateTimeOffset.MinValue, DateTimeOffset.MinValue, null));

                DateTimeOffset notAfter = testRoot.NotAfter;
                DateTimeOffset notBefore = testRoot.NotBefore;

                AssertExtensions.Throws<ArgumentException>(
                    null,
                    () => request.Create(testRoot, notAfter, notBefore, null));

                AssertExtensions.Throws<ArgumentException>(
                    "serialNumber",
                    () => request.Create(testRoot, notBefore, notAfter, null));

                AssertExtensions.Throws<ArgumentException>(
                    "serialNumber",
                    () => request.Create(testRoot, notBefore, notAfter, Array.Empty<byte>()));

                AssertExtensions.Throws<ArgumentException>(
                    "serialNumber",
                    () => request.Create(testRoot, notBefore, notAfter, ReadOnlySpan<byte>.Empty));
            }
        }

        [Fact]
        public static void CtorValidation_ECDSA_string()
        {
            string subjectName = null;
            ECDsa key = null;

            AssertExtensions.Throws<ArgumentNullException>(
                "subjectName",
                () => new CertificateRequest(subjectName, key, default(HashAlgorithmName)));

            subjectName = "";

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => new CertificateRequest(subjectName, key, default(HashAlgorithmName)));

            key = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters);

            using (key)
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "hashAlgorithm",
                    () => new CertificateRequest(subjectName, key, default(HashAlgorithmName)));

                AssertExtensions.Throws<ArgumentException>(
                    "hashAlgorithm",
                    () => new CertificateRequest(subjectName, key, new HashAlgorithmName("")));
            }
        }

        [Fact]
        public static void CtorValidation_ECDSA_X500DN()
        {
            X500DistinguishedName subjectName = null;
            ECDsa key = null;

            AssertExtensions.Throws<ArgumentNullException>(
                "subjectName",
                () => new CertificateRequest(subjectName, key, default(HashAlgorithmName)));

            subjectName = new X500DistinguishedName("");

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => new CertificateRequest(subjectName, key, default(HashAlgorithmName)));

            key = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters);

            using (key)
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "hashAlgorithm",
                    () => new CertificateRequest(subjectName, key, default(HashAlgorithmName)));
                AssertExtensions.Throws<ArgumentException>(
                    "hashAlgorithm",
                    () => new CertificateRequest(subjectName, key, new HashAlgorithmName("")));
            }
        }

        [Fact]
        public static void CtorValidation_RSA_string()
        {
            string subjectName = null;
            RSA key = null;
            RSASignaturePadding padding = null;

            AssertExtensions.Throws<ArgumentNullException>(
                "subjectName",
                () => new CertificateRequest(subjectName, key, default(HashAlgorithmName), padding));

            subjectName = "";

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => new CertificateRequest(subjectName, key, default(HashAlgorithmName), padding));

            key = RSA.Create(TestData.RsaBigExponentParams);

            using (key)
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "hashAlgorithm",
                    () => new CertificateRequest(subjectName, key, default(HashAlgorithmName), padding));

                AssertExtensions.Throws<ArgumentNullException>(
                    "padding",
                    () => new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, padding));
            }
        }

        [Fact]
        public static void CtorValidation_RSA_X500DN()
        {
            X500DistinguishedName subjectName = null;
            RSA key = null;
            RSASignaturePadding padding = null;

            AssertExtensions.Throws<ArgumentNullException>(
                "subjectName",
                () => new CertificateRequest(subjectName, key, default(HashAlgorithmName), padding));

            subjectName = new X500DistinguishedName("");

            AssertExtensions.Throws<ArgumentNullException>(
                "key",
                () => new CertificateRequest(subjectName, key, default(HashAlgorithmName), padding));

            key = RSA.Create(TestData.RsaBigExponentParams);

            using (key)
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "hashAlgorithm",
                    () => new CertificateRequest(subjectName, key, default(HashAlgorithmName), padding));

                AssertExtensions.Throws<ArgumentException>(
                    "hashAlgorithm",
                    () => new CertificateRequest(subjectName, key, new HashAlgorithmName(""), padding));

                AssertExtensions.Throws<ArgumentNullException>(
                    "padding",
                    () => new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, padding));
            }
        }

        [Fact]
        public static void CtorValidation_PublicKey_X500DN()
        {
            X500DistinguishedName subjectName = null;
            PublicKey publicKey = null;

            AssertExtensions.Throws<ArgumentNullException>(
                "subjectName",
                () => new CertificateRequest(subjectName, publicKey, default(HashAlgorithmName)));

            subjectName = new X500DistinguishedName("");

            AssertExtensions.Throws<ArgumentNullException>(
                "publicKey",
                () => new CertificateRequest(subjectName, publicKey, default(HashAlgorithmName)));

            using (ECDsa ecdsa = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters))
            {
                X509SignatureGenerator generator = X509SignatureGenerator.CreateForECDsa(ecdsa);
                publicKey = generator.PublicKey;
            }

            AssertExtensions.Throws<ArgumentNullException>(
                "hashAlgorithm",
                () => new CertificateRequest(subjectName, publicKey, default(HashAlgorithmName)));

            AssertExtensions.Throws<ArgumentException>(
                "hashAlgorithm",
                () => new CertificateRequest(subjectName, publicKey, new HashAlgorithmName("")));
        }
    }
}
