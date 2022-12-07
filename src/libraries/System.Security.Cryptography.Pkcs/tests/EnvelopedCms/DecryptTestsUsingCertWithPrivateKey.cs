// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Fact]
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
        public static void DecryptSuccessfullyWithWrongKeyProducesInvalidSymmetricKey()
        {
            using (X509Certificate2 wrongRecipient = Certificates.RSAKeyTransfer5_ExplicitSkiOfRSAKeyTransfer4.TryGetCertificateWithPrivateKey())
            {
                // This is an enveloped CMS that is encrypted with one RSA key recipient
                // but does not fail when decrypting the content encryption key (CEK).
                // Though it did not fail, the CEK is wrong and cannot decrypt the data
                // for the recipient. It might decrypt the CEK to a key that is an invalid
                // for the symmetric algorithm, like a 120-bit key for AES. For that case,
                // the symmetric decryption would throw an ArgumentException, not a
                // CryptographicException. This tests that circumstance where we need to
                // wrap the ArgumentException with a CryptographicException.
                //
                // This content can be re-created with trial-and-error with the managed PAL.
                //
                // Two certificates with the same SKI are required.
                // using X509Certificate2 cert1 = ...
                // using X509Certificate2 cert2 = ...
                // while (true) {
                //   EnvelopedCms ecms = new EnvelopedCms(..);
                //   CmsRecipient recipient = new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, cert1);
                //   ecms.Encrypt(recipient);
                //   byte[] encoded = ecms.Encode();
                //   ecms = new EnvelopedCms();
                //   ecms.Decode(encoded);
                //   try {
                //     ecms.Decrypt(new X509Certificate2Collection(cert2));
                //   }
                //   catch (CryptographicException e) when e.Message == SR.Cryptography_Cms_InvalidSymmetricKey;
                //     // If we get here, we've produced an EnvelopedCms with the needed criteria.
                //     break;
                //   }
                // }
                string encryptedContent =
"3082018806092A864886F70D010703A082017930820175020102318201303082" +
"012C0201028014B46B61938FF9864BD8494B3937DA19C9F06FA8D3300D06092A" +
"864886F70D0101010500048201008198CBAFF1C67EE634C7A32C356729F996BC" +
"E4125EE353B220A792CCFD37855B50E05916CC8EC6E25EF62B35B29620C8BF76" +
"144032854D14E3E19B613C15A26E376A2014AD3AD492F80A92F0D61910B6C416" +
"867985279CF4E26CDED351AFB84CE9E1BC105899280DB6B782688CE6B04B7003" +
"E4C53B580DD2F21A71B973C2AB70E61F1AFBDD2616FE0101BB02BCDA14881CEC" +
"037032C91FE803C76D91FA5E0A802ECB2FB2BBE71C8567F58B5B74638CD9765E" +
"F658172AAD423963784C5BE49AC01682751796F0AFE4943373981FC074F24640" +
"901201AD6884415788FC18721ECB201A60A7FE5859FF61DA8BB21D0D23593D28" +
"86896886D3507906DB58FB056953303C06092A864886F70D010701301D060960" +
"864801650304012A0410C85C553A73E9B55F98752E1133ACA645801099B22DFF" +
"6A9D8984F8B3F63079CE9265";
                EnvelopedCms ecms = new EnvelopedCms();
                ecms.Decode(encryptedContent.HexToByteArray());
                Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(new X509Certificate2Collection(wrongRecipient)));
            }
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
