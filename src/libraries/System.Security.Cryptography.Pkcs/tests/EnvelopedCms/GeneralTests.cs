// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography.Pkcs.Tests;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using Xunit;

using Test.Cryptography;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public static partial class GeneralTests
    {
        public static bool SupportsDiffieHellman { get; } = KeyAgreeRecipientInfoTests.SupportsDiffieHellman;
        public static bool SupportsRsaOaepCerts => PlatformDetection.IsWindows;

        [Fact]
        public static void DefaultEncryptionAlgorithm()
        {
            EnvelopedCms cms1 = new EnvelopedCms();
            EnvelopedCms cms2 = new EnvelopedCms(new ContentInfo(Array.Empty<byte>()));

            string[] supportedAlgorithms;

            // net48 changes the default to AES-256-CBC, older versions (and quirk) are
            // DES3-EDE-CBC
            if (PlatformDetection.IsNetFramework)
            {
                supportedAlgorithms = new[] { Oids.TripleDesCbc, Oids.Aes256 };
            }
            else
            {
                supportedAlgorithms = new[] { Oids.Aes256 };
            }

            Assert.Contains(cms1.ContentEncryptionAlgorithm.Oid.Value, supportedAlgorithms);
            Assert.Contains(cms2.ContentEncryptionAlgorithm.Oid.Value, supportedAlgorithms);
        }

        [Fact]
        public static void DecodeVersion0_RoundTrip()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();

            VerifyVersion0(encodedMessage);
        }

        [Fact]
        public static void DecodeVersion0_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818013"
                + "dc0eb2984a445d04a1f6246b8fe41f1d24507548d449d454d5bb5e0638d75ed101bf78c0155a5d208eb746755fbccbc86923"
                + "8443760a9ae94770d6373e0197be23a6a891f0c522ca96b3e8008bf23547474b7e24e7f32e8134df3862d84f4dea2470548e"
                + "c774dd74f149a56cdd966e141122900d00ad9d10ea1848541294a1302b06092a864886f70d010701301406082a864886f70d"
                + "030704089c8119f6cf6b174c8008bcea3a10d0737eb9").HexToByteArray();

            VerifyVersion0(encodedMessage);
        }

        private static void VerifyVersion0(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            int version = ecms.Version;
            Assert.Equal(0, version);
        }

        [ConditionalFact(nameof(SupportsDiffieHellman))]
        public static void DecodeRecipients3_RoundTrip()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, KeyAgreeRecipientInfoTests.TripleDesAlgId);
            CmsRecipientCollection recipients = new CmsRecipientCollection();
            foreach (X509Certificate2 cert in s_certs)
            {
                recipients.Add(new CmsRecipient(cert));
            }
            ecms.Encrypt(recipients);
            byte[] encodedMessage = ecms.Encode();

            VerifyRecipients3(encodedMessage);
        }

        [Fact]
        public static void DecodeRecipients3_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082032b06092a864886f70d010703a082031c30820318020102318202e43081c5020100302e301a31183016060355040313"
                + "0f5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004"
                + "81805d4b13a055c512c2367db4ac4ad4470415ef1113ee78f6b22d114873759ddc1135027f59a8583d24527ceee38b34be52"
                + "22400e37a265d5b4be67df685a21db2a1512d46d857c9c9ac8d801807131118efe68b8f89bfb81c06171cf12756e679bd518"
                + "3501193a86bb3b3893a34d6907698e2391701a0ddcd8fe337734db83a54c3081c5020100302e301a31183016060355040313"
                + "0f5253414b65795472616e736665723202102bce9f9ece39f98044f0cd2faa9a14e7300d06092a864886f70d010101050004"
                + "818030e318a393ec982869cccec4d7ea24106f996892abf35c4faa0f88b0d2f5d97371f761ef2e60dfe46b9c63bc004a433a"
                + "16504adeda4edb3f37d3da5e602d97d8a049bca07c0e092c1f43682d8b8ba0c8f495ba0265231f68624e74d263efee580629"
                + "b97e4faf8d66c053f9bf214ed76a2e81b03b0771baacd0b07ee775d06244a1820150020103a08195a18192300906072a8648"
                + "ce3e02010381840002818054287a01a44b38468f629e28d11b9f80de6cb0ab3dc0828cf41ff077d256beafaf06ae111e5235"
                + "d90c57a37a22fb10fd22d11fb47f0c278d5b23c5914475452dcac8cfb6bff3f326450ab7a666c183f89f96d966336464cc2f"
                + "39f61263996c3e56b9e782b8264a4e8cd57e5576174dca5d02bc7f33f7fdfe71af1ff1f11b287b301e060b2a864886f70d01"
                + "09100305300f060b2a864886f70d0109100306050030819230818f3063304f314d304b06035504031e44004d0061006e0061"
                + "00670065006400200050004b00430053002300370020005400650073007400200052006f006f007400200041007500740068"
                + "006f007200690074007902100ae59b0cb8119f8942eda74163413a0204285aadd33713104d128c5e1d70d9281f7c0df6fa42"
                + "64fd9fa77fcde800aaf8ea33d533b8572a1b9c4a302b06092a864886f70d010701301406082a864886f70d03070408fc6d30"
                + "2f218ea61f8008de1137262232ceae").HexToByteArray();

            VerifyRecipients3(encodedMessage);
        }

        private static void VerifyRecipients3(byte[] encodedMessage)
        {
            string[] expectedIssuers = s_certs.Select(c => c.Issuer).OrderBy(s => s).ToArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            RecipientInfoCollection col = ecms.RecipientInfos;
            int numRecipients = col.Count;
            Assert.Equal(3, numRecipients);

            RecipientInfo[] recipients = new RecipientInfo[numRecipients];
            col.CopyTo(recipients, 0);

            string[] actualIssuers = recipients.Select(r => r.RecipientIdentifier.Value).Cast<X509IssuerSerial>().Select(xis => xis.IssuerName).OrderBy(s => s).ToArray();
            Assert.Equal<string>(expectedIssuers, actualIssuers);
        }

        [Fact]
        public static void DecodeAllIndefinite()
        {
            byte[] encrypted = Convert.FromBase64String(
                @"
MIAGCSqGSIb3DQEHA6CAMIACAQAxggFXMIIBUwIBADA7MDMxGTAXBgNVBAoMEERh
dGEgSW50ZXJjaGFuZ2UxFjAUBgNVBAMMDVVubyBUZXN0IFJvb3QCBFqG6RQwDQYJ
KoZIhvcNAQEBBQAEggEAUPilAHUe67HG5vDCO/JBmof44G/XnDLtiDrbxD4QekGq
mdPqazZiLDKEewlBy2uFJr/JijeYx6qNKTXs/EShw/lYnKisaK5ue6JZ7ssMunM9
HpkiDfM+iyN7PxnC1riZ/Kg2JExY8pf5R1Zuvu29JSLhM9ajWk9C1pBzQRJ4vkY2
OvFKR2th0Vgw7mTmc2X6HUK4tosB3LGKDVNd6BVoMQMvfkseCqeZOe1KIiBFmhyk
E+B2UZcD6Z6kLnCk4LNGyoyxW6Thv5s/lwP9p7trVVbPXbuep1l8uMCGj6vjTD66
AamEIRmTFvEVHzyO2MGG9V0bM+8UpqPAVFNCXOm6mjCABgkqhkiG9w0BBwEwFAYI
KoZIhvcNAwcECJ01qtX2EKx6oIAEEM7op+R2U3GQbYwlEj5X+h0AAAAAAAAAAAAA
");
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(encrypted);

            RecipientInfoCollection recipientInfos = cms.RecipientInfos;

            Assert.Equal(1, recipientInfos.Count);
            Assert.Equal(
                SubjectIdentifierType.IssuerAndSerialNumber,
                recipientInfos[0].RecipientIdentifier.Type);

            string expectedContentHex = "CEE8A7E4765371906D8C25123E57FA1D";

            if (PlatformDetection.IsNetFramework)
            {
                // .NET Framework over-counts encrypted content.
                expectedContentHex += "000000000000";
            }

            // Still encrypted.
            Assert.Equal(
                expectedContentHex,
                cms.ContentInfo.Content.ByteArrayToHex());
        }

        [Fact]
        public static void TestGetContentTypeEnveloped()
        {
            byte[] encodedMessage =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818013"
                + "dc0eb2984a445d04a1f6246b8fe41f1d24507548d449d454d5bb5e0638d75ed101bf78c0155a5d208eb746755fbccbc86923"
                + "8443760a9ae94770d6373e0197be23a6a891f0c522ca96b3e8008bf23547474b7e24e7f32e8134df3862d84f4dea2470548e"
                + "c774dd74f149a56cdd966e141122900d00ad9d10ea1848541294a1302b06092a864886f70d010701301406082a864886f70d"
                + "030704089c8119f6cf6b174c8008bcea3a10d0737eb9").HexToByteArray();

            Oid contentType = ContentInfo.GetContentType(encodedMessage);
            Assert.Equal(Oids.Pkcs7Enveloped, contentType.Value);
        }

        [Fact]
        public static void TestContentTypeSigned()
        {
            byte[] encodedMessage =
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

            Oid contentType = ContentInfo.GetContentType(encodedMessage);
            Assert.Equal(Oids.Pkcs7Signed, contentType.Value);
        }

        [Theory]
        [InlineData(false)]
