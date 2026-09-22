// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;

using Xunit;

using TestCertificates = System.Security.Cryptography.Pkcs.Tests.Certificates;
using TestOids = System.Security.Cryptography.Pkcs.Tests.Oids;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    public static class CompositeMLKemEncryptTests
    {
        public static bool IsX25519Supported =>
            CompositeMLKem.IsAlgorithmSupported(CompositeMLKemAlgorithm.MLKem768WithX25519);

        [ConditionalTheory(typeof(CompositeMLKemEncryptTests), nameof(IsX25519Supported))]
        [InlineData(TestOids.Aes128, 16)]
        [InlineData(TestOids.Aes192, 24)]
        [InlineData(TestOids.Aes256, 32)]
        public static void EncryptAndDecryptContentEncryptionAlgorithm(
            string contentEncryptionAlgorithm,
            int contentEncryptionKeyLength)
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.X25519Vector;

            using (X509Certificate2 certificate = GetCertificate(vector))
            {
                EncryptAndDecrypt(
                    vector,
                    new CmsRecipient(certificate),
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    expectedUkm: null,
                    contentEncryptionAlgorithm,
                    contentEncryptionKeyLength);
            }
        }

        [Theory]
        [MemberData(
            nameof(CompositeMLKemTestData.SupportedAlgorithmIetfVectorsTestData),
            MemberType = typeof(CompositeMLKemTestData))]
        public static void EncryptAndDecryptAlgorithm(CompositeMLKemTestVector vector)
        {
            using (X509Certificate2 certificate = GetCertificate(vector))
            {
                EncryptAndDecrypt(
                    vector,
                    new CmsRecipient(certificate),
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    expectedUkm: null,
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [ConditionalFact(typeof(CompositeMLKemEncryptTests), nameof(IsX25519Supported))]
        public static void EncryptAndDecryptSubjectKeyIdentifier()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.X25519Vector;

            using (X509Certificate2 certificate = GetCertificate(vector))
            {
                EncryptAndDecrypt(
                    vector,
                    new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, certificate),
                    SubjectIdentifierType.SubjectKeyIdentifier,
                    expectedUkm: null,
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [ConditionalFact(typeof(CompositeMLKemEncryptTests), nameof(IsX25519Supported))]
        public static void EncryptAndDecryptFactoryWithEmptyUkm()
        {
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.X25519Vector;

            using (X509Certificate2 certificate = GetCertificate(vector))
            {
                EncryptAndDecrypt(
                    vector,
                    CmsRecipient.CreateForKeyEncapsulation(certificate, []),
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    expectedUkm: [],
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [ConditionalFact(typeof(CompositeMLKemEncryptTests), nameof(IsX25519Supported))]
        public static void EncryptAndDecryptFactoryWithUkmAndSubjectKeyIdentifier()
        {
            byte[] userKeyingMaterial = [1, 2, 3, 4, 5];
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.X25519Vector;

            using (X509Certificate2 certificate = GetCertificate(vector))
            {
                EncryptAndDecrypt(
                    vector,
                    CmsRecipient.CreateForKeyEncapsulation(
                        SubjectIdentifierType.SubjectKeyIdentifier,
                        certificate,
                        userKeyingMaterial),
                    SubjectIdentifierType.SubjectKeyIdentifier,
                    userKeyingMaterial,
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [ConditionalFact(typeof(CompositeMLKemEncryptTests), nameof(IsX25519Supported))]
        public static void CreateForKeyEncapsulationCopiesUkm()
        {
            byte[] userKeyingMaterial = [1, 2, 3, 4, 5];
            byte[] expectedUkm = userKeyingMaterial.AsSpan().ToArray();
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.X25519Vector;

            using (X509Certificate2 certificate = GetCertificate(vector))
            {
                CmsRecipient recipient =
                    CmsRecipient.CreateForKeyEncapsulation(certificate, userKeyingMaterial);

                userKeyingMaterial.AsSpan().Fill(0xFF);

                EncryptAndDecrypt(
                    vector,
                    recipient,
                    SubjectIdentifierType.IssuerAndSerialNumber,
                    expectedUkm,
                    TestOids.Aes256,
                    contentEncryptionKeyLength: 32);
            }
        }

        [ConditionalFact(typeof(CompositeMLKemEncryptTests), nameof(IsX25519Supported))]
        public static void EncryptInvalidContentEncryptionKeySize()
        {
            using (X509Certificate2 certificate = GetCertificate(CompositeMLKemCmsTestData.X25519Vector))
            {
                EnvelopedCms cms = new EnvelopedCms(
                    new ContentInfo("hello world!"u8.ToArray()),
                    new AlgorithmIdentifier(new Oid(TestOids.Des)));

                Assert.Throws<CryptographicException>(() => cms.Encrypt(new CmsRecipient(certificate)));
            }
        }

        [ConditionalFact(typeof(CompositeMLKemEncryptTests), nameof(IsX25519Supported))]
        public static void EncryptAndDecryptMixedRsaAndCompositeMLKemRecipients()
        {
            byte[] content = "hello world!"u8.ToArray();
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.X25519Vector;

            using (X509Certificate2 compositeCertificate = GetCertificate(vector))
            using (X509Certificate2 rsaCertificate = TestCertificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipientCollection recipients = new CmsRecipientCollection
                {
                    new CmsRecipient(rsaCertificate),
                    new CmsRecipient(compositeCertificate),
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

                using (CompositeMLKem privateKey = CompositeMLKem.ImportPkcs8PrivateKey(vector.Pkcs8))
                {
                    cms.Decrypt(kemRecipientInfo, privateKey);
                }

                AssertExtensions.SequenceEqual(content, cms.ContentInfo.Content);

                using (X509Certificate2 privateRsaCert =
                    TestCertificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
                {
                    if (privateRsaCert is null)
                    {
                        return;
                    }

                    cms = new EnvelopedCms();
                    cms.Decode(encoded);
                    keyTransRecipientInfo = null;

                    foreach (RecipientInfo recipientInfo in cms.RecipientInfos)
                    {
                        if (recipientInfo is KeyTransRecipientInfo keyTrans)
                        {
                            keyTransRecipientInfo = keyTrans;
                            break;
                        }
                    }

                    Assert.NotNull(keyTransRecipientInfo);

                    using (RSA privateKey = privateRsaCert.GetRSAPrivateKey())
                    {
                        cms.Decrypt(keyTransRecipientInfo, privateKey);
                    }
                }

                AssertExtensions.SequenceEqual(content, cms.ContentInfo.Content);
            }
        }

        [ConditionalFact(typeof(CompositeMLKemEncryptTests), nameof(IsX25519Supported))]
        public static void EncryptMultipleRecipientsUseDistinctCiphertexts()
        {
            byte[] content = "hello world!"u8.ToArray();
            CompositeMLKemTestVector vector = CompositeMLKemCmsTestData.X25519Vector;

            using (X509Certificate2 certificate = GetCertificate(vector))
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
                Assert.False(
                    first.KeyEncapsulationCiphertext.Span.SequenceEqual(
                        second.KeyEncapsulationCiphertext.Span));

                using (CompositeMLKem privateKey = CompositeMLKem.ImportPkcs8PrivateKey(vector.Pkcs8))
                {
                    cms.Decrypt(first, privateKey);
                }

                AssertExtensions.SequenceEqual(content, cms.ContentInfo.Content);
            }
        }

        private static void EncryptAndDecrypt(
            CompositeMLKemTestVector vector,
            CmsRecipient recipient,
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
            Assert.Equal(GetOid(vector), recipientInfo.KeyEncapsulationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncapsulationAlgorithm.Parameters);
            Assert.Equal(vector.Algorithm.CiphertextSizeInBytes, recipientInfo.KeyEncapsulationCiphertext.Length);
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
                AssertExtensions.SequenceEqual(expectedUkm, recipientInfo.UserKeyingMaterial.Value.Span);
            }

            using (CompositeMLKem privateKey = CompositeMLKem.ImportPkcs8PrivateKey(vector.Pkcs8))
            {
                cms.Decrypt(recipientInfo, privateKey);
            }

            AssertExtensions.SequenceEqual(content, cms.ContentInfo.Content);
        }

        private static X509Certificate2 GetCertificate(CompositeMLKemTestVector vector) =>
            X509CertificateLoader.LoadCertificate(vector.Certificate);

        private static string GetOid(CompositeMLKemTestVector vector)
        {
            AsnReader reader = new AsnReader(vector.Spki.ToArray(), AsnEncodingRules.DER);
            return reader.ReadSequence().ReadSequence().ReadObjectIdentifier();
        }
    }
}
