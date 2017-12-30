// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using Xunit;

using Test.Cryptography;
using System.Security.Cryptography.Pkcs.Tests;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public static partial class DecryptTests
    {
        public static bool SupportsCngCertificates { get; } = (!PlatformDetection.IsFullFramework || PlatformDetection.IsNetfx462OrNewer);

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_IssuerAndSerial()
        {
            byte[] content = { 5, 112, 233, 43 };
            ContentInfo contentInfo = new ContentInfo(content);
            TestSimpleDecrypt_RoundTrip(Certificates.RSAKeyTransfer1, contentInfo, Oids.Aes256, SubjectIdentifierType.IssuerAndSerialNumber);
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_Ski()
        {
            byte[] content = { 6, 3, 128, 33, 44 };
            ContentInfo contentInfo = new ContentInfo(content);
            TestSimpleDecrypt_RoundTrip(Certificates.RSAKeyTransfer1, contentInfo, Oids.Aes256, SubjectIdentifierType.SubjectKeyIdentifier);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_Capi()
        {
            byte[] content = { 5, 77, 32, 33, 2, 34 };
            ContentInfo contentInfo = new ContentInfo(content);
            TestSimpleDecrypt_RoundTrip(Certificates.RSAKeyTransferCapi1, contentInfo, Oids.Aes256, SubjectIdentifierType.IssuerAndSerialNumber);
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_256()
        {
            byte[] content = { 5, 77, 32, 33, 2, 34 };
            ContentInfo contentInfo = new ContentInfo(content);
            TestSimpleDecrypt_RoundTrip(Certificates.RSASha256KeyTransfer1, contentInfo, Oids.Aes256, SubjectIdentifierType.IssuerAndSerialNumber);
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_384()
        {
            byte[] content = { 5, 77, 32, 33, 2, 34 };
            ContentInfo contentInfo = new ContentInfo(content);
            TestSimpleDecrypt_RoundTrip(Certificates.RSASha384KeyTransfer1, contentInfo, Oids.Aes256, SubjectIdentifierType.IssuerAndSerialNumber);
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_512()
        {
            byte[] content = { 5, 77, 32, 33, 2, 34 };
            ContentInfo contentInfo = new ContentInfo(content);
            TestSimpleDecrypt_RoundTrip(Certificates.RSASha512KeyTransfer1, contentInfo, Oids.Aes256, SubjectIdentifierType.IssuerAndSerialNumber);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_SignedWithinEnveloped()
        {
            byte[] content =
                 ("3082032506092a864886f70d010702a082031630820312020101310b300906052b0e03021a0500301206092a864886f70d01"
                + "0701a0050403010203a08202103082020c30820179a00302010202105d2ffff863babc9b4d3c80ab178a4cca300906052b0e"
                + "03021d0500301e311c301a060355040313135253414b65795472616e736665724361706931301e170d313530343135303730"
                + "3030305a170d3235303431353037303030305a301e311c301a060355040313135253414b65795472616e7366657243617069"
                + "3130819f300d06092a864886f70d010101050003818d0030818902818100aa272700586c0cc41b05c65c7d846f5a2bc27b03"
                + "e301c37d9bff6d75b6eb6671ba9596c5c63ba2b1af5c318d9ca39e7400d10c238ac72630579211b86570d1a1d44ec86aa8f6"
                + "c9d2b4e283ea3535923f398a312a23eaeacd8d34faaca965cd910b37da4093ef76c13b337c1afab7d1d07e317b41a336baa4"
                + "111299f99424408d0203010001a3533051304f0603551d0104483046801015432db116b35d07e4ba89edb2469d7aa120301e"
                + "311c301a060355040313135253414b65795472616e73666572436170693182105d2ffff863babc9b4d3c80ab178a4cca3009"
                + "06052b0e03021d05000381810081e5535d8eceef265acbc82f6c5f8bc9d84319265f3ccf23369fa533c8dc1938952c593166"
                + "2d9ecd8b1e7b81749e48468167e2fce3d019fa70d54646975b6dc2a3ba72d5a5274c1866da6d7a5df47938e034a075d11957"
                + "d653b5c78e5291e4401045576f6d4eda81bef3c369af56121e49a083c8d1adb09f291822e99a4296463181d73081d4020101"
                + "3032301e311c301a060355040313135253414b65795472616e73666572436170693102105d2ffff863babc9b4d3c80ab178a"
                + "4cca300906052b0e03021a0500300d06092a864886f70d010101050004818031a718ea1483c88494661e1d3dedfea0a3d97e"
                + "eb64c3e093a628b257c0cfc183ecf11697ac84f2af882b8de0c793572af38dc15d1b6f3d8f2392ba1cc71210e177c146fd16"
                + "b77a583b6411e801d7a2640d612f2fe99d87e9718e0e505a7ab9536d71dbde329da21816ce7da1416a74a3e0a112b86b33af"
                + "336a2ba6ae2443d0ab").HexToByteArray();

            ContentInfo contentInfo = new ContentInfo(new Oid(Oids.Pkcs7Signed), content);
            TestSimpleDecrypt_RoundTrip(Certificates.RSAKeyTransferCapi1, contentInfo, Oids.Aes256, SubjectIdentifierType.IssuerAndSerialNumber);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_EnvelopedWithinEnveloped()
        {
            byte[] content =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818013"
                + "dc0eb2984a445d04a1f6246b8fe41f1d24507548d449d454d5bb5e0638d75ed101bf78c0155a5d208eb746755fbccbc86923"
                + "8443760a9ae94770d6373e0197be23a6a891f0c522ca96b3e8008bf23547474b7e24e7f32e8134df3862d84f4dea2470548e"
                + "c774dd74f149a56cdd966e141122900d00ad9d10ea1848541294a1302b06092a864886f70d010701301406082a864886f70d"
                + "030704089c8119f6cf6b174c8008bcea3a10d0737eb9").HexToByteArray();


            ContentInfo contentInfo = new ContentInfo(new Oid(Oids.Pkcs7SignedEnveloped), content);
            TestSimpleDecrypt_RoundTrip(Certificates.RSAKeyTransferCapi1, contentInfo, Oids.Aes256, SubjectIdentifierType.IssuerAndSerialNumber);
        }

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

        private static void TestSimpleDecrypt_RoundTrip(CertLoader certLoader, ContentInfo contentInfo, string algorithmOidValue, SubjectIdentifierType type)
        {
            // Deep-copy the contentInfo since the real ContentInfo doesn't do this. This defends against a bad implementation changing
            // our "expectedContentInfo" to match what it produces.
            ContentInfo expectedContentInfo = new ContentInfo(new Oid(contentInfo.ContentType), (byte[])(contentInfo.Content.Clone()));

            string certSubjectName;
            byte[] encodedMessage;
            using (X509Certificate2 certificate = certLoader.GetCertificate())
            {
                certSubjectName = certificate.Subject;
                AlgorithmIdentifier alg = new AlgorithmIdentifier(new Oid(algorithmOidValue));
                EnvelopedCms ecms = new EnvelopedCms(contentInfo, alg);
                CmsRecipient cmsRecipient = new CmsRecipient(type, certificate);
                ecms.Encrypt(cmsRecipient);
                encodedMessage = ecms.Encode();
            }

            // We don't pass "certificate" down because it's expected that the certificate used for encrypting doesn't have a private key (part of the purpose of this test is
            // to ensure that you don't need the recipient's private key to encrypt.) The decrypt phase will have to locate the matching cert with the private key.
            VerifySimpleDecrypt(encodedMessage, certLoader, expectedContentInfo);
        }

        private static void VerifySimpleDecrypt(byte[] encodedMessage, CertLoader certLoader, ContentInfo expectedContent)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 cert = certLoader.TryGetCertificateWithPrivateKey())
            {
                if (cert == null)
                    return; // Sorry - CertLoader is not configured to load certs with private keys - we've tested as much as we can.

                X509Certificate2Collection extraStore = new X509Certificate2Collection(cert);
                ecms.Decrypt(extraStore);
                ContentInfo contentInfo = ecms.ContentInfo;
                Assert.Equal(expectedContent.ContentType.Value, contentInfo.ContentType.Value);
                Assert.Equal<byte>(expectedContent.Content, contentInfo.Content);
            }
        }
    }
}


