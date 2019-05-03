// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Xunit;

using System.Security.Cryptography.Pkcs.Tests;
using Test.Cryptography;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public static partial class KeyTransRecipientInfoRsaPaddingModeTests
    {
        public static bool SupportsRsaOaepCerts => PlatformDetection.IsWindows;

        [Theory]
        [MemberData(nameof(TestKeyTransEncryptedKey_RsaAlgorithmTypes))]
        public static void TestKeyTransEncryptedKey_RsaAlgorithms(RSAEncryptionPadding encryptionPadding, string expectedOid, byte[] expectedParameters)
        {
            KeyTransRecipientInfo recipientInfo1 = EncodeKeyTransl_Rsa2048(encryptionPadding, Certificates.RSA2048Sha256KeyTransfer1);
            Assert.Equal(expectedOid, recipientInfo1.KeyEncryptionAlgorithm.Oid.Value);
            Assert.Equal(expectedParameters, recipientInfo1.KeyEncryptionAlgorithm.Parameters);
        }

        [ConditionalFact(nameof(SupportsRsaOaepCerts))]
        public static void TestKeyTransEncryptedKey_RsaAlgorithms_Recipient_PreferredOverCertificate()
        {
            KeyTransRecipientInfo recipientInfo1 = EncodeKeyTransl_Rsa2048(RSAEncryptionPadding.OaepSHA256, Certificates.RsaOaep2048_Sha1Parameters);
            Assert.Equal(Oids.RsaOaep, recipientInfo1.KeyEncryptionAlgorithm.Oid.Value);
            Assert.Equal(s_rsaOaepSha256Parameters, recipientInfo1.KeyEncryptionAlgorithm.Parameters);
        }

        [Fact]
        public static void TestKeyTransEncryptedKey_RsaOaepMd5_Throws()
        {
            RSAEncryptionPadding oaepMd5Padding = RSAEncryptionPadding.CreateOaep(HashAlgorithmName.MD5);
            Assert.ThrowsAny<CryptographicException>(() => {
                EncodeKeyTransl_Rsa2048(oaepMd5Padding, Certificates.RSA2048Sha256KeyTransfer1);
            });
        }

        public static IEnumerable<object[]> TestKeyTransEncryptedKey_RsaAlgorithmTypes
        {
            get
            {
                yield return new object[] { null, Oids.Rsa, Array.Empty<byte>() };
                yield return new object[] { RSAEncryptionPadding.Pkcs1, Oids.Rsa, Array.Empty<byte>() };
                yield return new object[] { RSAEncryptionPadding.OaepSHA1, Oids.RsaOaep, s_rsaOaepSha1Parameters };
                yield return new object[] { RSAEncryptionPadding.OaepSHA256, Oids.RsaOaep, s_rsaOaepSha256Parameters };
                yield return new object[] { RSAEncryptionPadding.OaepSHA384, Oids.RsaOaep, s_rsaOaepSha384Parameters };
                yield return new object[] { RSAEncryptionPadding.OaepSHA512, Oids.RsaOaep, s_rsaOaepSha512Parameters };
            }
        }

        private static KeyTransRecipientInfo EncodeKeyTransl_Rsa2048(RSAEncryptionPadding encryptionPadding, CertLoader loader)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = loader.GetCertificate())
            {
                CmsRecipient cmsRecipient;
                if (encryptionPadding is null)
                {
                    cmsRecipient = new CmsRecipient(cert);
                }
                else
                {
                    cmsRecipient = new CmsRecipient(cert, encryptionPadding);
                }

                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();

            EnvelopedCms ecms2 = new EnvelopedCms();
            ecms2.Decode(encodedMessage);

            RecipientInfoCollection recipients = ecms2.RecipientInfos;
            Assert.Equal(1, recipients.Count);
            RecipientInfo recipientInfo = recipients[0];
            Assert.IsType<KeyTransRecipientInfo>(recipientInfo);
            return (KeyTransRecipientInfo)recipientInfo;
        }

        private static readonly byte[] s_rsaOaepSha1Parameters = { 0x30, 0x00 };
        private static readonly byte[] s_rsaOaepSha256Parameters = { 0x30, 0x2f, 0xa0, 0x0f, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0xa1, 0x1c, 0x30, 0x1a, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x08, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00 };
        private static readonly byte[] s_rsaOaepSha384Parameters = { 0x30, 0x2f, 0xa0, 0x0f, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x05, 0x00, 0xa1, 0x1c, 0x30, 0x1a, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x08, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x05, 0x00 };
        private static readonly byte[] s_rsaOaepSha512Parameters = { 0x30, 0x2f, 0xa0, 0x0f, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05, 0x00, 0xa1, 0x1c, 0x30, 0x1a, 0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x08, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05, 0x00 };
    }
}
