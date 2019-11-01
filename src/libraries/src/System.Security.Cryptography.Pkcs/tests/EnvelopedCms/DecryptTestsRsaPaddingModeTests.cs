// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography.Pkcs.Tests;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public class DecryptTestsRsaPaddingMode : DecryptTests
    {
        public static bool SupportsDiffieHellman { get; } = KeyAgreeRecipientInfoTests.SupportsDiffieHellman;

        public DecryptTestsRsaPaddingMode() : base(false)
        {
        }

        [Theory]
        [MemberData(nameof(Roundtrip_RsaPaddingModes_TestData))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Roundtrip_RsaPaddingModes(RSAEncryptionPadding rsaEncryptionPadding)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RSA2048Sha256KeyTransfer1.GetCertificate())
            {
                CmsRecipient recipient = new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, cert, rsaEncryptionPadding);
                ecms.Encrypt(recipient);
            }

            byte[] encodedMessage = ecms.Encode();

            ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 privateCert = Certificates.RSA2048Sha256KeyTransfer1.TryGetCertificateWithPrivateKey())
            {
                if (privateCert == null)
                    return; // CertLoader can't load the private certificate.

                ecms.Decrypt(new X509Certificate2Collection(privateCert));
            }
            Assert.Equal(contentInfo.ContentType.Value, ecms.ContentInfo.ContentType.Value);
            Assert.Equal<byte>(contentInfo.Content, ecms.ContentInfo.Content);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void MultipleRecipientIdentifiers_RoundTrip_DifferingRsaPaddingModes()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            CmsRecipientCollection recipients = new CmsRecipientCollection();
            using (X509Certificate2 issuerSerialCert = Certificates.RSAKeyTransfer1.GetCertificate())
            using (X509Certificate2 explicitSkiCert = Certificates.RSAKeyTransfer_ExplicitSki.GetCertificate())
            {
                // CmsRecipients have different identifiers to test multiple identifier encryption.
                recipients.Add(new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, issuerSerialCert, RSAEncryptionPadding.OaepSHA1));
                recipients.Add(new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, explicitSkiCert, RSAEncryptionPadding.OaepSHA256));
                ecms.Encrypt(recipients);
            }

            byte[] encodedMessage = ecms.Encode();

            ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 privateIssuerSerialCert = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            {
                if (privateIssuerSerialCert != null)
                    return; // CertLoader can't load the private certificate.

                ecms.Decrypt(new X509Certificate2Collection(privateIssuerSerialCert));
            }

            using (X509Certificate2 privateExplicitSkiCert = Certificates.RSAKeyTransfer_ExplicitSki.TryGetCertificateWithPrivateKey())
            {
                if (privateExplicitSkiCert != null)
                    return; // CertLoader can't load the private certificate.

                ecms.Decrypt(new X509Certificate2Collection(privateExplicitSkiCert));
            }
        }

        [ConditionalFact(nameof(SupportsDiffieHellman))]
        public static void CmsRecipient_RejectsNonRSACertificateWithRSAPadding()
        {
            using (X509Certificate2 keyAgreeCertificate = Certificates.DHKeyAgree1.GetCertificate())
            {
                Assert.Throws<CryptographicException>(() => {
                    _ = new CmsRecipient(keyAgreeCertificate, RSAEncryptionPadding.OaepSHA1);
                });
                Assert.Throws<CryptographicException>(() => {
                    _ = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, keyAgreeCertificate, RSAEncryptionPadding.OaepSHA1);
                });
            }
        }

        public static IEnumerable<object[]> Roundtrip_RsaPaddingModes_TestData
        {
            get
            {
                yield return new object[] { RSAEncryptionPadding.OaepSHA1 };
                yield return new object[] { RSAEncryptionPadding.OaepSHA256 };
                yield return new object[] { RSAEncryptionPadding.OaepSHA384 };
                yield return new object[] { RSAEncryptionPadding.OaepSHA512 };
                yield return new object[] { RSAEncryptionPadding.Pkcs1 };
            }
        }
    }
}
