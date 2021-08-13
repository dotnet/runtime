// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static PublicKey GetTestECDsaKey()
        {
            using (var cert = new X509Certificate2(TestData.ECDsa256Certificate))
            {
                return cert.PublicKey;
            }
        }

        private static PublicKey GetTestECDHKey()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.EcDh256Certificate))
            {
                return cert.PublicKey;
            }
        }

        /// <summary>
        /// First parameter is the cert, the second is a hash of "Hello"
        /// </summary>
        public static IEnumerable<object[]> BrainpoolCurves
        {
            get
            {
                yield return new object[] {
                    TestData.ECDsabrainpoolP160r1_CertificatePemBytes,
                    "9145C79DD4DF758EB377D13B0DB81F83CE1A63A4099DDC32FE228B06EB1F306423ED61B6B4AF4691".HexToByteArray() };

                yield return new object[] {
                    TestData.ECDsabrainpoolP160r1_ExplicitCertificatePemBytes,
                    "6D74F1C9BCBBA5A25F67E670B3DABDB36C24E8FAC3266847EB2EE7E3239208ADC696BB421AB380B4".HexToByteArray() };
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
        public static void TestOid_ECDSA()
        {
            PublicKey pk = GetTestECDsaKey();
            Assert.Equal("1.2.840.10045.2.1", pk.Oid.Value);
        }

        [Fact]
        public static void TestOid_ECDH()
        {
            PublicKey pk = GetTestECDHKey();
            Assert.Equal("1.2.840.10045.2.1", pk.Oid.Value);
        }

        [Fact]
        public static void TestPublicKey_Key_RSA()
        {
            PublicKey pk = GetTestRsaKey();
            using (AsymmetricAlgorithm alg = pk.Key)
            {
                Assert.NotNull(alg);
                Assert.Same(alg, pk.Key);
                Assert.Equal(2048, alg.KeySize);

                Assert.IsAssignableFrom<RSA>(alg);
                VerifyKey_RSA( /* cert */ null, (RSA)alg);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void TestPublicKey_Key_DSA()
        {
            PublicKey pk = GetTestDsaKey();
            using (AsymmetricAlgorithm alg = pk.Key)
            {
                Assert.NotNull(alg);
                Assert.Same(alg, pk.Key);
                Assert.Equal(1024, alg.KeySize);

                Assert.IsAssignableFrom<DSA>(alg);
                VerifyKey_DSA((DSA)alg);
            }
        }

        [Fact]
        public static void TestPublicKey_Key_ECDSA()
        {
            PublicKey pk = GetTestECDsaKey();

            Assert.Throws<NotSupportedException>(() => pk.Key);
        }

        [Fact]
        public static void TestPublicKey_Key_ECDH()
        {
            PublicKey pk = GetTestECDHKey();

            Assert.Throws<NotSupportedException>(() => pk.Key);
        }

        private static void VerifyKey_DSA(DSA dsa)
        {
            DSAParameters dsaParameters = dsa.ExportParameters(false);

            byte[] expected_g = (
                "859B5AEB351CF8AD3FABAC22AE0350148FD1D55128472691709EC08481584413" +
                "E9E5E2F61345043B05D3519D88C021582CCEF808AF8F4B15BD901A310FEFD518" +
                "AF90ABA6F85F6563DB47AE214A84D0B7740C9394AA8E3C7BFEF1BEEDD0DAFDA0" +
                "79BF75B2AE4EDB7480C18B9CDFA22E68A06C0685785F5CFB09C2B80B1D05431D").HexToByteArray();
            byte[] expected_p = (
                "871018CC42552D14A5A9286AF283F3CFBA959B8835EC2180511D0DCEB8B97928" +
                "5708C800FC10CB15337A4AC1A48ED31394072015A7A6B525986B49E5E1139737" +
                "A794833C1AA1E0EAAA7E9D4EFEB1E37A65DBC79F51269BA41E8F0763AA613E29" +
                "C81C3B977AEEB3D3C3F6FEB25C270CDCB6AEE8CD205928DFB33C44D2F2DBE819").HexToByteArray();
            byte[] expected_q = "E241EDCF37C1C0E20AADB7B4E8FF7AA8FDE4E75D".HexToByteArray();
            byte[] expected_y = (
                "089A43F439B924BEF3529D8D6206D1FCA56A55CAF52B41D6CE371EBF07BDA132" +
                "C8EADC040007FCF4DA06C1F30504EBD8A77D301F5A4702F01F0D2A0707AC1DA3" +
                "8DD3251883286E12456234DA62EDA0DF5FE2FA07CD5B16F3638BECCA7786312D" +
                "A7D3594A4BB14E353884DA0E9AECB86E3C9BDB66FCA78EA85E1CC3F2F8BF0963").HexToByteArray();

            Assert.Equal(expected_g, dsaParameters.G);
            Assert.Equal(expected_p, dsaParameters.P);
            Assert.Equal(expected_q, dsaParameters.Q);
            Assert.Equal(expected_y, dsaParameters.Y);
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
        public static void TestEncodedKeyValue_ECDSA()
        {
            // Uncompressed key (04), then the X coord, then the Y coord.
            string expectedPublicKeyHex =
                "04" +
                "448D98EE08AEBA0D8B40F3C6DBD500E8B69F07C70C661771655228EA5A178A91" +
                "0EF5CB1759F6F2E062021D4F973F5BB62031BE87AE915CFF121586809E3219AF";

            PublicKey pk = GetTestECDsaKey();

            Assert.Equal(expectedPublicKeyHex, pk.EncodedKeyValue.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void TestEncodedKeyValue_ECDH()
        {
            // Uncompressed key (04), then the X coord, then the Y coord.
            string expectedPublicKeyHex =
                "04" +
                "4337F39C7AB746015018DE0E23F94A802EAE96ADE89858CFE482E813CD2BDF40" +
                "A1AC886F2C1D6135C7DEC49EC1EE892D1F8F96E75CAFF8420D2EFED269629E78";

            PublicKey pk = GetTestECDHKey();

            Assert.Equal(expectedPublicKeyHex, pk.EncodedKeyValue.RawData.ByteArrayToHex());
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
        public static void TestEncodedParameters_ECDSA()
        {
            // OID: 1.2.840.10045.3.1.7
            string expectedParametersHex = "06082A8648CE3D030107";

            PublicKey pk = GetTestECDsaKey();
            Assert.Equal(expectedParametersHex, pk.EncodedParameters.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void TestEncodedParameters_ECDH()
        {
            // OID: 1.2.840.10045.3.1.7
            string expectedParametersHex = "06082A8648CE3D030107";

            PublicKey pk = GetTestECDHKey();
            Assert.Equal(expectedParametersHex, pk.EncodedParameters.RawData.ByteArrayToHex());
        }

        [Fact]
        public static void TestKey_RSA()
        {
            using (X509Certificate2 cert = new X509Certificate2(TestData.MsCertificate))
            {
                RSA rsa = cert.GetRSAPublicKey();

                VerifyKey_RSA(cert, rsa);
            }
        }

        private static void VerifyKey_RSA(X509Certificate2 cert, RSA rsa)
        {
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

            byte[] originalModulus = rsaParameters.Modulus;
            byte[] originalExponent = rsaParameters.Exponent;

            if (!expectedModulus.SequenceEqual(rsaParameters.Modulus) ||
                !expectedExponent.SequenceEqual(rsaParameters.Exponent))
            {
                Console.WriteLine("Modulus or Exponent not equal");

                rsaParameters = rsa.ExportParameters(false);

                if (!expectedModulus.SequenceEqual(rsaParameters.Modulus) ||
                    !expectedExponent.SequenceEqual(rsaParameters.Exponent))
                {
                    Console.WriteLine("Second call to ExportParameters did not produce valid data either");
                }

                if (cert != null)
                {
                    rsa = cert.GetRSAPublicKey();
                    rsaParameters = rsa.ExportParameters(false);

                    if (!expectedModulus.SequenceEqual(rsaParameters.Modulus) ||
                        !expectedExponent.SequenceEqual(rsaParameters.Exponent))
                    {
                        Console.WriteLine("New key handle ExportParameters was not successful either");
                    }
                }
            }

            Assert.Equal(expectedModulus, originalModulus);
            Assert.Equal(expectedExponent, originalExponent);
        }

        [Fact]
        public static void TestKey_RSA384_ValidatesSignature()
        {
            byte[] signature =
            {
                0x79, 0xD9, 0x3C, 0xBF, 0x54, 0xFA, 0x55, 0x8C,
                0x44, 0xC3, 0xC3, 0x83, 0x85, 0xBB, 0x78, 0x44,
                0xCD, 0x0F, 0x5A, 0x8E, 0x71, 0xC9, 0xC2, 0x68,
                0x68, 0x0A, 0x33, 0x93, 0x19, 0x37, 0x02, 0x06,
                0xE2, 0xF7, 0x67, 0x97, 0x3C, 0x67, 0xB3, 0xF4,
                0x11, 0xE0, 0x6E, 0xD2, 0x22, 0x75, 0xE7, 0x7C,
            };

            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello");

            using (var cert = new X509Certificate2(TestData.Rsa384CertificatePemBytes))
            using (RSA rsa = cert.GetRSAPublicKey())
            {
                Assert.True(rsa.VerifyData(helloBytes, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
            }
        }

        [Theory, MemberData(nameof(BrainpoolCurves))]
        public static void TestKey_ECDsabrainpool_PublicKey(byte[] curveData, byte[] notUsed)
        {
            _ = notUsed;
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello");

            try
            {
                using (var cert = new X509Certificate2(curveData))
                {
                    using (ECDsa ec = cert.GetECDsaPublicKey())
                    {
                        Assert.Equal(160, ec.KeySize);

                        // The public key should be unable to sign.
                        Assert.ThrowsAny<CryptographicException>(() => ec.SignData(helloBytes, HashAlgorithmName.SHA256));
                    }
                }
            }
            catch (CryptographicException)
            {
                // Windows 7, Windows 8, CentOS can fail. Verify known good platforms don't fail.
                Assert.False(PlatformDetection.IsWindows && PlatformDetection.WindowsVersion >= 10);
                Assert.False(PlatformDetection.IsUbuntu);
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
        public static void TestECDHPublicKey()
        {
            using (var cert = new X509Certificate2(TestData.EcDh256Certificate))
            using (ECDiffieHellman publicKey = cert.GetECDiffieHellmanPublicKey())
            using (ECDiffieHellman otherParty = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256))
            {
                Assert.Equal(256, publicKey.KeySize);

                // The public key should be unable to derive a secret private parameters.
                Assert.ThrowsAny<CryptographicException>(() => publicKey.DeriveKeyFromHash(otherParty.PublicKey, HashAlgorithmName.SHA256));
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
        public static void TestECDHPublicKey_DeriveSecret()
        {
            using (var publicEcdhCert = new X509Certificate2(TestData.EccCert_KeyAgreement))
            using (ECDiffieHellman otherParty = ECDiffieHellman.Create())
            using (ECDiffieHellman publicKey = publicEcdhCert.GetECDiffieHellmanPublicKey())
            {
                otherParty.ImportFromPem(TestData.EcDhPkcs8Key);
                byte[] key = otherParty.DeriveKeyFromHash(publicKey.PublicKey, HashAlgorithmName.SHA256);

                byte[] expectedKey = new byte[]
                {
                    0x3f, 0x67, 0x8f, 0x51, 0xa0, 0x91, 0xfa, 0x5d,
                    0x38, 0x00, 0x92, 0xd3, 0x5b, 0x29, 0xd4, 0x00,
                    0x1c, 0x4a, 0x8f, 0x30, 0x7d, 0x78, 0xf4, 0x79,
                    0xed, 0x3d, 0x0b, 0x23, 0xdc, 0xfa, 0x26, 0x57
                };

                Assert.Equal(expectedKey, key);
            }
        }

        [Theory, MemberData(nameof(BrainpoolCurves))]
        public static void TestECDsaPublicKey_BrainpoolP160r1_ValidatesSignature(byte[] curveData, byte[] existingSignature)
        {
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello");

            try
            {
                using (var cert = new X509Certificate2(curveData))
                {
                    using (ECDsa publicKey = cert.GetECDsaPublicKey())
                    {
                        Assert.Equal(160, publicKey.KeySize);

                        // It is an Elliptic Curve Cryptography public key.
                        Assert.Equal("1.2.840.10045.2.1", cert.PublicKey.Oid.Value);

                        bool isSignatureValid = publicKey.VerifyData(helloBytes, existingSignature, HashAlgorithmName.SHA256);

                        if (!isSignatureValid)
                        {
                            Assert.True(PlatformDetection.IsAndroid, "signature invalid on Android only");
                            return;
                        }

                        unchecked
                        {
                            --existingSignature[existingSignature.Length - 1];
                        }
                        isSignatureValid = publicKey.VerifyData(helloBytes, existingSignature, HashAlgorithmName.SHA256);
                        Assert.False(isSignatureValid, "isSignatureValidNeg");
                    }
                }
            }
            catch (CryptographicException)
            {
                // Windows 7, Windows 8, CentOS can fail. Verify known good platforms don't fail.
                Assert.False(PlatformDetection.IsWindows && PlatformDetection.WindowsVersion >= 10);
                Assert.False(PlatformDetection.IsUbuntu);
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
        public static void TestECDsa224PublicKey()
        {
            using (var cert = new X509Certificate2(TestData.ECDsa224Certificate))
            {
                // It is an Elliptic Curve Cryptography public key.
                Assert.Equal("1.2.840.10045.2.1", cert.PublicKey.Oid.Value);

                ECDsa ecdsa;

                try
                {
                    ecdsa = cert.GetECDsaPublicKey();
                }
                catch (CryptographicException)
                {
                    // Windows 7, Windows 8, CentOS.
                    return;
                }

                // Other Unix
                using (ecdsa)
                {
                    byte[] data = ByteUtils.AsciiBytes("Hello");

                    byte[] signature = (
                        // r
                        "8ede5053d546d35c1aba829bca3ecf493eb7a73f751548bd4cf2ad10" +
                        // s
                        "5e3da9d359001a6be18e2b4e49205e5219f30a9daeb026159f41b9de").HexToByteArray();

                    Assert.True(ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA1));
                }
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void TestDSAPublicKey()
        {
            using (var cert = new X509Certificate2(TestData.DssCer))
            using (DSA pubKey = cert.GetDSAPublicKey())
            {
                Assert.NotNull(pubKey);
                VerifyKey_DSA(pubKey);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void TestDSAPublicKey_VerifiesSignature()
        {
            byte[] data = { 1, 2, 3, 4, 5 };
            byte[] wrongData = { 0xFE, 2, 3, 4, 5 };
            byte[] signature =
                "B06E26CFC939F25B864F52ABD3288222363A164259B0027FFC95DBC88F9204F7A51A901F3005C9F7".HexToByteArray();

            using (var cert = new X509Certificate2(TestData.Dsa1024Cert))
            using (DSA pubKey = cert.GetDSAPublicKey())
            {
                Assert.True(pubKey.VerifyData(data, signature, HashAlgorithmName.SHA1), "pubKey verifies signature");
                Assert.False(pubKey.VerifyData(wrongData, signature, HashAlgorithmName.SHA1), "pubKey verifies tampered data");

                signature[0] ^= 0xFF;
                Assert.False(pubKey.VerifyData(data, signature, HashAlgorithmName.SHA1), "pubKey verifies tampered signature");
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void TestDSAPublicKey_RSACert()
        {
            using (var cert = new X509Certificate2(TestData.Rsa384CertificatePemBytes))
            using (DSA pubKey = cert.GetDSAPublicKey())
            {
                Assert.Null(pubKey);
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void TestDSAPublicKey_ECDSACert()
        {
            using (var cert = new X509Certificate2(TestData.ECDsa256Certificate))
            using (DSA pubKey = cert.GetDSAPublicKey())
            {
                Assert.Null(pubKey);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void TestKey_ECDsaCng256()
        {
            TestKey_ECDsaCng(TestData.ECDsa256Certificate, TestData.ECDsaCng256PublicKey);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void TestKey_ECDsaCng384()
        {
            TestKey_ECDsaCng(TestData.ECDsa384Certificate, TestData.ECDsaCng384PublicKey);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void TestKey_ECDsaCng521()
        {
            TestKey_ECDsaCng(TestData.ECDsa521Certificate, TestData.ECDsaCng521PublicKey);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public static void TestKey_BrainpoolP160r1()
        {
            if (PlatformDetection.WindowsVersion >= 10)
            {
                TestKey_ECDsaCng(TestData.ECDsabrainpoolP160r1_CertificatePemBytes, TestData.ECDsabrainpoolP160r1_PublicKey);
            }
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfo_RSA()
        {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(TestData.RsaPkcs8PublicKey);
            PublicKey key = new PublicKey(rsa);

            Span<byte> algSpki = rsa.ExportSubjectPublicKeyInfo();
            Assert.True(algSpki.SequenceEqual(key.ExportSubjectPublicKeyInfo()), "SequenceEquals(ExportSubjectPublicKeyInfo)");

            // Just right
            Assert.True(key.TryExportSubjectPublicKeyInfo(algSpki, out int written), nameof(key.TryExportSubjectPublicKeyInfo));
            Assert.Equal(algSpki.Length, written);

            // Too small
            Assert.False(key.TryExportSubjectPublicKeyInfo(algSpki.Slice(1), out written), nameof(key.TryExportSubjectPublicKeyInfo));
            Assert.Equal(0, written);
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void ExportSubjectPublicKeyInfo_DSA()
        {
            using DSA dsa = DSA.Create();
            dsa.ImportFromPem(TestData.DsaPkcs8PublicKey);
            PublicKey key = new PublicKey(dsa);

            Span<byte> algSpki = dsa.ExportSubjectPublicKeyInfo();
            Assert.True(algSpki.SequenceEqual(key.ExportSubjectPublicKeyInfo()), "SequenceEquals(ExportSubjectPublicKeyInfo)");

            // Just right
            Assert.True(key.TryExportSubjectPublicKeyInfo(algSpki, out int written), nameof(key.TryExportSubjectPublicKeyInfo));
            Assert.Equal(algSpki.Length, written);

            // Too small
            Assert.False(key.TryExportSubjectPublicKeyInfo(algSpki.Slice(1), out written), nameof(key.TryExportSubjectPublicKeyInfo));
            Assert.Equal(0, written);
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfo_ECDSA()
        {
            using ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(TestData.ECDsaPkcs8PublicKey);
            PublicKey key = new PublicKey(ecdsa);

            Span<byte> algSpki = ecdsa.ExportSubjectPublicKeyInfo();
            Assert.True(algSpki.SequenceEqual(key.ExportSubjectPublicKeyInfo()), "SequenceEquals(ExportSubjectPublicKeyInfo)");

            // Just right
            Assert.True(key.TryExportSubjectPublicKeyInfo(algSpki, out int written), nameof(key.TryExportSubjectPublicKeyInfo));
            Assert.Equal(algSpki.Length, written);

            // Too small
            Assert.False(key.TryExportSubjectPublicKeyInfo(algSpki.Slice(1), out written), nameof(key.TryExportSubjectPublicKeyInfo));
            Assert.Equal(0, written);
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfo_ECDH()
        {
            using ECDiffieHellman ecdh = ECDiffieHellman.Create();
            ecdh.ImportFromPem(TestData.EcDhPkcs8Key);
            PublicKey key = new PublicKey(ecdh);

            Span<byte> algSpki = ecdh.ExportSubjectPublicKeyInfo();
            Assert.True(algSpki.SequenceEqual(key.ExportSubjectPublicKeyInfo()), "SequenceEquals(ExportSubjectPublicKeyInfo)");

            // Just right
            Assert.True(key.TryExportSubjectPublicKeyInfo(algSpki, out int written), nameof(key.TryExportSubjectPublicKeyInfo));
            Assert.Equal(algSpki.Length, written);

            // Too small
            Assert.False(key.TryExportSubjectPublicKeyInfo(algSpki.Slice(1), out written), nameof(key.TryExportSubjectPublicKeyInfo));
            Assert.Equal(0, written);
        }

        [Fact]
        public static void CreateFromSubjectPublicKeyInfo_Roundtrip_RSA()
        {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(TestData.RsaPkcs8PublicKey);
            byte[] spki = rsa.ExportSubjectPublicKeyInfo();

            PublicKey key = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out int read);

            Assert.IsAssignableFrom<RSA>(key.Key);
            Assert.Equal("1.2.840.113549.1.1.1", key.Oid.Value);
            Assert.Equal(spki, key.ExportSubjectPublicKeyInfo());
            Assert.Equal(spki.Length, read);
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void CreateFromSubjectPublicKeyInfo_Roundtrip_DSA()
        {
            using DSA dsa = DSA.Create();
            dsa.ImportFromPem(TestData.DsaPkcs8PublicKey);
            byte[] spki = dsa.ExportSubjectPublicKeyInfo();

            PublicKey key = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out int read);

            Assert.IsAssignableFrom<DSA>(key.Key);
            Assert.Equal("1.2.840.10040.4.1", key.Oid.Value);
            Assert.Equal(spki, key.ExportSubjectPublicKeyInfo());
            Assert.Equal(spki.Length, read);
        }

        [Fact]
        public static void CreateFromSubjectPublicKeyInfo_Roundtrip_ECDSA()
        {
            using ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(TestData.ECDsaPkcs8PublicKey);
            byte[] spki = ecdsa.ExportSubjectPublicKeyInfo();

            PublicKey key = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out int read);

            Assert.Throws<NotSupportedException>(() => key.Key);
            Assert.Equal("1.2.840.10045.2.1", key.Oid.Value);
            Assert.Equal(spki, key.ExportSubjectPublicKeyInfo());
            Assert.Equal(spki.Length, read);
        }

        [Fact]
        public static void CreateFromSubjectPublicKeyInfo_Roundtrip_ECDH()
        {
            using ECDiffieHellman ecdh = ECDiffieHellman.Create();
            ecdh.ImportFromPem(TestData.EcDhPkcs8PublicKey);
            byte[] spki = ecdh.ExportSubjectPublicKeyInfo();

            PublicKey key = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out int read);

            Assert.Throws<NotSupportedException>(() => key.Key);
            Assert.Equal("1.2.840.10045.2.1", key.Oid.Value);
            Assert.Equal(spki, key.ExportSubjectPublicKeyInfo());
            Assert.Equal(spki.Length, read);
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void CreateFromSubjectPublicKeyInfo_Roundtrip_DSA_InvalidKey()
        {
            // The DSA key is invalid here, but we should be able to round-trip the
            // parameters as-is.
            byte[] spki = Convert.FromHexString(
                "301B301306072A8648CE3804013008020100020300FFFF030400020103");

            PublicKey key = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out int read);

            Assert.ThrowsAny<CryptographicException>(() => key.Key);
            Assert.Equal("1.2.840.10040.4.1", key.Oid.Value);
            Assert.Equal(spki, key.ExportSubjectPublicKeyInfo());
            Assert.Equal(spki.Length, read);
        }

        [Fact]
        public static void CreateFromSubjectPublicKeyInfo_BadEncoding()
        {
            Assert.Throws<CryptographicException>(() =>
                PublicKey.CreateFromSubjectPublicKeyInfo(new byte[] { 0xFF }, out _));
        }

        [Fact]
        public static void CreateFromSubjectPublicKeyInfo_AnyAlgorithm()
        {
            byte[] spki = TestData.GostR3410SubjectPublicKeyInfo;
            PublicKey key = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out int read);

            Assert.Throws<NotSupportedException>(() => key.Key);
            Assert.Equal("1.2.643.2.2.19", key.Oid.Value);
            Assert.Equal(spki, key.ExportSubjectPublicKeyInfo());
            Assert.Equal(spki.Length, read);
        }

        [Fact]
        public static void GetPublicKey_NullForDifferentAlgorithm()
        {
            byte[] spki = TestData.GostR3410SubjectPublicKeyInfo;
            PublicKey key = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out _);

            Assert.Null(key.GetRSAPublicKey());
            Assert.Null(key.GetECDsaPublicKey());
            Assert.Null(key.GetECDiffieHellmanPublicKey());
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void GetDSAPublicKey_NullForDifferentAlgorithm()
        {
            byte[] spki = TestData.GostR3410SubjectPublicKeyInfo;
            PublicKey key = PublicKey.CreateFromSubjectPublicKeyInfo(spki, out _);

            Assert.Null(key.GetDSAPublicKey());
        }

        [Fact]
        public static void GetRSAPublicKey_ReturnsRsaKey()
        {
            PublicKey key = GetTestRsaKey();

            using (RSA rsa = key.GetRSAPublicKey())
            {
                Assert.NotNull(rsa);
                Assert.Equal(rsa.ExportSubjectPublicKeyInfo(), key.ExportSubjectPublicKeyInfo());
            }
        }

        [Fact]
        public static void GetRSAPublicKey_ThrowsForCorruptKey()
        {
            AsnEncodedData badData = new AsnEncodedData(new byte[] { 1, 2, 3, 4 });
            PublicKey key = new PublicKey(GetTestRsaKey().Oid, badData, badData);

            Assert.ThrowsAny<CryptographicException>(() => key.GetRSAPublicKey());
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void GetDSAPublicKey_ReturnsDsaKey()
        {
            PublicKey key = GetTestDsaKey();

            using (DSA dsa = key.GetDSAPublicKey())
            {
                Assert.NotNull(dsa);
                Assert.Equal(dsa.ExportSubjectPublicKeyInfo(), key.ExportSubjectPublicKeyInfo());
            }
        }

        [Fact]
        [SkipOnPlatform(PlatformSupport.MobileAppleCrypto, "DSA is not available")]
        public static void GetDSAPublicKey_ThrowsForCorruptKey()
        {
            AsnEncodedData badData = new AsnEncodedData(new byte[] { 1, 2, 3, 4 });
            PublicKey key = new PublicKey(GetTestDsaKey().Oid, badData, badData);

            Assert.ThrowsAny<CryptographicException>(() => key.GetDSAPublicKey());
        }

        [Fact]
        public static void GetECDsaPublicKey_ReturnsECDsaKey()
        {
            PublicKey key = GetTestECDsaKey();

            using (ECDsa ecdsa = key.GetECDsaPublicKey())
            {
                Assert.NotNull(ecdsa);
                Assert.Equal(ecdsa.ExportSubjectPublicKeyInfo(), key.ExportSubjectPublicKeyInfo());
            }
        }

        [Fact]
        public static void GetECDsaPublicKey_ThrowsForCorruptKey()
        {
            AsnEncodedData badData = new AsnEncodedData(new byte[] { 1, 2, 3, 4 });
            PublicKey key = new PublicKey(GetTestECDsaKey().Oid, badData, badData);

            Assert.ThrowsAny<CryptographicException>(() => key.GetECDsaPublicKey());
        }

        [Fact]
        public static void GetECDiffieHellmanPublicKey_ReturnsECDHKey()
        {
            PublicKey key = GetTestECDHKey();

            using (ECDiffieHellman ecdh = key.GetECDiffieHellmanPublicKey())
            {
                Assert.NotNull(ecdh);
                Assert.Equal(ecdh.ExportSubjectPublicKeyInfo(), key.ExportSubjectPublicKeyInfo());
            }
        }

        [Fact]
        public static void GetECDiffieHellmanPublicKey_ThrowsForCorruptKey()
        {
            AsnEncodedData badData = new AsnEncodedData(new byte[] { 1, 2, 3, 4 });
            PublicKey key = new PublicKey(GetTestECDHKey().Oid, badData, badData);

            Assert.ThrowsAny<CryptographicException>(() => key.GetECDiffieHellmanPublicKey());
        }

        private static void TestKey_ECDsaCng(byte[] certBytes, TestData.ECDsaCngKeyValues expected)
        {
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
        }
    }
}
