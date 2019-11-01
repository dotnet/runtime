// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Xunit;

using System.Security.Cryptography.Pkcs.Tests;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public static partial class KeyTransRecipientInfoRsaOaepCertTests
    {
        public static bool SupportsRsaOaepCerts => PlatformDetection.IsWindows;
        public static bool DoesNotSupportRsaOaepCerts => !SupportsRsaOaepCerts;

        [ConditionalFact(nameof(SupportsRsaOaepCerts))]
        public static void TestKeyTransEncryptKey_RsaOaepCertificate_NoParameters_DefaultToSha1()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RsaOaep2048_NoParameters.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();

            EnvelopedCms ecms2 = new EnvelopedCms();
            ecms2.Decode(encodedMessage);

            RecipientInfoCollection recipients = ecms2.RecipientInfos;
            Assert.Equal(1, recipients.Count);
            KeyTransRecipientInfo recipient = Assert.IsType<KeyTransRecipientInfo>(recipients[0]);
            Assert.Equal(Oids.RsaOaep, recipient.KeyEncryptionAlgorithm.Oid.Value);
            Assert.Equal(s_rsaOaepSha1Parameters, recipient.KeyEncryptionAlgorithm.Parameters);
        }

        [ConditionalFact(nameof(SupportsRsaOaepCerts))]
        public static void TestKeyTransEncryptKey_RsaOaepCertificate_Sha256Parameters()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RsaOaep2048_Sha256Parameters.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();

            EnvelopedCms ecms2 = new EnvelopedCms();
            ecms2.Decode(encodedMessage);

            RecipientInfoCollection recipients = ecms2.RecipientInfos;
            Assert.Equal(1, recipients.Count);
            KeyTransRecipientInfo recipient = Assert.IsType<KeyTransRecipientInfo>(recipients[0]);
            Assert.Equal(Oids.RsaOaep, recipient.KeyEncryptionAlgorithm.Oid.Value);
            Assert.Equal(s_rsaOaepSha256Parameters, recipient.KeyEncryptionAlgorithm.Parameters);
        }

        [ConditionalFact(nameof(DoesNotSupportRsaOaepCerts))]
        public static void TestKeyTransEncryptKey_RsaOaepCertificate_NoPlatformSupport_Throws()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RsaOaep2048_NoParameters.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                Assert.Throws<CryptographicException>(() => ecms.Encrypt(cmsRecipient));
            }
        }

        private static readonly byte[] s_rsaOaepSha1Parameters = { 0x30, 0x00 };
        private static readonly byte[] s_rsaOaepSha256Parameters = { 0x30, 0x2f, 0xa0, 0x0f, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0xa1, 0x1c, 0x30, 0x1a, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x08, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00 };
    }
}
