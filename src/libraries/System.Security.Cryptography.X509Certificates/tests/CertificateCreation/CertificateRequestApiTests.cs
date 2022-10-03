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

        [Fact]
        public static void NullAttributeInCollection()
        {
            using (ECDsa key = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters))
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=Test",
                    key,
                    HashAlgorithmName.SHA384);

                req.OtherRequestAttributes.Add(null);
                
                X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);
                InvalidOperationException ex;

                ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest());
                Assert.Contains(nameof(CertificateRequest.OtherRequestAttributes), ex.Message);

                ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest(gen));
                Assert.Contains(nameof(CertificateRequest.OtherRequestAttributes), ex.Message);
            }
        }

        [Fact]
        public static void NullOidInAttributeInCollection()
        {
            using (ECDsa key = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters))
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=Test",
                    key,
                    HashAlgorithmName.SHA384);

                req.OtherRequestAttributes.Add(new AsnEncodedData((Oid)null, Array.Empty<byte>()));

                X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);
                InvalidOperationException ex;

                ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest());
                Assert.Contains(nameof(CertificateRequest.OtherRequestAttributes), ex.Message);

                ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest(gen));
                Assert.Contains(nameof(CertificateRequest.OtherRequestAttributes), ex.Message);
            }
        }

        [Fact]
        public static void NullOidValueInAttributeInCollection()
        {
            using (ECDsa key = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters))
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=Test",
                    key,
                    HashAlgorithmName.SHA384);

                req.OtherRequestAttributes.Add(new AsnEncodedData(new Oid(null, null), Array.Empty<byte>()));

                X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);
                InvalidOperationException ex;

                ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest());
                Assert.Contains(nameof(CertificateRequest.OtherRequestAttributes), ex.Message);

                ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest(gen));
                Assert.Contains(nameof(CertificateRequest.OtherRequestAttributes), ex.Message);
            }
        }

        [Fact]
        public static void ExtensionRequestInAttributeInCollection()
        {
            using (ECDsa key = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters))
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=Test",
                    key,
                    HashAlgorithmName.SHA384);

                req.OtherRequestAttributes.Add(
                    new AsnEncodedData(
                        new Oid("1.2.840.113549.1.9.14", null),
                        Array.Empty<byte>()));

                X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);
                InvalidOperationException ex;

                ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest());
                Assert.Contains(nameof(CertificateRequest.OtherRequestAttributes), ex.Message);
                Assert.Contains(nameof(CertificateRequest.CertificateExtensions), ex.Message);

                ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest(gen));
                Assert.Contains(nameof(CertificateRequest.OtherRequestAttributes), ex.Message);
                Assert.Contains(nameof(CertificateRequest.CertificateExtensions), ex.Message);
            }
        }

        [Fact]
        public static void PublicKeyConstructor_CannotSelfSign()
        {
            byte[] spki;

            using (ECDsa key = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters))
            {
                spki = key.ExportSubjectPublicKeyInfo();
            }

            PublicKey publicKey = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out _);

            CertificateRequest req = new CertificateRequest(
                new X500DistinguishedName("CN=Test"),
                publicKey,
                HashAlgorithmName.SHA384);

            InvalidOperationException ex;

            ex = Assert.Throws<InvalidOperationException>(() => req.CreateSigningRequest());
            Assert.Contains(nameof(X509SignatureGenerator), ex.Message);

            DateTimeOffset notBefore = DateTimeOffset.UtcNow;
            DateTimeOffset notAfter = notBefore.AddMinutes(1);

            ex = Assert.Throws<InvalidOperationException>(() => req.CreateSelfSigned(notBefore, notAfter));
            Assert.Contains(nameof(X509SignatureGenerator), ex.Message);
        }

        [Fact]
        public static void InvalidDerInAttribute()
        {
            using (ECDsa key = ECDsa.Create(EccTestData.Secp384r1Data.KeyParameters))
            {
                CertificateRequest req = new CertificateRequest(
                    "CN=Test",
                    key,
                    HashAlgorithmName.SHA384);

                // This is "legal DER", but contains more than one value, which is invalid in context.
                ReadOnlySpan<byte> invalidEncoding = new byte[]
                {
                    // PrintableString("123")
                    0x13, 0x03, 0x31, 0x32, 0x33,
                    // NULL
                    0x05, 0x00,
                };

                req.OtherRequestAttributes.Add(
                    new AsnEncodedData(
                        new Oid("1.2.840.113549.1.9.7", null),
                        invalidEncoding));

                X509SignatureGenerator gen = X509SignatureGenerator.CreateForECDsa(key);

                Assert.Throws<CryptographicException>(() => req.CreateSigningRequest());
                Assert.Throws<CryptographicException>(() => req.CreateSigningRequest(gen));
            }
        }

        [Fact]
        public static void LoadNullArray()
        {
            Assert.Throws<ArgumentNullException>(
                "pkcs10",
                () => CertificateRequest.LoadSigningRequest((byte[])null, HashAlgorithmName.SHA256));
        }

        [Fact]
        public static void LoadNullPemString()
        {
            Assert.Throws<ArgumentNullException>(
                "pkcs10Pem",
                () => CertificateRequest.LoadSigningRequestPem((string)null, HashAlgorithmName.SHA256));
        }

        [Fact]
        public static void LoadWithDefaultHashAlgorithm()
        {
            Assert.Throws<ArgumentNullException>(
                "signerHashAlgorithm",
                () => CertificateRequest.LoadSigningRequest(Array.Empty<byte>(), default(HashAlgorithmName)));

            {
                int consumed = -1;

                Assert.Throws<ArgumentNullException>(
                    "signerHashAlgorithm",
                    () => CertificateRequest.LoadSigningRequest(
                        ReadOnlySpan<byte>.Empty,
                        default(HashAlgorithmName),
                        out consumed));

                Assert.Equal(-1, consumed);
            }

            Assert.Throws<ArgumentNullException>(
                "signerHashAlgorithm",
                () => CertificateRequest.LoadSigningRequestPem(string.Empty, default(HashAlgorithmName)));

            Assert.Throws<ArgumentNullException>(
                "signerHashAlgorithm",
                () => CertificateRequest.LoadSigningRequestPem(
                    ReadOnlySpan<char>.Empty,
                    default(HashAlgorithmName)));
        }

        [Fact]
        public static void LoadWithEmptyHashAlgorithm()
        {
            HashAlgorithmName hashAlgorithm = new HashAlgorithmName("");

            Assert.Throws<ArgumentException>(
                "signerHashAlgorithm",
                () => CertificateRequest.LoadSigningRequest(Array.Empty<byte>(), hashAlgorithm));

            {
                int consumed = -1;

                Assert.Throws<ArgumentException>(
                    "signerHashAlgorithm",
                    () => CertificateRequest.LoadSigningRequest(
                        ReadOnlySpan<byte>.Empty,
                        hashAlgorithm,
                        out consumed));

                Assert.Equal(-1, consumed);
            }

            Assert.Throws<ArgumentException>(
                "signerHashAlgorithm",
                () => CertificateRequest.LoadSigningRequestPem(string.Empty, hashAlgorithm));

            Assert.Throws<ArgumentException>(
                "signerHashAlgorithm",
                () => CertificateRequest.LoadSigningRequestPem(
                    ReadOnlySpan<char>.Empty,
                    hashAlgorithm));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public static void LoadWithUnknownOptions(int optionsValue)
        {
            CertificateRequestLoadOptions options = (CertificateRequestLoadOptions)optionsValue;
            HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA256;
            ArgumentOutOfRangeException ex;

            ex = Assert.Throws<ArgumentOutOfRangeException>(
                "options",
                () => CertificateRequest.LoadSigningRequest(Array.Empty<byte>(), hashAlgorithm, options));

            Assert.Equal(options, ex.ActualValue);

            {
                int consumed = -1;

                ex = Assert.Throws<ArgumentOutOfRangeException>(
                    "options",
                    () => CertificateRequest.LoadSigningRequest(
                        ReadOnlySpan<byte>.Empty,
                        hashAlgorithm,
                        out consumed,
                        options));

                Assert.Equal(-1, consumed);
                Assert.Equal(options, ex.ActualValue);
            }

            ex = Assert.Throws<ArgumentOutOfRangeException>(
                "options",
                () => CertificateRequest.LoadSigningRequestPem(string.Empty, hashAlgorithm, options));

            Assert.Equal(options, ex.ActualValue);

            ex = Assert.Throws<ArgumentOutOfRangeException>(
                "options",
                () => CertificateRequest.LoadSigningRequestPem(
                    ReadOnlySpan<char>.Empty,
                    hashAlgorithm,
                    options));

            Assert.Equal(options, ex.ActualValue);
        }

        [Theory]
        [InlineData("SHA256")]
        [InlineData("SHA384")]
        [InlineData("SHA512")]
        [InlineData("SHA1")]
        public static void VerifySignature_ECDsa(string hashAlgorithm)
        {
            HashAlgorithmName hashAlgorithmName = new HashAlgorithmName(hashAlgorithm);

            using (ECDsa key = ECDsa.Create())
            {
                CertificateRequest first = new CertificateRequest(
                    "CN=Test",
                    key,
                    hashAlgorithmName);

                byte[] pkcs10;

                if (hashAlgorithm == "SHA1")
                {
                    pkcs10 = first.CreateSigningRequest(new ECDsaSha1SignatureGenerator(key));
                }
                else
                {
                    pkcs10 = first.CreateSigningRequest();
                }

                // Assert.NoThrow
                CertificateRequest.LoadSigningRequest(pkcs10, hashAlgorithmName, out _);

                pkcs10[^1] ^= 0xFF;

                Assert.Throws<CryptographicException>(
                    () => CertificateRequest.LoadSigningRequest(pkcs10, hashAlgorithmName, out _));

                // Assert.NoThrow
                CertificateRequest.LoadSigningRequest(
                    pkcs10,
                    hashAlgorithmName,
                    out _,
                    CertificateRequestLoadOptions.SkipSignatureValidation);
            }
        }
    }
}
