// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;

using Xunit;

using TestOids = System.Security.Cryptography.Pkcs.Tests.Oids;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    public static class CompositeMLKemDecryptTests
    {
        // From https://datatracker.ietf.org/doc/html/draft-ietf-lamps-cms-composite-kem-03#appendix-B
        // The RFC uses an AuthEnvelopedData so the contents here are translated to an EnvelopedData.
        private const string RfcExampleEnvelopedData = """
            MIIFYAYJKoZIhvcNAQcDoIIFUTCCBU0CAQMxggUIpIIFBAYLKoZIhvcNAQkQDQMwggTzAgEAgBQU8dj+0h9hA2dsdSyX0JSVN6lqsjAKBggrBgEFBQcGOwSC
            BIFUt1yUX/gxlPvzEiFHSbEUv2g4h4tJQD9SNb53SuZxnZBTHamrAToPioHb1lEFkvX7y+OxvPOTeT9RfnWLvdEEZ8GnKxQWUjPjhuprAn5IKCTTMkTMf4EL
            99Ic0o4AfIQZCHaYvg3Xk0ZT81yLAWTCGNy8vigwKoE+R6M0yVZTSQz1dgLHmNY9OT6ebrwhkmWqZWJjKmGNTD5AUrIMifLwTqRVVGc9G+iRH4PKSiDOhrWc
            8KjJZOWwRweHXVYBHUiSkavyNwg43GCP4uJuYX0OwsLGQ+Ucr2cW5kwsxaGFxydw7EGFSHWJFWu0fkFBgMbO3qzY2tB/+pCRRB/aztVTxIK9DEHVt3ZJ8O2A
            PQfSloLcLEHSBH0Y/dq5ZSm5+KLw3dXkswL//YzL7APZkX/6a40kkRcUbFj9iaE1qW7OZwxYpVliimgBLUyaQ+X/+0J/DfMNp0W7Dk6zNHFruCF1EEZ7i1jh
            vlSzDIV1ImVMu1eHgTyxsvuVenJfUWEhh7WLuDNT+OVqpq+JyU7dZW2Sv2aRMCL86PhXMGF9ihZruyRRVVmRXMRyQ1lQvOjCUhn+9QemUnpqOSDC3q2ncykC
            4tKujGC9pRimXaZvk1gbfjcEKhBGRnWrMm9gO+FOW1UlY0reuVZRKoT9uAmvLzfSCp/SP2fsp3QbSdW310+UeottrGiIiz44AplMmMzLaMKg55J/FeXnvhQA
            mD4ipMnvK6vfJEUlrd47TZHneOIeK9MsUiH1Qz8oOQEo6r87GBuFwsHv1/rpRjmbuP4h3irtCj5yyrNNMFUJVH3zOkXRfQE6OsCPDbadv/C65aS5wV7ovcTi
            UltJ5oZa63UgUZNfji+12uqRLRGGcRArj7RnXDk3s4mnxwaMtpezeY//6dbmQVZJO7gI0Gej6sQYp79YGeJadAyDSZFOUAg0DDgaB9hwsUub6ZOdMGvHAtRq
            WCFt+TLSvLRaPxgb2E9Lm6mS86XxKl22Fa4FmKnEMs9cAJUYeoSak8oNLX3u2y2xztU8DUrl0rN+sl4HmSRS4BiOLXJoL0bwFn8PamgCQ+/yxLX88CNYUvmd
            76TVNaR5F2+joiWHURWBs2Sf5BDjAuwbkGHLU1xJKuV6wSbLSepH3hKpCXxfioadhNSq6QPwvG9TpgUUzr4C/cmiBKa+KmZWfVify6yjZWWj4C0QsWkg6iaw
            XFC+gF0G7OPJ7n236yB8M8HUrJLKKU8aD4+sg5/qA4n65DOEotqnpMH16S1iL46zesDFXrnfjZcT3gOg74stkwbIUw9gfYmLqIrbatmCvs07BcrDK33+dnyC
            bl6L30ASDxpJ6EzRHEwJsHwnWRpgMrNyp71Giwns4H7m7KbpRVnxSEPdmpeTHgwG0QWqIjfmejwSjTOrYdxHmG/a+3l+YMtERTtN+896/f4C9obHIP1l1TgY
            J75i8zIqy8NyEwRD1mkOOuOxfyY/k0WZjKJjF/dXuG0K1BUxsRT11X+oLlAj5RdiJ9CH92XhQhzvMb6cMVhmg4AXuyqliVW/Us5uMA0GCyqGSIb3DQEJEAMc
            AgEgMAsGCWCGSAFlAwQBLQQoXROuANkUopFYqO8ysjrl8nuMKTOH475l/D38Gbmdj9xVJSf9QhVLNzA8BgkqhkiG9w0BBwEwHQYJYIZIAWUDBAEqBBAAESIz
            RFVmd4iZqrvM3e7/gBAcbi4o9kaMopTTmesr2ihb
            """;
        private const string RfcExamplePrivateKey = """
            MIGEAgEAMAoGCCsGAQUFBwY7BHOImiTrgVkiRNnS3EmMdxHUrh+EHeflRSSQaMrG7NAvf0DDSVt58hvzJz/RuCOE4/8REOs/DZVr
            5gWO2jOyldClMDECAQEEIM/ctKyhCrQyBNhBbpwS5ZkEO1mklf14j5NRtwj3obB+oAoGCCqGSM49AwEH
            """;

        public static TheoryData<int> InvalidEncryptedKeyLengths { get; } = new() { 16, 23, 25 };

        public static TheoryData<HashAlgorithmName, string> KdfAlgorithms { get; } = new()
        {
            { HashAlgorithmName.SHA256, TestOids.HkdfSha256 },
            { HashAlgorithmName.SHA384, TestOids.HkdfSha384 },
            { HashAlgorithmName.SHA512, TestOids.HkdfSha512 },
            { HashAlgorithmName.SHA3_256, TestOids.HkdfSha3_256 },
            { HashAlgorithmName.SHA3_384, TestOids.HkdfSha3_384 },
            { HashAlgorithmName.SHA3_512, TestOids.HkdfSha3_512 },
        };

        public static TheoryData<string, int> KeyWrapAlgorithms { get; } = new()
        {
            { TestOids.Aes128Wrap, 16 },
            { TestOids.Aes192Wrap, 24 },
            { TestOids.Aes256Wrap, 32 },
        };

        public static TheoryData<byte[]> UserKeyingMaterial { get; } = new()
        {
            (byte[])null,
            Array.Empty<byte>(),
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        public static bool IsMLKemWithX25519Supported =>
            CompositeMLKem.IsAlgorithmSupported(CompositeMLKemAlgorithm.MLKem768WithX25519);

        public static bool IsMLKemWithX25519AndMLKemSupported =>
            IsMLKemWithX25519Supported && MLKem.IsSupported;

        public static bool IsMLKem768WithECDHP256Supported =>
            CompositeMLKem.IsAlgorithmSupported(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256);

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptRfcExampleRecipientInfo()
        {
            EnvelopedCms cms = DecodeRfcExample();
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            Assert.Equal(
                TestOids.MLKem768WithECDiffieHellmanP256Sha3_256,
                recipientInfo.KeyEncapsulationAlgorithm.Oid.Value);
            Assert.Equal(
                CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256.CiphertextSizeInBytes,
                recipientInfo.KeyEncapsulationCiphertext.Length);
            Assert.Empty(recipientInfo.KeyEncapsulationAlgorithm.Parameters);
            Assert.Equal(TestOids.HkdfSha256, recipientInfo.KeyDerivationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyDerivationAlgorithm.Parameters);
            Assert.Equal(32, recipientInfo.KeyEncryptionKeyLengthInBytes);
            Assert.Null(recipientInfo.UserKeyingMaterial);
            Assert.Equal(TestOids.Aes256Wrap, recipientInfo.KeyEncryptionAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncryptionAlgorithm.Parameters);
            Assert.Equal(40, recipientInfo.EncryptedKey.Length);

            using (CompositeMLKem privateKey = ImportRfcExamplePrivateKey())
            {
                cms.Decrypt(recipientInfo, privateKey);
            }

            AssertExtensions.SequenceEqual("Hello, world!"u8, cms.ContentInfo.Content.AsSpan());
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void DecryptAlgorithm(CompositeMLKemTestVector vector)
        {
            Decrypt(CompositeMLKemCmsTestData.BuildEnvelopedData(vector), vector);
        }

        [ConditionalTheory(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        [MemberData(nameof(KdfAlgorithms))]
        public static void DecryptKdfAlgorithm(HashAlgorithmName hashAlgorithm, string kdfOid)
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            byte[] document = CompositeMLKemCmsTestData.BuildEnvelopedData(
                vector,
                kdfAlgorithm: hashAlgorithm,
                kdfOid: kdfOid);

            Decrypt(document, vector);
        }

        [ConditionalTheory(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        [MemberData(nameof(KeyWrapAlgorithms))]
        public static void DecryptKeyWrapAlgorithm(string wrapOid, int kekLength)
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            byte[] document = CompositeMLKemCmsTestData.BuildEnvelopedData(
                vector,
                wrapOid: wrapOid,
                kekLength: kekLength);

            Decrypt(document, vector);
        }

        [ConditionalTheory(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        [MemberData(nameof(UserKeyingMaterial))]
        public static void DecryptUserKeyingMaterial(byte[] userKeyingMaterial)
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            EnvelopedCms cms = Decrypt(
                CompositeMLKemCmsTestData.BuildEnvelopedData(vector, ukm: userKeyingMaterial),
                vector);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            if (userKeyingMaterial is null)
            {
                Assert.Null(recipientInfo.UserKeyingMaterial);
            }
            else
            {
                Assert.True(recipientInfo.UserKeyingMaterial.HasValue);
                AssertExtensions.SequenceEqual(
                    userKeyingMaterial,
                    recipientInfo.UserKeyingMaterial.Value.Span);
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptNullArguments()
        {
            EnvelopedCms cms = DecodeRfcExample();
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (CompositeMLKem privateKey = ImportRfcExamplePrivateKey())
            {
                Assert.Throws<ArgumentNullException>(() => cms.Decrypt(null, privateKey));
                Assert.Throws<ArgumentNullException>(() => cms.Decrypt(recipientInfo, (CompositeMLKem)null));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptWithDisposedPrivateKey()
        {
            EnvelopedCms cms = DecodeRfcExample();
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
            CompositeMLKem privateKey = ImportRfcExamplePrivateKey();
            privateKey.Dispose();

            Assert.Throws<ObjectDisposedException>(() => cms.Decrypt(recipientInfo, privateKey));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptWithEncapsulationOnlyKey()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            EnvelopedCms cms = DecodeRfcExample();
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (CompositeMLKem publicKey = CompositeMLKem.ImportSubjectPublicKeyInfo(vector.Spki))
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, publicKey));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptBeforeDecode()
        {
            EnvelopedCms decodedCms = DecodeRfcExample();
            KemRecipientInfo recipientInfo =
                Assert.IsType<KemRecipientInfo>(Assert.Single(decodedCms.RecipientInfos));

            using (CompositeMLKem privateKey = ImportRfcExamplePrivateKey())
            {
                EnvelopedCms cms = new EnvelopedCms();
                Assert.Throws<InvalidOperationException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptAfterEncrypt()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            EnvelopedCms decodedCms = DecodeRfcExample();
            KemRecipientInfo recipientInfo =
                Assert.IsType<KemRecipientInfo>(Assert.Single(decodedCms.RecipientInfos));

            using (X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(vector.Certificate))
            using (CompositeMLKem privateKey = CompositeMLKem.ImportPkcs8PrivateKey(vector.Pkcs8))
            {
                EnvelopedCms cms = new EnvelopedCms(new ContentInfo("hello world!"u8.ToArray()));
                cms.Encrypt(new CmsRecipient(certificate));

                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptTwice()
        {
            EnvelopedCms cms = DecodeRfcExample();
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (CompositeMLKem privateKey = ImportRfcExamplePrivateKey())
            {
                cms.Decrypt(recipientInfo, privateKey);
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptCorrectKeyAfterWrongKey()
        {
            EnvelopedCms cms = DecodeRfcExample();
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
            CompositeMLKemAlgorithm wrongAlgorithm = CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256;

            using (CompositeMLKem wrongKey = CompositeMLKem.GenerateKey(wrongAlgorithm))
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, wrongKey));
            }

            using (CompositeMLKem correctKey = ImportRfcExamplePrivateKey())
            {
                cms.Decrypt(recipientInfo, correctKey);
            }

            AssertExtensions.SequenceEqual("Hello, world!"u8, cms.ContentInfo.Content.AsSpan());
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptWrongPrivateKey()
        {
            EnvelopedCms cms = DecodeRfcExample();
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
            CompositeMLKemAlgorithm algorithm = CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256;

            using (CompositeMLKem privateKey = CompositeMLKem.GenerateKey(algorithm))
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptTamperedEncryptedKey()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            EnvelopedCms cms = Decode(
                CompositeMLKemCmsTestData.BuildEnvelopedData(vector, encryptedKeyLength: 40));
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (CompositeMLKem privateKey = CompositeMLKem.ImportPkcs8PrivateKey(vector.Pkcs8))
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptInvalidVersion()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(CompositeMLKemCmsTestData.BuildEnvelopedData(vector, version: 1));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptInvalidKemCiphertextLength()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(CompositeMLKemCmsTestData.BuildEnvelopedData(vector, kemCiphertext: new byte[1]));
        }

        [ConditionalTheory(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        [MemberData(nameof(InvalidEncryptedKeyLengths))]
        public static void DecryptInvalidEncryptedKeyLength(int encryptedKeyLength)
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(
                CompositeMLKemCmsTestData.BuildEnvelopedData(vector, encryptedKeyLength: encryptedKeyLength));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptAesKeyWrapOidDoesNotMatchKekLength()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(
                CompositeMLKemCmsTestData.BuildEnvelopedData(
                    vector,
                    wrapOid: TestOids.Aes128Wrap,
                    kekLength: 32));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptUnknownKdf()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(CompositeMLKemCmsTestData.BuildEnvelopedData(vector, kdfOid: "1.2.3.4"));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptUnknownKem()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(CompositeMLKemCmsTestData.BuildEnvelopedData(vector, kemOid: "1.2.3.5"));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptKemAlgorithmParameters()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(
                CompositeMLKemCmsTestData.BuildEnvelopedData(vector, includeKemParameters: true));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptKdfAlgorithmParameters()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(
                CompositeMLKemCmsTestData.BuildEnvelopedData(vector, includeKdfParameters: true));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptKeyWrapAlgorithmParameters()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(
                CompositeMLKemCmsTestData.BuildEnvelopedData(vector, includeWrapParameters: true));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKem768WithECDHP256Supported))]
        public static void DecryptUnknownAesKeyWrap()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            AssertInvalidDocument(CompositeMLKemCmsTestData.BuildEnvelopedData(vector, wrapOid: "1.2.3.6"));
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKemWithX25519Supported))]
        public static void DecryptKemAlgorithmDoesNotMatchPrivateKeyAlgorithm()
        {
            EnvelopedCms cms = DecodeRfcExample();
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (CompositeMLKem privateKey =
                CompositeMLKem.ImportPkcs8PrivateKey(CompositeMLKemCmsTestData.X25519Vector.Pkcs8))
            {
                Assert.Throws<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKemWithX25519Supported))]
        public static void DecryptWithCertificatePrivateKeyNotSupported()
        {
            byte[] content = "hello world!"u8.ToArray();

            using (X509Certificate2 certificate =
                X509CertificateLoader.LoadCertificate(CompositeMLKemCmsTestData.X25519Vector.Certificate))
            {
                EnvelopedCms cms = new EnvelopedCms(new ContentInfo(content));
                cms.Encrypt(new CmsRecipient(certificate));
                byte[] encoded = cms.Encode();

                cms = new EnvelopedCms();
                cms.Decode(encoded);

                Assert.Throws<PlatformNotSupportedException>(
                    () => cms.Decrypt(new X509Certificate2Collection(certificate)));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsMLKemWithX25519AndMLKemSupported))]
        public static void DecryptWithCertificateContinuesToNextRecipient()
        {
            byte[] content = "hello world!"u8.ToArray();

            using (X509Certificate2 compositeCertificate =
                X509CertificateLoader.LoadCertificate(CompositeMLKemCmsTestData.X25519Vector.Certificate))
            using (X509Certificate2 mlKemCertificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem1024CertificatePem,
                MLKemTestData.IetfMlKem1024PrivateKeySeedPem))
            {
                CmsRecipientCollection recipients = new CmsRecipientCollection
                {
                    new CmsRecipient(compositeCertificate),
                    new CmsRecipient(mlKemCertificate),
                };
                EnvelopedCms cms = new EnvelopedCms(new ContentInfo(content));
                cms.Encrypt(recipients);
                byte[] encoded = cms.Encode();

                cms = new EnvelopedCms();
                cms.Decode(encoded);

                KemRecipientInfo first = Assert.IsType<KemRecipientInfo>(cms.RecipientInfos[0]);
                Assert.Equal(TestOids.MLKem768WithX25519Sha3_256, first.KeyEncapsulationAlgorithm.Oid.Value);
                KemRecipientInfo second = Assert.IsType<KemRecipientInfo>(cms.RecipientInfos[1]);
                Assert.Equal(TestOids.MLKem1024, second.KeyEncapsulationAlgorithm.Oid.Value);

                X509Certificate2Collection extraStore = new X509Certificate2Collection
                {
                    compositeCertificate,
                    mlKemCertificate,
                };

                cms.Decrypt(extraStore);
                AssertExtensions.SequenceEqual(content, cms.ContentInfo.Content);
            }
        }

        private static KemRecipientInfo AssertInvalidDocument(byte[] document)
        {
            EnvelopedCms cms = Decode(document);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (CompositeMLKem privateKey =
                CompositeMLKem.ImportPkcs8PrivateKey(CompositeMLKemCmsTestData.P256Vector.Pkcs8))
            {
                Assert.Throws<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }

            return recipientInfo;
        }

        private static EnvelopedCms Decode(byte[] document)
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(document);
            return cms;
        }

        private static EnvelopedCms DecodeRfcExample() =>
            Decode(Convert.FromBase64String(RfcExampleEnvelopedData));

        private static EnvelopedCms Decrypt(byte[] document, CompositeMLKemTestVector vector)
        {
            EnvelopedCms cms = Decode(document);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (CompositeMLKem privateKey = CompositeMLKem.ImportPkcs8PrivateKey(vector.Pkcs8))
            {
                cms.Decrypt(recipientInfo, privateKey);
            }

            AssertExtensions.SequenceEqual("hello world!"u8, cms.ContentInfo.Content.AsSpan());
            return cms;
        }

        private static CompositeMLKem ImportRfcExamplePrivateKey() =>
            CompositeMLKem.ImportPkcs8PrivateKey(Convert.FromBase64String(RfcExamplePrivateKey));

        [Fact]
        public static void Decrypt_CustomCompositeMLKemInstance()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.P256Vector;
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(CompositeMLKemCmsTestData.BuildEnvelopedData(vector));
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MockCompositeMLKem privateKey = new MockCompositeMLKem(vector))
            {
                cms.Decrypt(recipientInfo, privateKey);
            }

            AssertExtensions.SequenceEqual("hello world!"u8, cms.ContentInfo.Content.AsSpan());
        }

        private sealed class MockCompositeMLKem : CompositeMLKem
        {
            private readonly CompositeMLKemTestVector _vector;

            internal MockCompositeMLKem(CompositeMLKemTestVector vector)
                : base(vector.Algorithm)
            {
                _vector = vector;
            }

            protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
            {
                AssertExtensions.SequenceEqual(_vector.Ciphertext, ciphertext);
                _vector.SharedSecret.CopyTo(sharedSecret);
            }

            protected override void Dispose(bool disposing)
            {
            }

            protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret) =>
                throw new NotSupportedException();

            protected override int ExportDecapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override int ExportEncapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
                throw new NotSupportedException();
        }
    }
}