#if !NETFRAMEWORK
        [InlineData(true)]
#endif
        public static void TestContent(bool fromSpan)
        {
            // Tests that the content is what it is expected to be, even if it's still encrypted. This prevents from ambiguous definitions of content.

            // The encoded message was built in ASN.1 editor and tested in framework. It contains an enveloped message version 0 with one recipient of
            // key transport type. The symmetric algorithm is 3DES and the contained type is data.
            byte[] encodedMessage =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818013"
                + "dc0eb2984a445d04a1f6246b8fe41f1d24507548d449d454d5bb5e0638d75ed101bf78c0155a5d208eb746755fbccbc86923"
                + "8443760a9ae94770d6373e0197be23a6a891f0c522ca96b3e8008bf23547474b7e24e7f32e8134df3862d84f4dea2470548e"
                + "c774dd74f149a56cdd966e141122900d00ad9d10ea1848541294a1302b06092a864886f70d010701301406082a864886f70d"
                + "030704089c8119f6cf6b174c8008bcea3a10d0737eb9").HexToByteArray();

            EnvelopedCms cms = new EnvelopedCms();

            if (fromSpan)
            {
#if !NETFRAMEWORK
                cms.Decode(encodedMessage.AsSpan());
#else
                throw new Xunit.Sdk.XunitException(
                    "This test should not evaluate for .NET Framework, the API is missing.");
#endif
            }
            else
            {
                cms.Decode(encodedMessage);
            }

            string expectedHex = "BCEA3A10D0737EB9";

            if (PlatformDetection.IsNetFramework)
            {
                expectedHex = "BCEA3A10D0737EB9000000000000";
            }

            Assert.Equal(expectedHex, cms.ContentInfo.Content.ByteArrayToHex());
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void MultipleRecipientIdentifiers_RoundTrip()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            CmsRecipientCollection recipients = new CmsRecipientCollection();
            using (X509Certificate2 issuerSerialCert = Certificates.RSAKeyTransfer1.GetCertificate())
            using (X509Certificate2 explicitSkiCert = Certificates.RSAKeyTransfer_ExplicitSki.GetCertificate())
            {
                // CmsRecipients have different identifiers to test multiple identifier encryption.
                recipients.Add(new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, issuerSerialCert));
                recipients.Add(new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, explicitSkiCert));
                ecms.Encrypt(recipients);
            }

            byte[] encodedMessage = ecms.Encode();

            ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            // Try decoding it, doesn't really matter with which cert you want to do it as it's not what this
            // test aims for.

            using (X509Certificate2 privateCert = Certificates.RSAKeyTransfer_ExplicitSki.TryGetCertificateWithPrivateKey())
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
        public static void RoundTrip_ExplicitSki()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 explicitSkiCert = Certificates.RSAKeyTransfer_ExplicitSki.GetCertificate())
            {
                CmsRecipient recipient = new CmsRecipient(SubjectIdentifierType.SubjectKeyIdentifier, explicitSkiCert);
                ecms.Encrypt(recipient);
            }

            byte[] encodedMessage = ecms.Encode();

            ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 privateCert = Certificates.RSAKeyTransfer_ExplicitSki.TryGetCertificateWithPrivateKey())
            {
                if (privateCert == null)
                    return; // CertLoader can't load the private certificate.

                ecms.Decrypt(new X509Certificate2Collection(privateCert));
            }
            Assert.Equal(contentInfo.ContentType.Value, ecms.ContentInfo.ContentType.Value);
            Assert.Equal<byte>(contentInfo.Content, ecms.ContentInfo.Content);
        }

        [ConditionalFact(nameof(SupportsRsaOaepCerts))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void RsaOaepCertificate_NullParameters_Throws()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RsaOaep2048_NullParameters.GetCertificate())
            {
                CmsRecipient recipient = new CmsRecipient(cert);
                Assert.ThrowsAny<CryptographicException>(() => ecms.Encrypt(recipient));
            }
        }

        [ConditionalFact(nameof(SupportsRsaOaepCerts))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void RoundTrip_RsaOaepCertificate_Sha1KeyParameters()
        {
            Assert_Certificate_Roundtrip(Certificates.RsaOaep2048_Sha1Parameters);
        }

        [ConditionalFact(nameof(SupportsRsaOaepCerts))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void RoundTrip_RsaOaepCertificate_Sha256KeyParameters()
        {
            Assert_Certificate_Roundtrip(Certificates.RsaOaep2048_Sha256Parameters);
        }

        [ConditionalFact(nameof(SupportsRsaOaepCerts))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void RoundTrip_RsaOaepCertificate_NoParameters()
        {
            Assert_Certificate_Roundtrip(Certificates.RsaOaep2048_NoParameters);
        }

        [Fact]
        public static void Encrypt_Data_DoesNotIncreaseInSize()
        {
            byte[] content = new byte[15]; // One short of AES block size boundary
            ContentInfo contentInfo = new ContentInfo(content);
            AlgorithmIdentifier identifier = new AlgorithmIdentifier(new Oid(Oids.Aes128));
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, identifier);

            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient recipient = new CmsRecipient(cert);
                ecms.Encrypt(recipient);
            }

            byte[] encoded = ecms.Encode();
            EnvelopedCms reDecoded = new EnvelopedCms();
            reDecoded.Decode(encoded);
            int expectedSize = PlatformDetection.IsNetFramework ? 22 : 16; //NetFx compat.
            Assert.Equal(expectedSize, reDecoded.ContentInfo.Content.Length);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void Decrypt_DoesNotAlterAsnOctetStringContent()
        {
            // The content in the message happens to be an ASN.1 OCTET STRING.
            // We used to decode this for compatibility purposes, but that has
            // been removed. Instead, test that the content remains untouched.
            byte[] expectedContent = new byte[] { 4, 3, 1, 2, 3 };
            byte[] encodedMessage =
                ("3082010C06092A864886F70D010703A081FE3081FB0201003181C83081C5020100302" +
                 "E301A311830160603550403130F5253414B65795472616E7366657231021031D935FB" +
                 "63E8CFAB48A0BF7B397B67C0300D06092A864886F70D0101010500048180586BCA530" +
                 "9A74A211859714715D90B8E13A7712838746877DF7D68B0BCF36DE3F77854276C8EAD" +
                 "389ADD8402697E4FFF215143E0E63676349592CB3A86FF556230D5F4AC4A9A6758219" +
                 "9E65281A8B63DFBCFB7180E6B54C6E38BECAF09624C6B6D2B3058F280FE8F0BF8EBA3" +
                 "57AECC1B9B177E98671A9659B034501AE3D58789302B06092A864886F70D010701301" +
                 "406082A864886F70D0307040810B222648FDC0DE38008036BB59C8B6A784B").HexToByteArray();
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 privateCert = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            {
                if (privateCert == null)
                {
                    return; //Private key not available.
                }

                ecms.Decrypt(new X509Certificate2Collection(privateCert));
            }

            Assert.Equal(expectedContent, ecms.ContentInfo.Content);
        }

        private static void Assert_Certificate_Roundtrip(CertLoader certificateLoader)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = certificateLoader.GetCertificate())
            {
                CmsRecipient recipient = new CmsRecipient(cert);
                ecms.Encrypt(recipient);
            }

            byte[] encodedMessage = ecms.Encode();

            ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 privateCert = certificateLoader.TryGetCertificateWithPrivateKey())
            {
                if (privateCert == null)
                    return; // CertLoader can't load the private certificate.

                ecms.Decrypt(new X509Certificate2Collection(privateCert));
            }
            Assert.Equal(contentInfo.ContentType.Value, ecms.ContentInfo.ContentType.Value);
            Assert.Equal<byte>(contentInfo.Content, ecms.ContentInfo.Content);
        }

        private static X509Certificate2[] s_certs =
        {
            Certificates.RSAKeyTransfer1.GetCertificate(),
            Certificates.RSAKeyTransfer2.GetCertificate(),
            Certificates.DHKeyAgree1.GetCertificate(),
        };
    }
}
