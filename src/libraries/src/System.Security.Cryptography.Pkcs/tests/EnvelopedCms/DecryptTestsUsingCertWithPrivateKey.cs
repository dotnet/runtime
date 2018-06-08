// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Pkcs.Tests;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public class DecryptTestsUsingCertWithPrivateKey : DecryptTests
    {
        public DecryptTestsUsingCertWithPrivateKey() : base(false) { }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void DecryptMultipleRecipients()
        {
            // Force Decrypt() to try multiple recipients. Ensure that a failure to find a matching cert in one doesn't cause it to quit early.

            CertLoader[] certLoaders = new CertLoader[]
            {
                    Certificates.RSAKeyTransfer1,
                    Certificates.RSAKeyTransfer2,
                    Certificates.RSAKeyTransfer3,
            };

            byte[] content = { 6, 3, 128, 33, 44 };
            EnvelopedCms ecms = new EnvelopedCms(new ContentInfo(content), new AlgorithmIdentifier(new Oid(Oids.Aes256)));
            CmsRecipientCollection recipients = new CmsRecipientCollection();
            foreach (CertLoader certLoader in certLoaders)
            {
                recipients.Add(new CmsRecipient(certLoader.GetCertificate()));
            }
            ecms.Encrypt(recipients);
            byte[] encodedMessage = ecms.Encode();

            ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            // How do we know that Decrypt() tries receipients in the order they appear in ecms.RecipientInfos? Because we wrote the implementation.
            // Not that some future implementation can't ever change it but it's the best guess we have.
            RecipientInfo me = ecms.RecipientInfos[2];

            CertLoader matchingCertLoader = null;
            for (int index = 0; index < recipients.Count; index++)
            {
                if (recipients[index].Certificate.Issuer == ((X509IssuerSerial)(me.RecipientIdentifier.Value)).IssuerName)
                {
                    matchingCertLoader = certLoaders[index];
                    break;
                }
            }
            Assert.NotNull(matchingCertLoader);

            using (X509Certificate2 cert = matchingCertLoader.TryGetCertificateWithPrivateKey())
            {
                if (cert == null)
                    return; // Sorry - CertLoader is not configured to load certs with private keys - we've tested as much as we can.
                X509Certificate2Collection extraStore = new X509Certificate2Collection();
                extraStore.Add(cert);
                ecms.Decrypt(extraStore);
            }

            ContentInfo contentInfo = ecms.ContentInfo;
            Assert.Equal<byte>(content, contentInfo.Content);
        }

        [Fact]
        public static void DecryptUsingCertificateWithSameSubjectKeyIdentifierButDifferentKeyPair()
        {
            using (X509Certificate2 recipientCert = Certificates.RSAKeyTransfer4_ExplicitSki.GetCertificate())
            using (X509Certificate2 otherRecipientWithSameSki = Certificates.RSAKeyTransfer5_ExplicitSkiOfRSAKeyTransfer4.TryGetCertificateWithPrivateKey())
            using (X509Certificate2 realRecipientCert = Certificates.RSAKeyTransfer4_ExplicitSki.TryGetCertificateWithPrivateKey())
            {
                Assert.Equal(recipientCert, realRecipientCert);
                Assert.NotEqual(recipientCert, otherRecipientWithSameSki);
                Assert.Equal(GetSubjectKeyIdentifier(recipientCert), GetSubjectKeyIdentifier(otherRecipientWithSameSki));

                byte[] plainText = new byte[] { 1, 3, 7, 9 };

                ContentInfo content = new ContentInfo(plainText);
                EnvelopedCms ecms = new EnvelopedCms(content);

                CmsRecipient recipient = new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, recipientCert);
                ecms.Encrypt(recipient);
                byte[] encoded = ecms.Encode();

                ecms = new EnvelopedCms();
                ecms.Decode(encoded);

                Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(new X509Certificate2Collection(otherRecipientWithSameSki)));
                ecms.Decrypt(new X509Certificate2Collection(realRecipientCert));

                Assert.Equal(plainText, ecms.ContentInfo.Content);
            }
        }

        private static string GetSubjectKeyIdentifier(X509Certificate2 cert)
        {
            foreach (var ext in cert.Extensions)
            {
                X509SubjectKeyIdentifierExtension skiExt = ext as X509SubjectKeyIdentifierExtension;
                if (skiExt != null)
                {
                    return skiExt.SubjectKeyIdentifier;
                }
            }

            Assert.False(true, "Subject Key Identifier not found");
            return null;
        }
    }
}
