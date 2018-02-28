// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop("Leaks key on disk if interrupted")]
        public static void Decrypt_512_FixedValue()
        {
            byte[] content = { 5, 77, 32, 33, 2, 34 };
            byte[] message = (
                "3082012506092A864886F70D010703A0820116308201120201003181CE3081CB" +
                "02010030343020311E301C060355040313155253415368613531324B65795472" +
                "616E736665723102102F5D9D58A5F41B844650AA233E68F105300D06092A8648" +
                "86F70D01010105000481803163AA33F8F5E033DC03AE98CCEE158199589FC420" +
                "19200DCC1D202309CCCAF79CC0278B9502B5709F1311E522DA325338136D3F1E" +
                "A271FAEA978CC656A3CB94B1C6A8D7AFC836C3193DB693E8B8767472C2C23125" +
                "BA11E7D0623E4C8B848826BBF99EB411CB88B4731740D1AD834F0E4076BAD0D4" +
                "BA695CFE8CDB2DE3E77196303C06092A864886F70D010701301D060960864801" +
                "650304012A0410280AC7A629BFC9FD6FB24F8A42F094B48010B78CDFECFF32A8" +
                "E86D448989382A93E7"
            ).HexToByteArray();

            VerifySimpleDecrypt(message, Certificates.RSASha512KeyTransfer1, new ContentInfo(content));
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop("Leaks key on disk if interrupted")]
        public static void Decrypt_512_NoData_FixedValue()
        {
            // This is the Decrypt_512_FixedData test re-encoded to remove the
            // encryptedContentInfo.encryptedContent optional value.
            byte[] content = Array.Empty<byte>();
            byte[] message = (
                "3082011306092A864886F70D010703A0820104308201000201003181CE3081CB" +
                "02010030343020311E301C060355040313155253415368613531324B65795472" +
                "616E736665723102102F5D9D58A5F41B844650AA233E68F105300D06092A8648" +
                "86F70D01010105000481803163AA33F8F5E033DC03AE98CCEE158199589FC420" +
                "19200DCC1D202309CCCAF79CC0278B9502B5709F1311E522DA325338136D3F1E" +
                "A271FAEA978CC656A3CB94B1C6A8D7AFC836C3193DB693E8B8767472C2C23125" +
                "BA11E7D0623E4C8B848826BBF99EB411CB88B4731740D1AD834F0E4076BAD0D4" +
                "BA695CFE8CDB2DE3E77196302A06092A864886F70D010701301D060960864801" +
                "650304012A0410280AC7A629BFC9FD6FB24F8A42F094B4"
            ).HexToByteArray();

            if (PlatformDetection.IsFullFramework)
            {
                // On NetFx when Array.Empty should be returned an array of 6 zeros is
                // returned instead.
                content = new byte[6];
            }

            VerifySimpleDecrypt(message, Certificates.RSASha512KeyTransfer1, new ContentInfo(content));
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop("Leaks key on disk if interrupted")]
        public static void Decrypt_512_CekDoesNotDecrypt_FixedValue()
        {
            // This is the Decrypt_512_NoData_FixedValue test except that the last
            // byte of the recipient encrypted key has been changed from 0x96 to 0x95
            // (the sequence 7195 identifies the changed byte)
            byte[] content = Array.Empty<byte>();
            byte[] message = (
                "3082011306092A864886F70D010703A0820104308201000201003181CE3081CB" +
                "02010030343020311E301C060355040313155253415368613531324B65795472" +
                "616E736665723102102F5D9D58A5F41B844650AA233E68F105300D06092A8648" +
                "86F70D01010105000481803163AA33F8F5E033DC03AE98CCEE158199589FC420" +
                "19200DCC1D202309CCCAF79CC0278B9502B5709F1311E522DA325338136D3F1E" +
                "A271FAEA978CC656A3CB94B1C6A8D7AFC836C3193DB693E8B8767472C2C23125" +
                "BA11E7D0623E4C8B848826BBF99EB411CB88B4731740D1AD834F0E4076BAD0D4" +
                "BA695CFE8CDB2DE3E77195302A06092A864886F70D010701301D060960864801" +
                "650304012A0410280AC7A629BFC9FD6FB24F8A42F094B4"
            ).HexToByteArray();

            Assert.ThrowsAny<CryptographicException>(
                () => VerifySimpleDecrypt(message, Certificates.RSASha512KeyTransfer1, new ContentInfo(content)));
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

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleAes128_IssuerAndSerial()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSAKeyTransfer1.GetCertificate()
            // and of type IssuerAndSerialNumber. The symmetric algorithm is Aes128
            byte[] encryptedMessage =
                ("3082011F06092A864886F70D010703A08201103082010C0201003181C83081C5020100302E301A311830160"
                + "603550403130F5253414B65795472616E7366657231021031D935FB63E8CFAB48A0BF7B397B67C0300D0609"
                + "2A864886F70D0101073000048180862175CD3B2932235A67C6A025F75CDA1A43B53E785370895BA9AC8D0DD"
                + "318EB36DFAE275B16ABD497FEBBFCF2D4B3F38C75B91DC40941A2CC1F7F47E701EEA2D5A770C485565F8726"
                + "DC0D59DDE17AA6DB0F9384C919FC8BC6CB561A980A9AE6095486FDF9F52249FB466B3676E4AEFE4035C15DC"
                + "EE769F25E4660D4BE664E7F303C06092A864886F70D010701301D060960864801650304010204100A068EE9"
                + "03E085EA5A03D1D8B4B73DD88010740E5DE9B798AA062B449F104D0F5D35").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSAKeyTransfer1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleAes192_IssuerAndSerial()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSAKeyTransfer1.GetCertificate()
            // and of type IssuerAndSerialNumber. The symmetric algorithm used is Aes192
            byte[] encryptedMessage =
                ("3082011F06092A864886F70D010703A08201103082010C0201003181C83081C5020100302E301A311830160"
                + "603550403130F5253414B65795472616E7366657231021031D935FB63E8CFAB48A0BF7B397B67C0300D0609"
                + "2A864886F70D010107300004818029B82454B4C301F277D7872A14695A41ED24FD37AC4C9942F9EE96774E0"
                + "C6ACC18E756993A38AB215E5702CD34F244E52402DA432E8B79DF748405135E8A6D8CB78D88D9E4C142565C"
                + "06F9FAFB32F5A9A4074E10FCCB0758A708CA758C12A17A4961969FCB3B2A6E6C9EB49F5E688D107E1B1DF3D"
                + "531BC684B944FCE6BD4550C303C06092A864886F70D010701301D06096086480165030401160410FD7CBBF5"
                + "6101854387E584C1B6EF3B08801034BD11C68228CB683E0A43AB5D27A8A4").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSAKeyTransfer1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleAes256_IssuerAndSerial()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSAKeyTransfer1.GetCertificate()
            // and of type IssuerAndSerialNumber. The symmetric algorithm used is Aes256
            byte[] encryptedMessage =
                ("3082011F06092A864886F70D010703A08201103082010C0201003181C83081C5020100302E301A311830160"
                + "603550403130F5253414B65795472616E7366657231021031D935FB63E8CFAB48A0BF7B397B67C0300D0609"
                + "2A864886F70D01010730000481800215BF7505BCD5D083F8EFDA01A4F91D61DE3967779B2F5E4360593D4CB"
                + "96474E36198531A5E20E417B04C5C7E3263C3301DF8FA888FFBECC796500D382858379059C986285AFD605C"
                + "B5DE125487CCA658DF261C836720E2E14440DA60E2F12D6D5E3992A0DB59973929DF6FC23D8E891F97CA956"
                + "2A7AD160B502FA3C10477AA303C06092A864886F70D010701301D060960864801650304012A04101287FE80"
                + "93F3C517AE86AFB95E599D7E80101823D88F47191857BE0743C4C730E39E").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSAKeyTransfer1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleTripleDes_IssuerAndSerial()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSAKeyTransfer1.GetCertificate()
            // and of type IssuerAndSerialNumber. The symmetric algorithm used is 3DES-CBC
            byte[] encryptedMessage =
                ("3082010C06092A864886F70D010703A081FE3081FB0201003181C83081C5020100302E301A3118301606035"
                + "50403130F5253414B65795472616E7366657231021031D935FB63E8CFAB48A0BF7B397B67C0300D06092A86"
                + "4886F70D0101010500048180062F6F16637C8F35B73924AD85BA47D99DBB4800CB8F0C4094F6896050B7C1F"
                + "11CE79BEE55A638EAAE70F2C32C01FC24B8D09D9D574CB7373788C8BC3A4748124154338C74B644A2A11750"
                + "9E97D1B3535FAE70E4E7C8F2F866232CBFC6448E89CF9D72B948EDCF9C9FC9C153BCC7104680282A4BBBC1E"
                + "E367F094F627EE45FCD302B06092A864886F70D010701301406082A864886F70D030704081E3F12D42E4041"
                + "58800877A4A100165DD0F2").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSAKeyTransfer1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleAes256_Ski()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSAKeyTransfer1.GetCertificate()
            // and of type SubjectKeyIdentifier. The symmetric algorithm used is Aes256
            byte[] encryptedMessage =
                ("3082010306092A864886F70D010703A081F53081F20201023181AE3081AB0201028014F2008AA9FA3742E83"
                + "70CB1674CE1D1582921DCC3300D06092A864886F70D010101050004818055F258073615B95426A7021E1B30"
                + "9CFE8DD135B58D29F174B9FE19AE80CFC84621BCE3DBD63A5422AF30A6FAA3E2DFC05CB1AB5AB4FBA6C84EB"
                + "1C2E17D5BE5C4959DBE8F96BF1A9701F55B697843032EEC7AFEC58A36815168F017DCFD70C74AD05C48B5E4"
                + "D9DDEE409FDC9DC3326B6C5BA9F433A9E031FF9B09473176637F50303C06092A864886F70D010701301D060"
                + "960864801650304012A0410314DA87435ED110DFE4F52FA70CEF7B080104DDA6C617338DEBDD10913A9141B"
                + "EE52").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSAKeyTransfer1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleAes256_RsaTransferCapi()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSAKeyTransferCapi1.GetCertificate()
            // and of type IssuerAndSerialNumber. The symmetric algorithm used is Aes256
            byte[] encryptedMessage =
                ("3082012306092A864886F70D010703A0820114308201100201003181CC3081C90201003032301E311C301A0"
                + "60355040313135253414B65795472616E73666572436170693102105D2FFFF863BABC9B4D3C80AB178A4CCA"
                + "300D06092A864886F70D01010730000481804F3F4A6707B329AB9A7343C62F20D5C1EAF4E74ECBB2DC66D1C"
                + "642FC4AA3E40FC4C13547C6C9F73D525EE2FE4147B2043B8FEBF8604C0E4091C657B48DFD83A322F0879580"
                + "FA002C9B27AD1FCF9B8AF24EDDA927BB6728D11530B3F96EBFC859ED6B9F7B009F992171FACB587A7D05E8B"
                + "467B3A1DACC08B2F3341413A7E96576303C06092A864886F70D010701301D060960864801650304012A0410"
                + "6F911E14D9D991DAB93C0B7738D1EC208010044264D201501735F73052FFCA4B2A95").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSAKeyTransferCapi1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleAes256_RsaSha256()
        {
            // Message encrypted on framework for a recipient using the certificate returned by 
            // Certificates.RSASha256KeyTransfer1.GetCertificate() and of type IssuerAndSerialNumber. The symmetric algorithm used is Aes256
            byte[] encryptedMessage =
                ("3082012506092A864886F70D010703A0820116308201120201003181CE3081CB02010030343020311E301C0"
                + "60355040313155253415368613235364B65795472616E7366657231021072C6C7734916468C4D608253DA01"
                + "7676300D06092A864886F70D01010730000481805C32FA32EBDCFFC3595166EEDACFC9E9D60842105B581E1"
                + "8B85DE1409F4C999995637153480438530955EE4481A3B27B866FF4E106A525CDFFC6941BDD01EFECCC6CCC"
                + "82A3D7F743F7543AB20A61A7831FE4DFB24A1652B072B3758FE4B2588D3B94A29575B6422DC5EF52E432565"
                + "36CA25A11BB92817D61FEAFBDDDEC6EE331303C06092A864886F70D010701301D060960864801650304012A"
                + "041021D59FDB89C13A3EC3766EF32FB333D080105AE8DEB71DF50DD85F66FEA63C8113F4").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSASha256KeyTransfer1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleAes256_RsaSha384()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSASha384KeyTransfer1.GetCertificate()
            // and of type IssuerAndSerialNumber. The symmetric algorithm used is Aes256
            byte[] encryptedMessage =
                ("3082012506092A864886F70D010703A0820116308201120201003181CE3081CB02010030343020311E301C0"
                + "60355040313155253415368613338344B65795472616E736665723102103C724FB7A0159A9345CAAC9E3DF5"
                + "F136300D06092A864886F70D010107300004818011C1B85914331C005EA89E30D00364821B29BC0C459A22D"
                + "917494A1092CDBDA2022792E46C5E88BAD0EE3FD4927B856722311F9B17934FB29CAB8FE595C2AB2B20096B"
                + "9E2FC6F9D7B92125F571CBFC945C892EE4764D9B63369350FD2DAEFE455B367F48E100CB461F112808E792A"
                + "8AA49B66C79E511508A877530BBAA896696303C06092A864886F70D010701301D060960864801650304012A"
                + "0410D653E25E06BFF2EEB0BED4A90D00FE2680106B7EF143912ABA5C24F5E2C151E59D7D").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSASha384KeyTransfer1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimpleAes256_RsaSha512()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSASha512KeyTransfer1.GetCertificate()
            // and of type IssuerAndSerialNumber. The symmetric algorithm used is Aes256
            byte[] encryptedMessage =
                ("3082012506092A864886F70D010703A0820116308201120201003181CE3081CB02010030343020311E301C0"
                + "60355040313155253415368613531324B65795472616E736665723102102F5D9D58A5F41B844650AA233E68"
                + "F105300D06092A864886F70D01010730000481802156D42FF5ED2F0338302E7298EF79BA1D04E20E68B079D"
                + "B3239120E1FC03FEDA8B544F59142AACAFBC5E58205E8A0D124AAD17B5DCAA39BFC6BA634E820DE623BFDB6"
                + "582BC48AF1B3DEF6849A57D2033586AF01079D67C9AB3AA9F6B51754BCC479A19581D4045EBE23145370219"
                + "98ECB6F5E1BCF8D6BED6A75FE957A40077D303C06092A864886F70D010701301D060960864801650304012A"
                + "04100B696608E489E7C35914D0A3DB9EB27F80103D362181B54721FB2CB7CE461CB31030").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSASha512KeyTransfer1;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void TestDecryptSimple_ExplicitSki()
        {
            // Message encrypted on framework for a recipient using the certificate returned by Certificates.RSAKeyTransfer_ExplicitSki.GetCertificate()
            // and of type SubjectKeyIdentifier. The symmetric algorithm used is Aes256
            byte[] encryptedMessage =
                ("3082018806092A864886F70D010703A082017930820175020102318201303082012C020102801401952851C"
                + "55DB594B0C6167F5863C5B6B67AEFE6300D06092A864886F70D010101050004820100269EAF029262C87125"
                + "314DD3FB02302FA212EB3CC06F73DF1474382BBA2A92845F39FF5A7F5020482849C36B4BC6BC82F7AF0E2E3"
                + "9143548CC32B93B72EF0659C6895F77E6B5839962678532392185C9431658B34D1ABD31F64F4C4A9B348A77"
                + "56783D60244519ADDD33560405E9377A91617127C2EECF2BAE53AB930FC13AFD25723FB60DB763286EDF6F1"
                + "187D8124B6A569AA2BD19294A7D551A0D90F8436274690231520A2254C19EA9BF877FC99566059A29CDF503"
                + "6BEA1D517916BA2F20AC9F1D8F164B6E8ACDD52BA8B2650EBBCC2ED9103561E11AF422D10DF7405404195FA"
                + "EF79A1FDC680F3A3DC395E3E9C0B10394DF35AE134E6CB719E35152F8E5303C06092A864886F70D01070130"
                + "1D060960864801650304012A041085072D8771A2A2BB403E3236A7C60C2A80105C71A04E73C57FE75C1DEDD"
                + "94B57FD01").HexToByteArray();

            byte[] expectedContent = { 1, 2, 3, 4 };
            ContentInfo expectedContentInfo = new ContentInfo(expectedContent);
            CertLoader certLoader = Certificates.RSAKeyTransfer_ExplicitSki;

            VerifySimpleDecrypt(encryptedMessage, certLoader, expectedContentInfo);
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


