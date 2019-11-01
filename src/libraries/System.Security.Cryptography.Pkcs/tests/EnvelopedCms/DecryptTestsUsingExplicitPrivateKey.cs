// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Pkcs.Tests;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public class DecryptTestsUsingExplicitPrivateKey : DecryptTests
    {
        public DecryptTestsUsingExplicitPrivateKey() : base(true) { }

        [Fact]
        public static void DecryptUsingWrongPrivateKeyType()
        {
            byte[] content = new byte[] { 1, 2, 3, 4 };
            ContentInfo contentInfo = new ContentInfo(content);
            AlgorithmIdentifier alg = new AlgorithmIdentifier(new Oid(Oids.Aes192));

            EnvelopedCms ecms = new EnvelopedCms(contentInfo, alg);

            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(SubjectIdentifierType.IssuerAndSerialNumber, cert);
                ecms.Encrypt(cmsRecipient);
            }

            byte[] encoded = ecms.Encode();
            ecms = new EnvelopedCms();
            ecms.Decode(encoded);

            using (ECDsa ecdsa = ECDsa.Create())
            {
                Assert.Throws<CryptographicException>(() => ecms.Decrypt(ecms.RecipientInfos[0], ecdsa));
            }
        }

        [Fact]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public void TestDecryptSimpleAes256_RsaOaepMd5_Throws()
        {
            // Generated with:
            // openssl cms -encrypt -in input.txt -out out.msg -recip cert.pem -keyopt rsa_padding_mode:oaep -keyopt rsa_mgf1_md:md5 --aes256 -keyopt rsa_oaep_md:md5
            byte[] encodedMessage = Convert.FromBase64String(
                    "MIIBUgYJKoZIhvcNAQcDoIIBQzCCAT8CAQAxgfswgfgCAQAwNDAgMR4wHAYDVQQD" +
                    "ExVSU0FTaGEyNTZLZXlUcmFuc2ZlcjECEHLGx3NJFkaMTWCCU9oBdnYwOgYJKoZI" +
                    "hvcNAQEHMC2gDjAMBggqhkiG9w0CBQUAoRswGQYJKoZIhvcNAQEIMAwGCCqGSIb3" +
                    "DQIFBQAEgYCqBXo+gdyWvxfMJ3WR6Tw76i8N69awXjhuCQulWqZtYQiBoVb1fptK" +
                    "mBIcsnEHuHvG8s4UIU/vzLvB/SFCTYR0pJAT0rY2dkjQYsGtup822qO0cbWE9aqO" +
                    "D/fkVd8LREoMrMF3AGF6r9SclgWUDi871sfGcKwpjzU9gx5C9aasIjA8BgkqhkiG" +
                    "9w0BBwEwHQYJYIZIAWUDBAEqBBAA7mDmj510sKUxedURDS2lgBD7or6d7ZeMhZL6" +
                    "6rUUMLoU");
            byte[] content = "68690D0A".HexToByteArray();
            ContentInfo expectedContentInfo = new ContentInfo(new Oid(Oids.Pkcs7Data), content);

            Assert.ThrowsAny<CryptographicException>(
                () => VerifySimpleDecrypt(encodedMessage, Certificates.RSASha256KeyTransfer1, expectedContentInfo));
        }
    }
}
