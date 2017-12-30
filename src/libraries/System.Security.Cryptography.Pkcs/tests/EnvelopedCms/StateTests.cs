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
    public static partial class StateTests
    {
        public static bool SupportsCngCertificates { get; } = (!PlatformDetection.IsFullFramework || PlatformDetection.IsNetfx462OrNewer);

        //
        // Exercises various edge cases when EnvelopedCms methods and properties are called out of the "expected" order.
        //
        // The "expected" sequences are one of:
        //
        //    ctor(ContentInfo)|ctor(ContentInfo,AlgorithmIdentifier) => Encrypt() => Encode()
        //
        // or
        //
        //    ctor() => Decode() => RecipientInfos => Decrypt() => ContentInfo
        //
        // Most of these semantics are driven by backward compatibility. A tighter api design wouldn't
        // have exposed all these state transitions in the first place.  
        //
        // The four states an EnvelopedCms can be in are as follows:
        //
        //   State 1: Post constructor
        //
        //       There are three constructor overloads, but all but one just supply default arguments
        //       so we can consider there to be just one constructor.
        //
        //       At this stage, there is no CMS underneath, just some properties (ContentInfo, AlgorithmIdentifier, etc.)
        //       that serve as implicit inputs to a future Encrypt() call.
        //
        //   State 2: Post Encrypt()
        //
        //       Encrypt() can be called at any time and it effectively resets the state of the EnvelopedCms
        //       (although the prior contents of ContentInfo, ContentEncryptionAlgorithm, UnprotectedAttributes and Certificates
        //       will naturally influence the contents of CMS is constructed.)
        //
        //       Encrypt() actually both encrypts and encodes, but you have to call Encode() afterward to pick up the encoded bytes.
        //
        //   State 3: Post Decode()
        //
        //       Decode() can also be called at any time - it's effectively a constructor that resets the internal
        //       state and all the member properties. 
        //
        //       In this state, you can invoke the RecipientInfos properties to decide which recipient to pass to Decrypt().
        //
        //   State 4: Post Decrypt()
        //
        //       A Decrypt() can only happen after a Decode().
        //     
        //       Once in this state, you can fetch ContentInfo to get the decrypted content
        //       but otherwise, the CMS is in a pretty useless state.
        //

        //
        // State 1
        // 
        //    Constructed using any of the constructor overloads.
        //

        [Fact]
        public static void PostCtor_Version()
        {
            // Version returns 0 by fiat.
            EnvelopedCms ecms = new EnvelopedCms();
            Assert.Equal(0, ecms.Version);
        }

        [Fact]
        public static void PostCtor_RecipientInfos()
        {
            // RecipientInfo returns empty collection by fiat.
            EnvelopedCms ecms = new EnvelopedCms();
            RecipientInfoCollection recipients = ecms.RecipientInfos;
            Assert.Equal(0, recipients.Count);
        }

        [Fact]
        public static void PostCtor_Encode()
        {
            EnvelopedCms ecms = new EnvelopedCms();
            Assert.Throws<InvalidOperationException>(() => ecms.Encode());
        }

        [Fact]
        public static void PostCtor_Decrypt()
        {
            EnvelopedCms ecms = new EnvelopedCms();
            Assert.Throws<InvalidOperationException>(() => ecms.Decrypt());
        }

        [Fact]
        public static void PostCtor_ContentInfo()
        {
            ContentInfo expectedContentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(expectedContentInfo);
            ContentInfo actualContentInfo = ecms.ContentInfo;
            Assert.Equal(expectedContentInfo.ContentType, actualContentInfo.ContentType);
            Assert.Equal<byte>(expectedContentInfo.Content, actualContentInfo.Content);
        }

        //
        // State 2
        // 
        //    Called constructor + Encrypt()
        //

        [Fact]
        public static void PostEncrypt_Version()
        {
            ContentInfo expectedContentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(expectedContentInfo);
            int versionBeforeEncrypt = ecms.Version;
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                ecms.Encrypt(new CmsRecipient(cert));
            }

            // Encrypt does not update Version member.
            Assert.Equal(versionBeforeEncrypt, ecms.Version);
        }

        [Fact]
        public static void PostEncrypt_RecipientInfos()
        {
            ContentInfo expectedContentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(expectedContentInfo);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                ecms.Encrypt(new CmsRecipient(cert));
            }

            object ignore;
            Assert.ThrowsAny<CryptographicException>(() => ignore = ecms.RecipientInfos);
        }

        [Fact]
        public static void PostEncrypt_Decrypt()
        {
            ContentInfo expectedContentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(expectedContentInfo);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                ecms.Encrypt(new CmsRecipient(cert));
            }
            Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt());
        }

        [Fact]
        public static void PostEncrypt_ContentInfo()
        {
            ContentInfo expectedContentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(expectedContentInfo);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                ecms.Encrypt(new CmsRecipient(cert));
            }

            // Encrypting does not update ContentInfo.
            ContentInfo actualContentInfo = ecms.ContentInfo;
            Assert.Equal(expectedContentInfo.ContentType, actualContentInfo.ContentType);
            Assert.Equal<byte>(expectedContentInfo.Content, actualContentInfo.Content);
        }

        //
        // State 3: Called Decode()
        //

        public static void PostDecode_Encode(bool isRunningOnDesktop)
        {
            byte[] encodedMessage =
                ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
                + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
                + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
                + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
                + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            // This should really have thrown an InvalidOperationException. Instead, you get... something back.
            string expectedString = isRunningOnDesktop ? "35edc437e31d0b70000000000000" : "35edc437e31d0b70";
            byte[] expectedGarbage = expectedString.HexToByteArray();
            byte[] garbage = ecms.Encode();
            Assert.Equal(expectedGarbage, garbage);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void PostDecode_Encode_net46()
        {
            PostDecode_Encode(isRunningOnDesktop: true);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void PostDecode_Encode_netcore()
        {
            PostDecode_Encode(isRunningOnDesktop: false);
        }

        public static void PostDecode_ContentInfo(bool isRunningOnDesktop)
        {
            byte[] encodedMessage =
                ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
                + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
                + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
                + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
                + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            // This gets you the encrypted inner content.
            ContentInfo contentInfo = ecms.ContentInfo;
            Assert.Equal(Oids.Pkcs7Data, contentInfo.ContentType.Value);
            string expectedString = isRunningOnDesktop ? "35edc437e31d0b70000000000000" : "35edc437e31d0b70";
            byte[] expectedGarbage = expectedString.HexToByteArray();
            Assert.Equal(expectedGarbage, contentInfo.Content);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void PostDecode_ContentInfo_net46()
        {
            PostDecode_ContentInfo(isRunningOnDesktop: true);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void PostDecode_ContentInfo_netcore()
        {
            PostDecode_ContentInfo(isRunningOnDesktop: false);
        }

        //
        // State 4: Called Decode() + Decrypt()
        //
        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void PostDecrypt_Encode()
        {
            byte[] expectedContent = { 6, 3, 128, 33, 44 };
            EnvelopedCms ecms = new EnvelopedCms(new ContentInfo(expectedContent));
            ecms.Encrypt(new CmsRecipient(Certificates.RSAKeyTransfer1.GetCertificate()));
            byte[] encodedMessage =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818067"
                + "6bada56dcaf2e65226941242db73b5a5420a6212cd6af662db52fdc0ca63875cb69066f7074da0fc009ce724e2d73fb19380"
                + "2deea8d92b069486a41c7c4fc3cd0174a918a559f79319039b40ae797bcacc909c361275ee2a5b1f0ff09fb5c19508e3f5ac"
                + "051ac0f03603c27fb8993d49ac428f8bcfc23a90ef9b0fac0f423a302b06092a864886f70d010701301406082a864886f70d"
                + "0307040828dc4d72ca3132e48008546cc90f2c5d4b79").HexToByteArray();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 cer = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            {
                if (cer == null)
                    return; // Sorry - CertLoader is not configured to load certs with private keys - we've tested as much as we can.
                X509Certificate2Collection extraStore = new X509Certificate2Collection(cer);
                RecipientInfoCollection r = ecms.RecipientInfos;
                ecms.Decrypt(r[0], extraStore);

                // Desktop compat: Calling Encode() at this point should have thrown an InvalidOperationException. Instead, it returns
                // the decrypted inner content (same as ecms.ContentInfo.Content). This is easy for someone to take a reliance on
                // so for compat sake, we'd better keep it. 
                byte[] encoded = ecms.Encode();
                Assert.Equal<byte>(expectedContent, encoded);
            }
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void PostDecrypt_RecipientInfos()
        {
            byte[] expectedContent = { 6, 3, 128, 33, 44 };

            EnvelopedCms ecms = new EnvelopedCms(new ContentInfo(expectedContent));
            ecms.Encrypt(new CmsRecipient(Certificates.RSAKeyTransfer1.GetCertificate()));
            byte[] encodedMessage =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818067"
                + "6bada56dcaf2e65226941242db73b5a5420a6212cd6af662db52fdc0ca63875cb69066f7074da0fc009ce724e2d73fb19380"
                + "2deea8d92b069486a41c7c4fc3cd0174a918a559f79319039b40ae797bcacc909c361275ee2a5b1f0ff09fb5c19508e3f5ac"
                + "051ac0f03603c27fb8993d49ac428f8bcfc23a90ef9b0fac0f423a302b06092a864886f70d010701301406082a864886f70d"
                + "0307040828dc4d72ca3132e48008546cc90f2c5d4b79").HexToByteArray();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 cer = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            {
                if (cer == null)
                    return; // Sorry - CertLoader is not configured to load certs with private keys - we've tested as much as we can.
                X509Certificate2Collection extraStore = new X509Certificate2Collection(cer);
                RecipientInfoCollection col1 = ecms.RecipientInfos;
                ecms.Decrypt(col1[0], extraStore);

                // Make sure we can still RecipientInfos after a Decrypt()
                RecipientInfoCollection col2 = ecms.RecipientInfos;
                Assert.Equal(col1.Count, col2.Count);

                RecipientInfo r1 = col1[0];
                RecipientInfo r2 = col2[0];

                X509IssuerSerial is1 = (X509IssuerSerial)(r1.RecipientIdentifier.Value);
                X509IssuerSerial is2 = (X509IssuerSerial)(r2.RecipientIdentifier.Value);
                Assert.Equal(is1.IssuerName, is2.IssuerName);
                Assert.Equal(is1.SerialNumber, is2.SerialNumber);
            }
        }

        [ConditionalFact(nameof(SupportsCngCertificates))]
        [OuterLoop(/* Leaks key on disk if interrupted */)]
        public static void PostDecrypt_Decrypt()
        {
            byte[] expectedContent = { 6, 3, 128, 33, 44 };

            byte[] encodedMessage =
                 ("308202b006092a864886f70d010703a08202a13082029d020100318202583081c5020100302e301a31183016060355040313"
                + "0f5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004"
                + "81801026d9fb60d1a55686b73cf859c8bd66b58defda5e23e3da5f535f1427e3c5f7a4a2a94373e8e3ba5488a7c6a1059bfb"
                + "57301156698e7fca62671426d388fb3fb4373c9cb53132fda067598256bbfe8491b14dadaaf04d5fdfb2463f358ad0d6a594"
                + "bf6a4fbab6b3d725f08032e601492265e6336d5a638096f9975025ccd6393081c5020100302e301a31183016060355040313"
                + "0f5253414b65795472616e736665723202102bce9f9ece39f98044f0cd2faa9a14e7300d06092a864886f70d010101050004"
                + "8180b6497a2b789728f200ca1f974a676c531a4769f03f3929bd7526e7333ea483b4abb530a49c8532db5d4a4df66f173e3e"
                + "a4ba9e4814b584dc987ac87c46bb131daab535140968aafad8808100a2515e9c6d0c1f382b024992ce36b70b841628e0eb43"
                + "4db89545d702a8fbd3403188e7de7cb4bc1dcc3bc325467570654aaf2ee83081c5020100302e301a31183016060355040313"
                + "0f5253414b65795472616e736665723302104497d870785a23aa4432ed0106ef72a6300d06092a864886f70d010101050004"
                + "81807517e594c353d41abff334c6162988b78e05df7d79457c146fbc886d2d8057f594fa3a96cd8df5842c9758baac1fcdd5"
                + "d9672a9f8ef9426326cccaaf5954f2ae657f8c7b13aef2f811adb4954323aa8319a1e8f2ad4e5c96c1d3fbe413ae479e471b"
                + "b701cbdfa145c9b64f5e1f69f472804995d56c31351553f779cf8efec237303c06092a864886f70d010701301d0609608648"
                + "01650304012a041023a114c149d7d4017ce2f5ec7c5d53f980104e50ab3c15533743dd054ef3ff8b9d83").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            using (X509Certificate2 cert1 = Certificates.RSAKeyTransfer1.TryGetCertificateWithPrivateKey())
            using (X509Certificate2 cert2 = Certificates.RSAKeyTransfer2.TryGetCertificateWithPrivateKey())
            using (X509Certificate2 cert3 = Certificates.RSAKeyTransfer3.TryGetCertificateWithPrivateKey())
            {
                if (cert1 == null || cert2 == null || cert3 == null)
                    return; // Sorry - CertLoader is not configured to load certs with private keys - we've tested as much as we can.
                X509Certificate2Collection extraStore = new X509Certificate2Collection();
                extraStore.Add(cert1);
                extraStore.Add(cert2);
                extraStore.Add(cert3);
                RecipientInfoCollection r = ecms.RecipientInfos;
                ecms.Decrypt(r[0], extraStore);
                ContentInfo contentInfo = ecms.ContentInfo;
                Assert.Equal<byte>(expectedContent, contentInfo.Content);

                // Though this doesn't seem like a terribly unreasonable thing to attempt, attempting to call Decrypt() again
                // after a successful Decrypt() throws a CryptographicException saying "Already decrypted."
                Assert.ThrowsAny<CryptographicException>(() => ecms.Decrypt(r[1], extraStore));
            }
        }
    }
}


