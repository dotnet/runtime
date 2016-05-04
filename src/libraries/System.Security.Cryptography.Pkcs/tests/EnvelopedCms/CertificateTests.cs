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
    public static partial class CertificateTests
    {
        [Fact]
        public static void DecodeCertificates0_RoundTrip()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();

            VerifyCertificates0(encodedMessage);
        }

        [Fact]
        public static void DecodeCertificates0_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818013"
                + "dc0eb2984a445d04a1f6246b8fe41f1d24507548d449d454d5bb5e0638d75ed101bf78c0155a5d208eb746755fbccbc86923"
                + "8443760a9ae94770d6373e0197be23a6a891f0c522ca96b3e8008bf23547474b7e24e7f32e8134df3862d84f4dea2470548e"
                + "c774dd74f149a56cdd966e141122900d00ad9d10ea1848541294a1302b06092a864886f70d010701301406082a864886f70d"
                + "030704089c8119f6cf6b174c8008bcea3a10d0737eb9").HexToByteArray();

            VerifyCertificates0(encodedMessage);
        }

        private static void VerifyCertificates0(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            X509Certificate2Collection certs = ecms.Certificates;
            Assert.Equal(0, certs.Count);
        }

        [Fact]
        public static void DecodeCertificates3_RoundTrip()
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);

            foreach (X509Certificate2 cert in s_certs)
            {
                ecms.Certificates.Add(cert);
            }

            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();

            VerifyCertificates3(encodedMessage);
        }

        [Fact]
        public static void DecodeCertificates3_FixedValue()
        {
            byte[] encodedMessage =
                 ("308208cb06092a864886f70d010703a08208bc308208b8020102a08207b9a08207b5308201c830820131a00302010202102b"
                + "ce9f9ece39f98044f0cd2faa9a14e7300d06092a864886f70d0101050500301a311830160603550403130f5253414b657954"
                + "72616e7366657232301e170d3136303332353231323334325a170d3137303332363033323334325a301a3118301606035504"
                + "03130f5253414b65795472616e736665723230819f300d06092a864886f70d010101050003818d0030818902818100ea5a38"
                + "34bfb863ae481b696ea7010ba4492557a160a102b3b4d11c120a7128f20b656ebbd24b426f1a6d40be0a55ca1b53ebdca202"
                + "d258eebb20d5c662819182e64539360461dd3b5dda4085f10250fc5249cf023976b8db2bc5f5e628fdb0f26e1b11e83202cb"
                + "cfc9750efd6bb4511e6211372b60a97adb984779fdae21ce070203010001a30f300d300b0603551d0f040403020520300d06"
                + "092a864886f70d0101050500038181004dc6f9fd6054ae0361d28d2d781be590fa8f5685fedfc947e315db12a4c47e220601"
                + "e8c810e84a39b05b7a89f87425a06c0202ad48b3f2713109f5815e6b5d61732dac4541da152963e700a6f37faf7678f084a9"
                + "fb4fe88f7b2cbc6cdeb0b9fdcc6a8a16843e7bc281a71dc6eb8bbc4092d299bf7599a3492c99c9a3acf41b29308201c83082"
                + "0131a003020102021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d0101050500301a3118301606035504"
                + "03130f5253414b65795472616e7366657231301e170d3136303431323136323534375a170d3137303431323232323534375a"
                + "301a311830160603550403130f5253414b65795472616e736665723130819f300d06092a864886f70d010101050003818d00"
                + "308189028181009eaab63f5629db5ac0bd74300b43ba61f49189ccc30c001fa96bd3b139f45732cd3c37e422ccbb2c598a4c"
                + "6b3977a516a36ff850a5e914331f7445e86973f5a6cbb590105e933306e240eab6db72d08430cd7316e99481a272adef0f24"
                + "79d0b7c58e89e072364d660fdad1b51a603ff4549a82e8dc914df82bcc6c6c232985450203010001a30f300d300b0603551d"
                + "0f040403020520300d06092a864886f70d01010505000381810048c83e6f45d73a111c67e8f9f9c2d646292b75cec52ef0f9"
                + "ae3e1639504aa1759512c46527fcf5476897d3fb6fc515ff1646f8f8bc09f84ea6e2ad04242d3fb9b190b81686b73d334e8b"
                + "3afa7fb8eb31483efc0c7ccb0f8c1ca94d8be4f0daade4498501d02e6f92dd7b2f4401550896eb511ef14417cbb5a1b360d6"
                + "7998d3343082041930820305a00302010202100ae59b0cb8119f8942eda74163413a02300906052b0e03021d0500304f314d"
                + "304b06035504031e44004d0061006e006100670065006400200050004b004300530023003700200054006500730074002000"
                + "52006f006f007400200041007500740068006f0072006900740079301e170d3136303431333132313630315a170d33393132"
                + "33313233353935395a301f311d301b06035504031314446648656c6c654b657941677265656d656e7431308201b63082012b"
                + "06072a8648ce3e02013082011e02818100b2f221e2b4649401f817557771e4f2ca1c1309caab3fa4d85b03dc1ea13c839566"
                + "5eb4d05a212b33e1d727403fec46d30ef3c3fd58cd5b621d7d30912f2360676f16b206aa419dba39b95267b42f14f6500b17"
                + "29de2d94ef182ed0f3042fd3850a7398808c48f3501fca0e929cec7a9594e98bccb093c21ca9b7dbdfcdd733110281805e0b"
                + "ed02dd17342f9f96d186d2cc9e6ff57f5345b44bfeeb0da936b37bca62e2e508d9635a216616abe777c3fa64021728e7aa42"
                + "cfdae52101c6a390c3eb618226d8060ceacdbc59fa43330ad41e34a604b1c740959b534f00bd6cf0f35b62d1f8de68d8f373"
                + "89cd435d764b4abec5fc39a1e936cdf52a8b73e0f4f37dda536902150093ced62909a4ac3aeca9982f68d1eed34bf055b303"
                + "8184000281804f7e72a0e0ed4aae8e498131b0f23425537b9a28b15810a3c1ff6f1439647f4e55dcf73e72a7573ce609a5fb"
                + "5c5dc3dcdaa883b334780c232ea12b3af2f88226775db48f4b800c9ab1b54e7a26c4c0697bbd5e09355e3b4ac8005a89c650"
                + "27e1d0d7091b6aec8ede5dc72e9bb0d3597915d50da58221673ad8a74e76b2a79f25a38194308191300c0603551d130101ff"
                + "040230003081800603551d010479307780109713ac709a6e2cc6aa54b098e5557cd8a151304f314d304b06035504031e4400"
                + "4d0061006e006100670065006400200050004b00430053002300370020005400650073007400200052006f006f0074002000"
                + "41007500740068006f00720069007400798210d581eafe596cd7a34d453011f4a4b6f0300906052b0e03021d050003820101"
                + "00357fbe079401e111bf80db152752766983c756eca044610f8baab67427dc9b5f37df736da806e91a562939cf876a0998c1"
                + "232f31b99cf38f0e34d39c7e8a2cc04ed897bfdc91f7f292426063ec3ec5490e35c52a7f98ba86a4114976c45881373dacc9"
                + "5ad3e6847e1e28bb58e4f7cfc7138a56ce75f01a8050194159e1878bd90f9f580f63c6dd41e2d15cd80dc0a8db61101df900"
                + "9d891ec228f70f3a0a37358e7917fc94dfeb6e7cb176e8f5dbfa1ace2af6c0a4306e22eb3051e7705306152ce87328b24f7f"
                + "153d565b73aef677d25ae8657f81ca1cd5dd50404b70b9373eadcd2d276e263105c00607a86f0c10ab26d1aafd986313a36c"
                + "70389a4d1a8e883181c83081c5020100302e301a311830160603550403130f5253414b65795472616e7366657231021031d9"
                + "35fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481808396f2d79d5a98f93778c8522fbb40d49193"
                + "d3b2c0c1656ab987f7b3a34af282ada97d3f1e79ea84ab09f973b24ef5615097fca7e989a9017f94f65113e3c29bf7908863"
                + "8095255d522b08e863c041969ccdf9826a1aea42816ee6ec4bb399c663a4a4e891ec20ec778786d7efe91f6cade5859a9299"
                + "69fa5d990524578b6917302b06092a864886f70d010701301406082a864886f70d030704082a476e3ed67037f480088fb65b"
                + "4df40a6635").HexToByteArray();

            VerifyCertificates3(encodedMessage);
        }

        private static void VerifyCertificates3(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            X509Certificate2Collection certs = ecms.Certificates;
            Assert.Equal(3, certs.Count);

            X509Certificate2[] expectedCerts = s_certs.OrderBy(c => c.Issuer).ToArray();

            X509Certificate2[] actualCerts = new X509Certificate2[certs.Count];
            certs.CopyTo(actualCerts, 0);
            actualCerts = actualCerts.OrderBy(c => c.Issuer).ToArray();

            for (int i = 0; i < certs.Count; i++)
            {
                X509Certificate2 expectedCert = expectedCerts[i];
                X509Certificate2 actualCert = actualCerts[i];

                byte[] expectedDer = expectedCert.Export(X509ContentType.Cert);
                byte[] actualDer = actualCert.Export(X509ContentType.Cert);
                Assert.Equal<byte>(expectedDer, actualDer);
            }
        }

        private static X509Certificate2[] s_certs =
        {
            Certificates.RSAKeyTransfer1.GetCertificate(),
            Certificates.RSAKeyTransfer2.GetCertificate(),
            Certificates.DHKeyAgree1.GetCertificate(),
        };
    }
}

