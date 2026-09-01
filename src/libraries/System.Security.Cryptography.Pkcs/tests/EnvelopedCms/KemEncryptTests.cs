// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;

using Xunit;

using TestCertificates = System.Security.Cryptography.Pkcs.Tests.Certificates;
using TestOids = System.Security.Cryptography.Pkcs.Tests.Oids;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static class KemEncryptTests
    {
        public static TheoryData<string, byte[], string, int> MlKemParameterSets { get; } =
            new TheoryData<string, byte[], string, int>
            {
                {
                    MLKemTestData.IetfMlKem512CertificatePem,
                    MLKemTestData.IetfMlKem512PrivateKeySeed,
                    TestOids.MLKem512,
                    768
                },
                {
                    MLKemTestData.IetfMlKem768CertificatePem,
                    MLKemTestData.IetfMlKem768PrivateKeySeed,
                    TestOids.MLKem768,
                    1088
                },
                {
                    MLKemTestData.IetfMlKem1024CertificatePem,
                    MLKemTestData.IetfMlKem1024PrivateKeySeed,
                    TestOids.MLKem1024,
                    1568
                },
            };

        [Theory]
        [InlineData(TestOids.Aes128, 16)]
        [InlineData(TestOids.Aes192, 24)]
        [InlineData(TestOids.Aes256, 32)]
        public static void EncryptAndDecryptContentEncryptionAlgorithm(
            string contentEncryptionAlgorithm,
            int contentEncryptionKeyLength)
        {
            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                CmsRecipient recipient = new CmsRecipient(certificate);

                EncryptAndDecrypt(
                    recipient,
                    MLKemTestData.IetfMlKem768PrivateKeySeed,
                    TestOids.MLKem768,
                    1088,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    expectedUkm: null,
                    contentEncryptionAlgorithm,
                    contentEncryptionKeyLength);
            }
        }

        [Theory]
        [MemberData(nameof(MlKemParameterSets))]
        public static void EncryptAndDecryptMlKemParameterSet(
            string certificatePem,
            byte[] privateKey,
            string expectedKemAlgorithm,
            int expectedCiphertextLength)
        {
            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(certificatePem))
            {
                CmsRecipient recipient = new CmsRecipient(certificate);

                EncryptAndDecrypt(
                    recipient,
                    privateKey,
                    expectedKemAlgorithm,
                    expectedCiphertextLength,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    expectedUkm: null,
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [Fact]
        public static void EncryptAndDecryptSubjectKeyIdentifier()
        {
            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                CmsRecipient recipient = new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, certificate);

                EncryptAndDecrypt(
                    recipient,
                    MLKemTestData.IetfMlKem768PrivateKeySeed,
                    TestOids.MLKem768,
                    1088,
                    SubjectIdentifierType.SubjectKeyIdentifier,
                    expectedUkm: null,
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [Fact]
        public static void EncryptAndDecryptFactoryWithEmptyUkm()
        {
            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                CmsRecipient recipient = CmsRecipient.CreateForKeyEncapsulation(certificate, []);

                EncryptAndDecrypt(
                    recipient,
                    MLKemTestData.IetfMlKem768PrivateKeySeed,
                    TestOids.MLKem768,
                    1088,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    expectedUkm: [],
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [Fact]
        public static void EncryptAndDecryptFactoryWithUkmAndSubjectKeyIdentifier()
        {
            byte[] userKeyingMaterial = [1, 2, 3, 4, 5];

            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                CmsRecipient recipient = CmsRecipient.CreateForKeyEncapsulation(
                    SubjectIdentifierType.SubjectKeyIdentifier,
                    certificate,
                    userKeyingMaterial);

                EncryptAndDecrypt(
                    recipient,
                    MLKemTestData.IetfMlKem768PrivateKeySeed,
                    TestOids.MLKem768,
                    1088,
                    SubjectIdentifierType.SubjectKeyIdentifier,
                    userKeyingMaterial,
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [Fact]
        public static void CreateForKeyEncapsulationCopiesUkm()
        {
            byte[] userKeyingMaterial = [1, 2, 3, 4, 5];
            byte[] expectedUkm = userKeyingMaterial.AsSpan().ToArray();

            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                CmsRecipient recipient = CmsRecipient.CreateForKeyEncapsulation(certificate, userKeyingMaterial);
                userKeyingMaterial.AsSpan().Fill(0xFF);

                EncryptAndDecrypt(
                    recipient,
                    MLKemTestData.IetfMlKem768PrivateKeySeed,
                    TestOids.MLKem768,
                    1088,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    expectedUkm,
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void CreateForKeyEncapsulationRejectsNonKemCertificate(bool specifyRecipientIdentifier)
        {
            using (X509Certificate2 certificate = TestCertificates.RSAKeyTransfer1.GetCertificate())
            {
                if (specifyRecipientIdentifier)
                {
                    Assert.Throws<CryptographicException>(() =>
                        CmsRecipient.CreateForKeyEncapsulation(
                            SubjectIdentifierType.SubjectKeyIdentifier,
                            certificate,
                            []));
                }
                else
                {
                    Assert.Throws<CryptographicException>(() =>
                        CmsRecipient.CreateForKeyEncapsulation(certificate, []));
                }
            }
        }

        [Fact]
        public static void CreateForKeyEncapsulationNullCertificate()
        {
            byte[] userKeyingMaterial = [];

            Assert.Throws<ArgumentNullException>(() =>
                CmsRecipient.CreateForKeyEncapsulation(null, userKeyingMaterial));

            Assert.Throws<ArgumentNullException>(() =>
                CmsRecipient.CreateForKeyEncapsulation(
                    SubjectIdentifierType.SubjectKeyIdentifier,
                    null,
                    userKeyingMaterial));
        }

        [Fact]
        public static void EncryptInvalidContentEncryptionKeySize()
        {
            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                EnvelopedCms cms = new EnvelopedCms(
                    new ContentInfo("hello world!"u8.ToArray()),
                    new AlgorithmIdentifier(new Oid(TestOids.Des)));

                Assert.Throws<CryptographicException>(() => cms.Encrypt(new CmsRecipient(certificate)));
            }
        }

        [Fact]
        public static void EncryptAndDecryptMixedRsaAndMlKemRecipients()
        {
            byte[] content = "hello world!"u8.ToArray();

            using (X509Certificate2 mlKemCertificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            using (X509Certificate2 rsaCertificate = TestCertificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipientCollection recipients = new CmsRecipientCollection
                {
                    new CmsRecipient(rsaCertificate),
                    new CmsRecipient(mlKemCertificate),
                };

                EnvelopedCms cms = new EnvelopedCms(new ContentInfo(content));
                cms.Encrypt(recipients);
                byte[] encoded = cms.Encode();

                cms = new EnvelopedCms();
                cms.Decode(encoded);

                Assert.Equal(2, cms.RecipientInfos.Count);
                KemRecipientInfo? kemRecipientInfo = null;
                KeyTransRecipientInfo? keyTransRecipientInfo = null;

                foreach (RecipientInfo recipientInfo in cms.RecipientInfos)
                {
                    if (recipientInfo is KemRecipientInfo kem)
                    {
                        kemRecipientInfo = kem;
                    }
                    else if (recipientInfo is KeyTransRecipientInfo keyTrans)
                    {
                        keyTransRecipientInfo = keyTrans;
                    }
                }

                Assert.NotNull(kemRecipientInfo);
                Assert.NotNull(keyTransRecipientInfo);

                using (MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
                {
                    cms.Decrypt(kemRecipientInfo, privateKey);
                }

                Assert.Equal(content, cms.ContentInfo.Content);
            }
        }

        [Fact]
        public static void EncryptMultipleMlKemRecipientsUseDistinctCiphertexts()
        {
            byte[] content = "hello world!"u8.ToArray();

            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                CmsRecipientCollection recipients = new CmsRecipientCollection
                {
                    new CmsRecipient(certificate),
                    new CmsRecipient(certificate),
                };

                EnvelopedCms cms = new EnvelopedCms(new ContentInfo(content));
                cms.Encrypt(recipients);
                byte[] encoded = cms.Encode();

                cms = new EnvelopedCms();
                cms.Decode(encoded);

                Assert.Equal(2, cms.RecipientInfos.Count);
                KemRecipientInfo first = Assert.IsType<KemRecipientInfo>(cms.RecipientInfos[0]);
                KemRecipientInfo second = Assert.IsType<KemRecipientInfo>(cms.RecipientInfos[1]);
                Assert.False(first.KeyEncapsulationCiphertext.Span.SequenceEqual(second.KeyEncapsulationCiphertext.Span));

                using (MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
                {
                    cms.Decrypt(first, privateKey);
                }

                Assert.Equal(content, cms.ContentInfo.Content);
            }
        }

        private static void EncryptAndDecrypt(
            CmsRecipient recipient,
            byte[] privateKey,
            string expectedKemAlgorithm,
            int expectedCiphertextLength,
            SubjectIdentifierType expectedRecipientIdentifierType,
            byte[]? expectedUkm,
            string contentEncryptionAlgorithm,
            int contentEncryptionKeyLength)
        {
            byte[] content = "hello world!"u8.ToArray();
            EnvelopedCms cms = new EnvelopedCms(
                new ContentInfo(content),
                new AlgorithmIdentifier(new Oid(contentEncryptionAlgorithm)));

            cms.Encrypt(recipient);
            byte[] encoded = cms.Encode();

            cms = new EnvelopedCms();
            cms.Decode(encoded);

            Assert.Equal(3, cms.Version);
            Assert.Equal(contentEncryptionAlgorithm, cms.ContentEncryptionAlgorithm.Oid.Value);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
            Assert.Equal(0, recipientInfo.Version);
            Assert.Equal(expectedRecipientIdentifierType, recipientInfo.RecipientIdentifier.Type);
            Assert.Equal(expectedKemAlgorithm, recipientInfo.KeyEncapsulationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncapsulationAlgorithm.Parameters);
            Assert.Equal(expectedCiphertextLength, recipientInfo.KeyEncapsulationCiphertext.Length);
            Assert.Equal(TestOids.HkdfSha384, recipientInfo.KeyDerivationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyDerivationAlgorithm.Parameters);
            Assert.Equal(32, recipientInfo.KeyEncryptionKeyLengthInBytes);
            Assert.Equal(TestOids.Aes256Wrap, recipientInfo.KeyEncryptionAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncryptionAlgorithm.Parameters);
            Assert.Equal(contentEncryptionKeyLength + 8, recipientInfo.EncryptedKey.Length);

            if (expectedUkm is null)
            {
                Assert.Null(recipientInfo.UserKeyingMaterial);
            }
            else
            {
                Assert.True(recipientInfo.UserKeyingMaterial.HasValue);
                Assert.Equal<byte>(expectedUkm, recipientInfo.UserKeyingMaterial.Value.ToArray());
            }

            using (MLKem mlKem = MLKem.ImportPkcs8PrivateKey(privateKey))
            {
                cms.Decrypt(recipientInfo, mlKem);
            }

            Assert.Equal(content, cms.ContentInfo.Content);
        }
    }
}
