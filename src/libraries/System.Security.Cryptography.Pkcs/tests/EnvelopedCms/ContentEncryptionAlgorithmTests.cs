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
    public static partial class ContentEncryptionAlgorithmTests
    {
        [Fact]
        public static void DecodeAlgorithmRc2_128_RoundTrip()
        {
            AlgorithmIdentifier algorithm = new AlgorithmIdentifier(new Oid(Oids.Rc2));
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, algorithm);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            VerifyAlgorithmRc2_128(encodedMessage);
        }

        [Fact]
        public static void DecodeAlgorithmRc2_128_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082011306092a864886f70d010703a0820104308201000201003181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "802fb2f6a7ccc1c696177c1ddb5ac92ab7f556dce4d21e924c37a06fbdb7015fd35dee9726f6301ca86af50b14275bf34584"
                + "3571848bf6f55281c75fb67adc9c63fac5c4427b38f4fab1567f2f08063a786059f9850c79ff202d1b556e8c90e41f977090"
                + "3c2d84a9046a372a0619a29713179304355750c9f6c180d1cc92d9b22b303006092a864886f70d010701301906082a864886"
                + "f70d0302300d02013a04086bcd05b70546e632800810f6c8d0e0466ee6").HexToByteArray();
            VerifyAlgorithmRc2_128(encodedMessage);
        }

        private static void VerifyAlgorithmRc2_128(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AlgorithmIdentifier algorithm = ecms.ContentEncryptionAlgorithm;
            Assert.NotNull(algorithm.Oid);
            Assert.Equal(Oids.Rc2, algorithm.Oid.Value);
            Assert.Equal(128, algorithm.KeyLength);
        }

        [Fact]
        public static void DecodeAlgorithmDes_RoundTrip()
        {
            AlgorithmIdentifier algorithm = new AlgorithmIdentifier(new Oid(Oids.Des));
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, algorithm);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            VerifyAlgorithmDes(encodedMessage);
        }

        [Fact]
        public static void DecodeAlgorithmDes_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082010906092a864886f70d010703a081fb3081f80201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481801f"
                + "0621d0d37f0e89c2eac528c2bf97eff89131aa55f08c286d6c41f403168e74bf49c39d4752830ff2b222b704dbec0a3bb109"
                + "0f6d39a2abb14819083f0a2c767958ebe19a2b73147306202da9ca483b911a0218ffb4ca3046de322cf3be6c1500af3d6b52"
                + "f02e3fa5a1a85e3fa035b3df65400fd29d8104d93481a6716c170c302806092a864886f70d010701301106052b0e03020704"
                + "0880052d38754b7f298008fc778a46c054e572").HexToByteArray();
            VerifyAlgorithmDes(encodedMessage);
        }

        private static void VerifyAlgorithmDes(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AlgorithmIdentifier algorithm = ecms.ContentEncryptionAlgorithm;
            Assert.NotNull(algorithm.Oid);
            Assert.Equal(Oids.Des, algorithm.Oid.Value);
            Assert.Equal(64, algorithm.KeyLength);
        }

        [Fact]
        public static void DecodeAlgorithm3Des_RoundTrip()
        {
            AlgorithmIdentifier algorithm = new AlgorithmIdentifier(new Oid(Oids.TripleDesCbc));
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, algorithm);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            VerifyAlgorithm3Des(encodedMessage);
        }

        [Fact]
        public static void DecodeAlgorithm3Des_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818087"
                + "77495ce527339dc78b063477104d513eda6f8a7b2f5c642fddf81d86a4c139f836590a1f81efafb953f7c6d666021fe5c031"
                + "10064f21ce4b17f4737a9370298a8b540b1d597fbc39d21a537b45d9dc65c8d2cbafcc6c7208b5f0453f7ef206f4b1d99cc0"
                + "7186f7f5b31a0a9ec885296ae27183f51b83a64bb8bf46ece16305302b06092a864886f70d010701301406082a864886f70d"
                + "03070408d8ac6958c16ea6f58008beb49fa4214d1e3f").HexToByteArray(); VerifyAlgorithm3Des(encodedMessage);
        }

        private static void VerifyAlgorithm3Des(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AlgorithmIdentifier algorithm = ecms.ContentEncryptionAlgorithm;
            Assert.NotNull(algorithm.Oid);
            Assert.Equal(Oids.TripleDesCbc, algorithm.Oid.Value);
            Assert.Equal(192, algorithm.KeyLength);
        }

        [Fact]
        public static void DecodeAlgorithmRc4_RoundTrip()
        {
            AlgorithmIdentifier algorithm = new AlgorithmIdentifier(new Oid(Oids.Rc4));
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, algorithm);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            VerifyAlgorithmRc4(encodedMessage);
        }

        [Fact]
        public static void DecodeAlgorithmRc4_FixedValue()
        {
            byte[] encodedMessage =
                 ("3081ff06092a864886f70d010703a081f13081ee0201003181c83081c5020100302e301a311830160603550403130f525341"
                + "4b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818036ce"
                + "a9d8c17763ceffa0c4902da6e02a2168349eba10c66f827fb427c944d035c0cea65a7729131527b38b1c5e0b378205bb571a"
                + "c94ea26e2b4e8ab9b53d5ec7fec48a095d1145769878e6b2947adf41ad924004b185914bed859b2be7e84bbdb59b45663c2c"
                + "56392895c0534766e743b70db12cd08377c35d9cdf21ac7eb4a4301e06092a864886f70d010701300c06082a864886f70d03"
                + "04050080030039bd").HexToByteArray();
            VerifyAlgorithmRc4(encodedMessage);
        }

        private static void VerifyAlgorithmRc4(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AlgorithmIdentifier algorithm = ecms.ContentEncryptionAlgorithm;
            Assert.NotNull(algorithm.Oid);
            Assert.Equal(Oids.Rc4, algorithm.Oid.Value);
            Assert.Equal(128, algorithm.KeyLength);
        }

        [Fact]
        public static void DecodeAlgorithmAes128_RoundTrip()
        {
            AlgorithmIdentifier algorithm = new AlgorithmIdentifier(new Oid(Oids.Aes128));
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, algorithm);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            VerifyAlgorithmAes128(encodedMessage);
        }

        [Fact]
        public static void DecodeAlgorithmAes128_FixedValue()
        {
            byte[] encodedMessage =
                ("3082011f06092a864886f70d010703a08201103082010c0201003181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "8011e777f410a2c2ab5152303dc011da5bfc5982f2254040ed00bdcfaa028a14624fb63c817082d6e373b4cdbbcce3ab5dee"
                + "bf85c33cea8ffa40b31a784b61dde7eab3736261f0d912b829773201bdf6cb93a602127a30cad5fa1b3034ba10cd4fddcfe5"
                + "f30bb05ffc2171b18d3200ef21bda8631a4b82af603277db7ebb752999303c06092a864886f70d010701301d060960864801"
                + "65030401020410d38e15cc9b02555ae95a75e5a7af86e98010c000f2c29b88ec5e4e6ba51159abae55").HexToByteArray();
            VerifyAlgorithmAes128(encodedMessage);
        }

        private static void VerifyAlgorithmAes128(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AlgorithmIdentifier algorithm = ecms.ContentEncryptionAlgorithm;
            Assert.NotNull(algorithm.Oid);
            Assert.Equal(Oids.Aes128, algorithm.Oid.Value);
            Assert.Equal(0, algorithm.KeyLength);
        }

        [Fact]
        public static void DecodeAlgorithmAes192_RoundTrip()
        {
            AlgorithmIdentifier algorithm = new AlgorithmIdentifier(new Oid(Oids.Aes192));
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, algorithm);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            VerifyAlgorithmAes192(encodedMessage);
        }

        [Fact]
        public static void DecodeAlgorithmAes192_FixedValue()
        {
            byte[] encodedMessage =
                ("3082011f06092a864886f70d010703a08201103082010c0201003181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "8095870ef593f7234a6a12fc23f6dacd75d6e5a5ee05077b7390632c0beb2689a3dd587757e976992ffd26f6dd374fb64f47"
                + "6eb4d920a55d735935716671bc12dc65b84c29c5a72aa78a4480e19a28ac09395e708e99e1e9e9704ee4d077541bfed1d06d"
                + "32f3a7e9441fde9133858a0e825af04a36b5943e0f39eade1463de7c12303c06092a864886f70d010701301d060960864801"
                + "65030401160410d217d9a8bb30516d54aab00a5e6089b68010149eec8997deedcbad000ae6c1a7fb9d").HexToByteArray();
            VerifyAlgorithmAes192(encodedMessage);
        }

        private static void VerifyAlgorithmAes192(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AlgorithmIdentifier algorithm = ecms.ContentEncryptionAlgorithm;
            Assert.NotNull(algorithm.Oid);
            Assert.Equal(Oids.Aes192, algorithm.Oid.Value);
            Assert.Equal(0, algorithm.KeyLength);
        }

        [Fact]
        public static void DecodeAlgorithmAes256_RoundTrip()
        {
            AlgorithmIdentifier algorithm = new AlgorithmIdentifier(new Oid(Oids.Aes256));
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo, algorithm);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            VerifyAlgorithmAes256(encodedMessage);
        }

        [Fact]
        public static void DecodeAlgorithmAes256_FixedValue()
        {
            byte[] encodedMessage =
                ("3082011f06092a864886f70d010703a08201103082010c0201003181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "809605c24f3bc99b3437f3e12e44c3a49c91ca0dc94a473fc21388a0f1c69486befa97eb7b9a96e2a9309f89612ad108d1c7"
                + "2db6cc66426253e639939b9be852df9212fba9bb52f857a39a26c04a20bae7b7620a1e53873a7ef03c4139edc7a50ee297ea"
                + "fdc1372596ef299e71b6d4db146cad48a8485e17b3604a56958afdbe83303c06092a864886f70d010701301d060960864801"
                + "650304012a04100b85a6899050456469102f41aaa685158010b3008bd0eb863574ecbe46a5cc91a99c").HexToByteArray();

            VerifyAlgorithmAes256(encodedMessage);
        }

        private static void VerifyAlgorithmAes256(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AlgorithmIdentifier algorithm = ecms.ContentEncryptionAlgorithm;
            Assert.NotNull(algorithm.Oid);
            Assert.Equal(Oids.Aes256, algorithm.Oid.Value);
            Assert.Equal(0, algorithm.KeyLength);
        }
    }
}


