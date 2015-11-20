// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class PublicKeyTests
    {
        private static PublicKey GetTestRsaKey()
        {
            using (var cert = new X509Certificate2(TestData.MsCertificate))
            {
                return cert.PublicKey;
            }
        }

        private static PublicKey GetTestDsaKey()
        {
            using (var cert = new X509Certificate2(TestData.DssCer))
            {
                return cert.PublicKey;
            }
        }

        [Fact]
        public static void TestOid_RSA()
        {
            PublicKey pk = GetTestRsaKey();
            Assert.Equal("1.2.840.113549.1.1.1", pk.Oid.Value);
        }

        [Fact]
        public static void TestOid_DSA()
        {
            PublicKey pk = GetTestDsaKey();
            Assert.Equal("1.2.840.10040.4.1", pk.Oid.Value);
        }

        [Fact]
        public static void TestEncodedKeyValue_RSA()
        {
            byte[] expectedPublicKey = (
                "3082010a0282010100e8af5ca2200df8287cbc057b7fadeeeb76ac28533f3adb" +
                "407db38e33e6573fa551153454a5cfb48ba93fa837e12d50ed35164eef4d7adb" +
                "137688b02cf0595ca9ebe1d72975e41b85279bf3f82d9e41362b0b40fbbe3bba" +
                "b95c759316524bca33c537b0f3eb7ea8f541155c08651d2137f02cba220b10b1" +
                "109d772285847c4fb91b90b0f5a3fe8bf40c9a4ea0f5c90a21e2aae3013647fd" +
                "2f826a8103f5a935dc94579dfb4bd40e82db388f12fee3d67a748864e162c425" +
                "2e2aae9d181f0e1eb6c2af24b40e50bcde1c935c49a679b5b6dbcef9707b2801" +
                "84b82a29cfbfa90505e1e00f714dfdad5c238329ebc7c54ac8e82784d37ec643" +
                "0b950005b14f6571c50203010001").HexToByteArray();

            PublicKey pk = GetTestRsaKey();

            Assert.Equal(expectedPublicKey, pk.EncodedKeyValue.RawData);
        }

        [Fact]
        public static void TestEncodedKeyValue_DSA()
        {
            byte[] expectedPublicKey = (
                "028180089a43f439b924bef3529d8d6206d1fca56a55caf52b41d6ce371ebf07" +
                "bda132c8eadc040007fcf4da06c1f30504ebd8a77d301f5a4702f01f0d2a0707" +
                "ac1da38dd3251883286e12456234da62eda0df5fe2fa07cd5b16f3638becca77" +
                "86312da7d3594a4bb14e353884da0e9aecb86e3c9bdb66fca78ea85e1cc3f2f8" +
                "bf0963").HexToByteArray();

            PublicKey pk = GetTestDsaKey();

            Assert.Equal(expectedPublicKey, pk.EncodedKeyValue.RawData);
        }

        [Fact]
        public static void TestEncodedParameters_RSA()
        {
            PublicKey pk = GetTestRsaKey();

            // RSA has no key parameters, so the answer is always
            // DER:NULL (type 0x05, length 0x00)
            Assert.Equal(new byte[] { 0x05, 0x00 }, pk.EncodedParameters.RawData);
        }

        [Fact]
        public static void TestEncodedParameters_DSA()
        {
            byte[] expectedParameters = (
                "3082011F02818100871018CC42552D14A5A9286AF283F3CFBA959B8835EC2180" +
                "511D0DCEB8B979285708C800FC10CB15337A4AC1A48ED31394072015A7A6B525" +
                "986B49E5E1139737A794833C1AA1E0EAAA7E9D4EFEB1E37A65DBC79F51269BA4" +
                "1E8F0763AA613E29C81C3B977AEEB3D3C3F6FEB25C270CDCB6AEE8CD205928DF" +
                "B33C44D2F2DBE819021500E241EDCF37C1C0E20AADB7B4E8FF7AA8FDE4E75D02" +
                "818100859B5AEB351CF8AD3FABAC22AE0350148FD1D55128472691709EC08481" +
                "584413E9E5E2F61345043B05D3519D88C021582CCEF808AF8F4B15BD901A310F" +
                "EFD518AF90ABA6F85F6563DB47AE214A84D0B7740C9394AA8E3C7BFEF1BEEDD0" +
                "DAFDA079BF75B2AE4EDB7480C18B9CDFA22E68A06C0685785F5CFB09C2B80B1D" +
                "05431D").HexToByteArray();

            PublicKey pk = GetTestDsaKey();
            Assert.Equal(expectedParameters, pk.EncodedParameters.RawData);
        }

        [Fact]
        public static void TestKey_RSA()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                RSA rsa = cert.GetRSAPublicKey();
                RSAParameters rsaParameters = rsa.ExportParameters(false);

                byte[] expectedModulus = (
                    "E8AF5CA2200DF8287CBC057B7FADEEEB76AC28533F3ADB407DB38E33E6573FA5" +
                    "51153454A5CFB48BA93FA837E12D50ED35164EEF4D7ADB137688B02CF0595CA9" +
                    "EBE1D72975E41B85279BF3F82D9E41362B0B40FBBE3BBAB95C759316524BCA33" +
                    "C537B0F3EB7EA8F541155C08651D2137F02CBA220B10B1109D772285847C4FB9" +
                    "1B90B0F5A3FE8BF40C9A4EA0F5C90A21E2AAE3013647FD2F826A8103F5A935DC" +
                    "94579DFB4BD40E82DB388F12FEE3D67A748864E162C4252E2AAE9D181F0E1EB6" +
                    "C2AF24B40E50BCDE1C935C49A679B5B6DBCEF9707B280184B82A29CFBFA90505" +
                    "E1E00F714DFDAD5C238329EBC7C54AC8E82784D37EC6430B950005B14F6571C5").HexToByteArray();

                byte[] expectedExponent = new byte[] { 0x01, 0x00, 0x01 };

                Assert.Equal(expectedModulus, rsaParameters.Modulus);
                Assert.Equal(expectedExponent, rsaParameters.Exponent);
            }
        }

        [Fact]
        public static void TestECDsaPublicKey()
        {
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello");

            using (var cert = new X509Certificate2(TestData.ECDsa384Certificate))
            using (ECDsa publicKey = cert.GetECDsaPublicKey())
            {
                Assert.Equal(384, publicKey.KeySize);

                // The public key should be unable to sign.
                Assert.ThrowsAny<CryptographicException>(() => publicKey.SignData(helloBytes, HashAlgorithmName.SHA256));
            }
        }

        [Fact]
        public static void TestECDsaPublicKey_ValidatesSignature()
        {
            // This signature was produced as the output of ECDsaCng.SignData with the same key
            // on .NET 4.6.  Ensure it is verified here as a data compatibility test.
            //
            // Note that since ECDSA signatures contain randomness as an input, this value is unlikely
            // to be reproduced by another equivalent program.
            byte[] existingSignature =
            {
                // r:
                0x7E, 0xD7, 0xEF, 0x46, 0x04, 0x92, 0x61, 0x27,
                0x9F, 0xC9, 0x1B, 0x7B, 0x8A, 0x41, 0x6A, 0xC6,
                0xCF, 0xD4, 0xD4, 0xD1, 0x73, 0x05, 0x1F, 0xF3,
                0x75, 0xB2, 0x13, 0xFA, 0x82, 0x2B, 0x55, 0x11,
                0xBE, 0x57, 0x4F, 0x20, 0x07, 0x24, 0xB7, 0xE5,
                0x24, 0x44, 0x33, 0xC3, 0xB6, 0x8F, 0xBC, 0x1F,

                // s:
                0x48, 0x57, 0x25, 0x39, 0xC0, 0x84, 0xB9, 0x0E,
                0xDA, 0x32, 0x35, 0x16, 0xEF, 0xA0, 0xE2, 0x34,
                0x35, 0x7E, 0x10, 0x38, 0xA5, 0xE4, 0x8B, 0xD3,
                0xFC, 0xE7, 0x60, 0x25, 0x4E, 0x63, 0xF7, 0xDB,
                0x7C, 0xBF, 0x18, 0xD6, 0xD3, 0x49, 0xD0, 0x93,
                0x08, 0xC5, 0xAA, 0xA6, 0xE5, 0xFD, 0xD0, 0x96,
            };

            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello");

            using (var cert = new X509Certificate2(TestData.ECDsa384Certificate))
            using (ECDsa publicKey = cert.GetECDsaPublicKey())
            {
                Assert.Equal(384, publicKey.KeySize);

                bool isSignatureValid = publicKey.VerifyData(helloBytes, existingSignature, HashAlgorithmName.SHA256);
                Assert.True(isSignatureValid, "isSignatureValid");
            }
        }

        [Fact]
        public static void TestECDsaPublicKey_NonSignatureCert()
        {
            using (var cert = new X509Certificate2(TestData.EccCert_KeyAgreement))
            using (ECDsa publicKey = cert.GetECDsaPublicKey())
            {
                // It is an Elliptic Curve Cryptography public key.
                Assert.Equal("1.2.840.10045.2.1", cert.PublicKey.Oid.Value);

                // But, due to KeyUsage, it shouldn't be used for ECDSA.
                Assert.Null(publicKey);
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void TestKey_ECDsaCng256()
        {
            TestKey_ECDsaCng(TestData.ECDsa256Certificate, TestData.ECDsaCng256PublicKey);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void TestKey_ECDsaCng384()
        {
            TestKey_ECDsaCng(TestData.ECDsa384Certificate, TestData.ECDsaCng384PublicKey);
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void TestKey_ECDsaCng521()
        {
            TestKey_ECDsaCng(TestData.ECDsa521Certificate, TestData.ECDsaCng521PublicKey);
        }

        private static void TestKey_ECDsaCng(byte[] certBytes, TestData.ECDsaCngKeyValues expected)
        {
#if !NETNATIVE
            using (X509Certificate2 cert = new X509Certificate2(certBytes))
            {
                ECDsaCng e = (ECDsaCng)(cert.GetECDsaPublicKey());
                CngKey k = e.Key;
                byte[] blob = k.Export(CngKeyBlobFormat.EccPublicBlob);
                using (BinaryReader br = new BinaryReader(new MemoryStream(blob)))
                {
                    int magic = br.ReadInt32();
                    int cbKey = br.ReadInt32();
                    Assert.Equal(expected.QX.Length, cbKey);

                    byte[] qx = br.ReadBytes(cbKey);
                    byte[] qy = br.ReadBytes(cbKey);
                    Assert.Equal<byte>(expected.QX, qx);
                    Assert.Equal<byte>(expected.QY, qy);
                }
            }
#endif //!NETNATIVE
        }
    }
}
