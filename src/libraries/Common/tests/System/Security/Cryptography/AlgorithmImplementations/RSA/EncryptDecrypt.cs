// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class EncryptDecrypt_Array : EncryptDecrypt
    {
        protected override byte[] Encrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding) =>
            rsa.Encrypt(data, padding);
        protected override byte[] Decrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding) =>
            rsa.Decrypt(data, padding);

        [Fact]
        public void NullArray_Throws()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("data", () => rsa.Encrypt(null, RSAEncryptionPadding.OaepSHA1));
                AssertExtensions.Throws<ArgumentNullException>("data", () => rsa.Decrypt(null, RSAEncryptionPadding.OaepSHA1));
            }
        }
    }

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class EncryptDecrypt
    {
        public static bool SupportsSha2Oaep => RSAFactory.SupportsSha2Oaep;

        protected abstract byte[] Encrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding);
        protected abstract byte[] Decrypt(RSA rsa, byte[] data, RSAEncryptionPadding padding);

        [Fact]
        public void NullPadding_Throws()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>("padding", () => Encrypt(rsa, TestData.HelloBytes, null));
                AssertExtensions.Throws<ArgumentNullException>("padding", () => Decrypt(rsa, TestData.HelloBytes, null));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UseAfterDispose(bool importKey)
        {
            RSA rsa = importKey ? RSAFactory.Create(TestData.RSA2048Params) : RSAFactory.Create(1024);
            byte[] data = TestData.HelloBytes;
            byte[] enc;

            using (rsa)
            {
                enc = Encrypt(rsa, data, RSAEncryptionPadding.Pkcs1);
            }

            Assert.Throws<ObjectDisposedException>(
                () => Decrypt(rsa, enc, RSAEncryptionPadding.Pkcs1));
        }

        [Fact]
        public void DecryptSavedAnswer()
        {
            byte[] cipherBytes =
            {
                0x35, 0x6F, 0x8F, 0x2C, 0x4D, 0x1A, 0xAC, 0x6D,
                0xE7, 0x52, 0xA5, 0xDF, 0x26, 0x54, 0xA6, 0x34,
                0xF5, 0xBB, 0x14, 0x26, 0x1C, 0xE4, 0xDC, 0xA2,
                0xD8, 0x4D, 0x8F, 0x1C, 0x55, 0xD4, 0xC7, 0xA7,
                0xF2, 0x3C, 0x99, 0x77, 0x9F, 0xE4, 0xB7, 0x34,
                0xA6, 0x28, 0xB2, 0xC4, 0xFB, 0x6F, 0x85, 0xCA,
                0x19, 0x21, 0xCA, 0xC1, 0xA7, 0x8D, 0xAE, 0x95,
                0xAB, 0x9B, 0xA9, 0x88, 0x5B, 0x44, 0xC6, 0x9B,
                0x44, 0x26, 0x71, 0x5D, 0x02, 0x3F, 0x43, 0x42,
                0xEF, 0x4E, 0xEE, 0x09, 0x87, 0xEF, 0xCD, 0xCF,
                0xF9, 0x88, 0x99, 0xE8, 0x49, 0xF7, 0x8F, 0x9B,
                0x59, 0x68, 0x20, 0xF3, 0xA7, 0xB2, 0x94, 0xA4,
                0x23, 0x70, 0x83, 0xD9, 0xAC, 0xE7, 0x5E, 0xEE,
                0xE9, 0x7B, 0xE4, 0x4F, 0x73, 0x2E, 0x9B, 0xD8,
                0x2A, 0x75, 0xFB, 0x6C, 0xB9, 0x39, 0x6D, 0x72,
                0x8A, 0x9C, 0xCD, 0x58, 0x1A, 0x27, 0x79, 0x97,
            };

            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA1024Params);
                output = Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public void DecryptWithPublicKey_Fails()
        {
            byte[] cipherBytes =
            {
                0x35, 0x6F, 0x8F, 0x2C, 0x4D, 0x1A, 0xAC, 0x6D,
                0xE7, 0x52, 0xA5, 0xDF, 0x26, 0x54, 0xA6, 0x34,
                0xF5, 0xBB, 0x14, 0x26, 0x1C, 0xE4, 0xDC, 0xA2,
                0xD8, 0x4D, 0x8F, 0x1C, 0x55, 0xD4, 0xC7, 0xA7,
                0xF2, 0x3C, 0x99, 0x77, 0x9F, 0xE4, 0xB7, 0x34,
                0xA6, 0x28, 0xB2, 0xC4, 0xFB, 0x6F, 0x85, 0xCA,
                0x19, 0x21, 0xCA, 0xC1, 0xA7, 0x8D, 0xAE, 0x95,
                0xAB, 0x9B, 0xA9, 0x88, 0x5B, 0x44, 0xC6, 0x9B,
                0x44, 0x26, 0x71, 0x5D, 0x02, 0x3F, 0x43, 0x42,
                0xEF, 0x4E, 0xEE, 0x09, 0x87, 0xEF, 0xCD, 0xCF,
                0xF9, 0x88, 0x99, 0xE8, 0x49, 0xF7, 0x8F, 0x9B,
                0x59, 0x68, 0x20, 0xF3, 0xA7, 0xB2, 0x94, 0xA4,
                0x23, 0x70, 0x83, 0xD9, 0xAC, 0xE7, 0x5E, 0xEE,
                0xE9, 0x7B, 0xE4, 0x4F, 0x73, 0x2E, 0x9B, 0xD8,
                0x2A, 0x75, 0xFB, 0x6C, 0xB9, 0x39, 0x6D, 0x72,
                0x8A, 0x9C, 0xCD, 0x58, 0x1A, 0x27, 0x79, 0x97,
            };

            using (RSA rsa = RSAFactory.Create())
            {
                RSAParameters parameters = TestData.RSA1024Params;
                RSAParameters pubParameters = new RSAParameters
                {
                    Modulus = parameters.Modulus,
                    Exponent = parameters.Exponent,
                };

                rsa.ImportParameters(pubParameters);

                Assert.ThrowsAny<CryptographicException>(
                    () => Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA1));
            }
        }

        [Fact]
        public void DecryptSavedAnswer_OaepSHA256()
        {
            byte[] cipherBytes = (
                "3ED1D2DCFBD1778D1B1F20C2CC4FD364D7236ACB6DBD7109CE9C44F6DA8D47A4" +
                "53C36A3D4E8E87168CD1E5427A6C261F87CDC5A62507AF6127D1C5D274D5EECD" +
                "50DC53C559FC85624D9FB999ADDD9BE5652926440A3DE32CA27554F524C30A7A" +
                "E66215FE725F5998FB2AFD2E1E5F06F2944B61502A27272660A21363F35C3DC8" +
                "8ED072096391B27D27BE5E1775F949A3A5C9C2903794090CE8D9DFE7003A4745" +
                "E7029B0D4C0F6FD28E9886227E05B56D6BB0BA2933126C808EE0D972054A26DB" +
                "2CA97B09967B2B6D7592F2563302111DE2FC42ED442522CD83A1AE9E8C3F0B1A" +
                "9D50A4A89008D2135E0D8BC859F81CEF76166834432B4AE9BAAD1FC08E4C2C70").HexToByteArray();

            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                if (RSAFactory.SupportsSha2Oaep)
                {
                    output = Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA256);
                }
                else
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA256));

                    return;
                }
            }

            Assert.Equal(TestData.RSA2048Params.InverseQ, output);
        }

        [Fact]
        public void DecryptSavedAnswer_OaepSHA384()
        {
            byte[] cipherBytes =
            {
                0x50, 0x71, 0x9F, 0x24, 0x7F, 0x63, 0xB6, 0xF6,
                0xBE, 0xDB, 0x20, 0x5A, 0x79, 0xEB, 0x65, 0x04,
                0x84, 0x96, 0xBF, 0xFA, 0x7E, 0x87, 0x4D, 0x38,
                0x78, 0xA9, 0x9D, 0x13, 0xC7, 0x8F, 0x29, 0x8C,
                0xFE, 0x57, 0x05, 0xE0, 0xC4, 0xD4, 0x20, 0x21,
                0x8E, 0x12, 0xCD, 0xBB, 0xE2, 0x65, 0x78, 0x89,
                0x6D, 0x58, 0x86, 0x3B, 0x30, 0x6E, 0xE3, 0x18,
                0x89, 0xA4, 0xDF, 0x23, 0x47, 0x97, 0x55, 0x57,
                0x07, 0xCD, 0xCA, 0x88, 0xC0, 0x88, 0x07, 0x58,
                0x2D, 0x5D, 0x27, 0x06, 0x30, 0x2F, 0xD1, 0x42,
                0x75, 0x4A, 0x48, 0xB9, 0xAA, 0x93, 0x9E, 0x1A,
                0x3C, 0x9A, 0xD1, 0xCB, 0x09, 0xE2, 0x1A, 0x42,
                0x2B, 0x80, 0xEC, 0x09, 0xD1, 0x4D, 0x5D, 0xB7,
                0x8C, 0x2F, 0x69, 0x66, 0x0E, 0xEE, 0xE8, 0xCF,
                0x13, 0x76, 0xD0, 0xB7, 0x6E, 0x19, 0x22, 0x4D,
                0x50, 0x0B, 0x41, 0xDF, 0x3F, 0xF0, 0x69, 0xAD,
                0x8F, 0xE8, 0x6E, 0xC6, 0xBB, 0x55, 0x12, 0x24,
                0xE0, 0x30, 0x84, 0x75, 0xC5, 0x5C, 0x49, 0xB6,
                0xBC, 0xD2, 0x07, 0x80, 0x53, 0xF0, 0xB3, 0xFA,
                0xDA, 0x73, 0xD8, 0xB5, 0x68, 0xD0, 0xD9, 0x0B,
                0x02, 0xF5, 0x20, 0xAA, 0x81, 0xA0, 0x07, 0xA2,
                0x8A, 0x96, 0xD4, 0xE5, 0x37, 0xD9, 0x72, 0x05,
                0x07, 0x5B, 0xE8, 0xEC, 0x09, 0xCA, 0x92, 0xA6,
                0x63, 0xAC, 0x80, 0xC4, 0xB3, 0xEB, 0x00, 0x59,
                0x0C, 0xF8, 0x84, 0xCF, 0x7E, 0x5F, 0x44, 0x08,
                0x67, 0x08, 0x3D, 0x94, 0xBE, 0xBF, 0xBA, 0x90,
                0xC3, 0xB8, 0xBE, 0x62, 0xB4, 0x13, 0x50, 0x92,
                0x08, 0xB0, 0xA2, 0xA6, 0x9F, 0x61, 0x27, 0xEE,
                0xA7, 0x37, 0xC3, 0x21, 0x31, 0x41, 0xD7, 0x8E,
                0x19, 0xE5, 0x5E, 0x57, 0x69, 0x54, 0xAE, 0x74,
                0x13, 0x93, 0x13, 0xCC, 0x3B, 0x55, 0x0C, 0x4F,
                0x3E, 0xF6, 0x06, 0x78, 0x18, 0x46, 0x8A, 0x23,
            };

            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                if (RSAFactory.SupportsSha2Oaep)
                {
                    output = Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA384);
                }
                else
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA384));

                    return;
                }
            }

            Assert.Equal(TestData.RSA2048Params.DP, output);
        }

        [Fact]
        public void DecryptSavedAnswer_OaepSHA512()
        {
            byte[] cipherBytes = (
                "7F0598C7257433FCDEF3F6C0AE69517D3AA6B4FD67D7F4C25F9A5996D20843AF" +
                "E14C21D35D4D289CE4E720A1C9D998C9F95AFB73E523F5EA4B54D0BAE9B5665C" +
                "B0A5F5719F5466A491FDB5B323F6B741CF7E0C263D1274959AD87B64B789F7EC" +
                "6E52085954B59F7A3EBE6295EB7F168E8DADB49F166B4CB753F0D2774370D3E2" +
                "D5B9F6493D7EEA65AA7BD8867313C13850CB2F2D7CCF46E553BEBDADA6060C14" +
                "CC43AE238410167BC42FDE9DA07D135C0D2DB48537299DC067A808CCBA2B0B0A" +
                "7A741705DA98872A7416610939DE4E2D4C387662ABD74D80E33502AFF1D571DB" +
                "B874CA25CC54CEE69B6252B33BA92119873E0F8B5CCE0496324904A7847D73FB").HexToByteArray();

            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                if (RSAFactory.SupportsSha2Oaep)
                {
                    output = Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA512);
                }
                else
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA512));

                    return;
                }
            }

            Assert.Equal(TestData.RSA1032Parameters.DQ, output);
        }

        [Fact]
        public void DecryptSavedAnswerUnusualExponent()
        {
            byte[] cipherBytes =
            {
                0x55, 0x64, 0x05, 0xF7, 0xBF, 0x99, 0xD8, 0x07,
                0xD0, 0xAC, 0x1B, 0x1B, 0x60, 0x92, 0x57, 0x95,
                0x5D, 0xA4, 0x5B, 0x55, 0x0E, 0x12, 0x90, 0x24,
                0x86, 0x35, 0xEE, 0x6D, 0xB3, 0x46, 0x3A, 0xB0,
                0x3D, 0x67, 0xCF, 0xB3, 0xFA, 0x61, 0xBB, 0x90,
                0x6D, 0x6D, 0xF8, 0x90, 0x5D, 0x67, 0xD1, 0x8F,
                0x99, 0x6C, 0x31, 0xA2, 0x2C, 0x8E, 0x99, 0x7E,
                0x75, 0xC5, 0x26, 0x71, 0xD1, 0xB0, 0xA5, 0x41,
                0x67, 0x19, 0xF7, 0x40, 0x04, 0xBE, 0xB2, 0xC0,
                0x97, 0xFB, 0xF6, 0xD4, 0xEF, 0x48, 0x5B, 0x93,
                0x81, 0xF8, 0xE1, 0x6A, 0x0E, 0xA0, 0x74, 0x6B,
                0x99, 0xC6, 0x23, 0xF5, 0x02, 0xDE, 0x47, 0x49,
                0x1E, 0x9D, 0xAE, 0x55, 0x20, 0xB5, 0xDE, 0xA0,
                0x04, 0x32, 0x37, 0x4B, 0x24, 0xE4, 0x64, 0x1B,
                0x1B, 0x4B, 0xC0, 0xC7, 0x30, 0x08, 0xA6, 0xAE,
                0x50, 0x86, 0x08, 0x34, 0x70, 0xE5, 0xB0, 0x3B,
            };

            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.UnusualExponentParameters);
                output = Decrypt(rsa, cipherBytes, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public void RsaCryptRoundtrip_OaepSHA1() => RsaCryptRoundtrip(RSAEncryptionPadding.OaepSHA1);

        [Fact]
        public void RsaCryptRoundtrip_OaepSHA256() =>
            RsaCryptRoundtrip(RSAEncryptionPadding.OaepSHA256, RSAFactory.SupportsSha2Oaep);

        [Fact]
        public void RsaCryptRoundtrip_OaepSHA384() =>
            RsaCryptRoundtrip(RSAEncryptionPadding.OaepSHA384, RSAFactory.SupportsSha2Oaep);

        [Fact]
        public void RsaCryptRoundtrip_OaepSHA512() =>
            RsaCryptRoundtrip(RSAEncryptionPadding.OaepSHA512, RSAFactory.SupportsSha2Oaep);

        private void RsaCryptRoundtrip(RSAEncryptionPadding paddingMode, bool expectSuccess=true)
        {
            byte[] crypt;
            byte[] output;

            using (RSA rsa = RSAFactory.Create(2048))
            {
                if (!expectSuccess)
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => Encrypt(rsa, TestData.HelloBytes, paddingMode));

                    return;
                }

                crypt = Encrypt(rsa, TestData.HelloBytes, paddingMode);
                output = Decrypt(rsa, crypt, paddingMode);
            }

            Assert.NotEqual(crypt, output);
            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52199", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void RoundtripEmptyArray()
        {
            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                void RoundtripEmpty(RSAEncryptionPadding paddingMode)
                {
                    byte[] encrypted = Encrypt(rsa, Array.Empty<byte>(), paddingMode);
                    byte[] decrypted = Decrypt(rsa, encrypted, paddingMode);

                    Assert.Equal(Array.Empty<byte>(), decrypted);
                }

                RoundtripEmpty(RSAEncryptionPadding.Pkcs1);
                RoundtripEmpty(RSAEncryptionPadding.OaepSHA1);

                if (RSAFactory.SupportsSha2Oaep)
                {
                    RoundtripEmpty(RSAEncryptionPadding.OaepSHA256);
                    RoundtripEmpty(RSAEncryptionPadding.OaepSHA384);
                    RoundtripEmpty(RSAEncryptionPadding.OaepSHA512);
                }
            }
        }

        [Fact]
        public void RsaPkcsEncryptMaxSize()
        {
            RSAParameters rsaParameters = TestData.RSA2048Params;

            using (RSA rsa = RSAFactory.Create(rsaParameters))
            {
                RSAEncryptionPadding paddingMode1 = RSAEncryptionPadding.Pkcs1;
                // The overhead required is 8 + 3 => 11.

                const int Pkcs1Overhead = 11;
                int maxSize = rsaParameters.Modulus.Length - Pkcs1Overhead;

                byte[] data = new byte[maxSize];
                byte[] encrypted = Encrypt(rsa, data, paddingMode1);
                byte[] decrypted = Decrypt(rsa, encrypted, paddingMode1);

                Assert.Equal(data.ByteArrayToHex(), decrypted.ByteArrayToHex());

                data = new byte[maxSize + 1];

                Assert.ThrowsAny<CryptographicException>(
                    () => Encrypt(rsa, data, paddingMode1));
            }
        }

        [Fact]
        public void RsaOaepMaxSize()
        {
            RSAParameters rsaParameters = TestData.RSA2048Params;

            using (RSA rsa = RSAFactory.Create(rsaParameters))
            {
                void Test(RSAEncryptionPadding paddingMode, int hashSizeInBits)
                {
                    // The overhead required is hLen + hLen + 2.
                    int hLen = (hashSizeInBits + 7) / 8;
                    int overhead = hLen + hLen + 2;
                    int maxSize = rsaParameters.Modulus.Length - overhead;

                    byte[] data = new byte[maxSize];
                    byte[] encrypted = Encrypt(rsa, data, paddingMode);
                    byte[] decrypted = Decrypt(rsa, encrypted, paddingMode);

                    Assert.Equal(data.ByteArrayToHex(), decrypted.ByteArrayToHex());

                    data = new byte[maxSize + 1];

                    Assert.ThrowsAny<CryptographicException>(
                        () => Encrypt(rsa, data, paddingMode));
                }

                Test(RSAEncryptionPadding.OaepSHA1, 160);

                if (RSAFactory.SupportsSha2Oaep)
                {
                    Test(RSAEncryptionPadding.OaepSHA256, 256);
                    Test(RSAEncryptionPadding.OaepSHA384, 384);
                    Test(RSAEncryptionPadding.OaepSHA512, 512);
                }
            }
        }

        [Fact]
        public void RsaDecryptOaep_ExpectFailure()
        {
            // This particular byte pattern, when decrypting under OAEP-SHA-2-384 has
            // an 0x01 in the correct range, and y=0, but lHash and lHashPrime do not agree
            byte[] encrypted = (
                "2A1914D11E2F6B9E286DAC9D76F32A008EC31457522CEA058D7C48C85085899F" +
                "E9C2DBD4FCA5FAD936F2B747E0BEF131217F8521FA921DF807A83C1B34DB1547" +
                "8D637EDFED222B6411C80D465332B2EE5208F87D4F8D1736FEBC291E14E77C4B" +
                "75A1F06B5124F225F310BFFA83BCA9F11101BA67A64109C37F52BF00B84FFD9A" +
                "D39282AD2BA9EEADADA0FE38998755B556B152EE8974F2C8158ACFA5F509DD4A" +
                "BFE72218C0DF596DFF02C332F45ECC04280455F5D2666E93A3522BB8B41FC92E" +
                "0176AFB1D3A5AE474B708B882ACA88447046E13D44E5EA8D66421DFC177A683B" +
                "7B395F18886AAFD9CED072079739ED1D390354976D188C50A29AAD58784886E6").HexToByteArray();

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => Decrypt(rsa, encrypted, RSAEncryptionPadding.OaepSHA384));
            }
        }

        [ConditionalFact(nameof(SupportsSha2Oaep))]
        public void RsaDecryptOaepWrongAlgorithm()
        {
            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] data = TestData.HelloBytes;
                byte[] encrypted = Encrypt(rsa, data, RSAEncryptionPadding.OaepSHA256);

                Assert.ThrowsAny<CryptographicException>(
                    () => Decrypt(rsa, encrypted, RSAEncryptionPadding.OaepSHA384));
            }
        }

        [Fact]
        public void RsaDecryptOaepWrongData()
        {
            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] data = TestData.HelloBytes;
                byte[] encrypted = Encrypt(rsa, data, RSAEncryptionPadding.OaepSHA1);
                encrypted[1] ^= 0xFF;

                Assert.ThrowsAny<CryptographicException>(
                    () => Decrypt(rsa, encrypted, RSAEncryptionPadding.OaepSHA1));

                if (RSAFactory.SupportsSha2Oaep)
                {
                    encrypted = Encrypt(rsa, data, RSAEncryptionPadding.OaepSHA256);
                    encrypted[1] ^= 0xFF;

                    Assert.ThrowsAny<CryptographicException>(
                        () => Decrypt(rsa, encrypted, RSAEncryptionPadding.OaepSHA256));
                }
            }
        }

        [Fact]
        public void RsaDecryptPkcs1LeadingZero()
        {
            // The first search for an encrypted value with a leading 0x00 turned up one with
            // two leading zeros.  How fortuitous.
            byte[] encrypted = (
                "0000B81E93CB9BA2B096DAC80ADC0C053D15CAD79A09D3DC154E3E75E0F59AF0" +
                "D0816C0946946A56FEAEDB951A49C3854966C01C47A9F54DE2A050C1625869FE" +
                "02BAD7AA427C42FE79D31267AE8713504CBBBBFA28EED0DF3E9F5BFC12C8A701" +
                "382E92BC50D7E9E9897AEBDDA8005B7906AE1ABAFFD30CF5A8733CAB7264445A" +
                "333730EA31F5F9F120B4B59F689BA529E106DA78340678C3BA2CE46427375A84" +
                "9E86950FC18BD1D6C33508596BAEF0D916F0E29D647C037022753B1E8E44ABCF" +
                "0079CEFA8972F02D05C4204078BD9ADF98571CE5374AB94BF01918F0EA31A815" +
                "59F065A4C3FA0DD0E3086530608CA54387F86F25ED77D46C7576376B64BE3C91").HexToByteArray();

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] decrypted = Decrypt(rsa, encrypted, RSAEncryptionPadding.Pkcs1);
                Assert.Equal(TestData.HelloBytes, decrypted);
            }
        }

        [Fact]
        public void RsaDecryptPkcs1Deficient()
        {
            // This value is gibberish, but it happens to be true that if it is preceded
            // with 0x00 it happens to pass a PKCS1 encryption padding sanity test with the
            // RSA2048Params key.
            //
            // If instead of prepending a 0x00 one appended 0x4B, it would decrypt to "Hello".
            byte[] encrypted = (
                "7EF2A69BBCF5B29A19DF6698B8BAB5EC4D9DF1D8CAA27D7D1BF60D560DB7D79D" +
                "020C85620657F2A32C872EE44DB604FAFFF792A886BEF2E142A2DB0379C5C57D" +
                "D444D2065A7976A6163B4A0D51AEE421B099A8E8A823A917A6E55A4A8E660715" +
                "B9AC53CF37392228B2F7042CCBDA14CA88314FD353EA70AA9899E88771B01C8E" +
                "E0DE35BD342F43809670B056B35A0EB68D370E1489D51AA4780766739887DBC6" +
                "A716FE05773803C43B5040BF29AB33C4567E8986B3C442A7CEFCF46D61E13E54" +
                "85468C0FF3FDC804BDDE60E4310CC45F5196DC75F713581D934FB914661B6B69" +
                "EC3CE2CF469D7CD8727B959B5593F8D38124B0947E7948252BF9A53763877F").HexToByteArray();

            byte[] correctlyPadded = new byte[encrypted.Length + 1];
            Buffer.BlockCopy(encrypted, 0, correctlyPadded, 1, encrypted.Length);

            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] decrypted = Decrypt(rsa, correctlyPadded, RSAEncryptionPadding.Pkcs1);
                Assert.NotNull(decrypted);

                Assert.ThrowsAny<CryptographicException>(
                    () => rsa.Decrypt(encrypted, RSAEncryptionPadding.Pkcs1));
            }
        }

        [Fact]
        public void RsaDecryptPkcs1WrongDataLength()
        {
            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] data = TestData.HelloBytes;

                byte[] encrypted = Encrypt(rsa, data, RSAEncryptionPadding.Pkcs1);
                Array.Resize(ref encrypted, encrypted.Length + 1);

                // Baseline/exempt a .NET Framework difference for RSACng
                if (!PlatformDetection.IsNetFramework ||
                    rsa.GetType().Assembly.GetName().Name != "System.Core")
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => Decrypt(rsa, encrypted, RSAEncryptionPadding.Pkcs1));
                }

                Array.Resize(ref encrypted, encrypted.Length - 2);

                Assert.ThrowsAny<CryptographicException>(
                    () => Decrypt(rsa, encrypted, RSAEncryptionPadding.Pkcs1));
            }
        }

        [Fact]
        public void RsaDecryptOaepWrongDataLength()
        {
            using (RSA rsa = RSAFactory.Create(TestData.RSA2048Params))
            {
                byte[] data = TestData.HelloBytes;

                byte[] encrypted = Encrypt(rsa, data, RSAEncryptionPadding.OaepSHA1);
                Array.Resize(ref encrypted, encrypted.Length + 1);

                if (!PlatformDetection.IsNetFramework)
                {
                    Assert.ThrowsAny<CryptographicException>(
                        () => Decrypt(rsa, encrypted, RSAEncryptionPadding.OaepSHA1));
                }

                Array.Resize(ref encrypted, encrypted.Length - 2);

                Assert.ThrowsAny<CryptographicException>(
                    () => Decrypt(rsa, encrypted, RSAEncryptionPadding.OaepSHA1));

                if (RSAFactory.SupportsSha2Oaep)
                {
                    encrypted = Encrypt(rsa, data, RSAEncryptionPadding.OaepSHA256);
                    Array.Resize(ref encrypted, encrypted.Length + 1);

                    if (!PlatformDetection.IsNetFramework)
                    {
                        Assert.ThrowsAny<CryptographicException>(
                            () => Decrypt(rsa, encrypted, RSAEncryptionPadding.OaepSHA256));
                    }

                    Array.Resize(ref encrypted, encrypted.Length - 2);

                    Assert.ThrowsAny<CryptographicException>(
                        () => Decrypt(rsa, encrypted, RSAEncryptionPadding.OaepSHA256));
                }
            }
        }

        [Fact]
        public void RsaDecryptAfterExport()
        {
            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                byte[] crypt = Encrypt(rsa, TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);

                // Export the key, this should not clear/destroy the key.
                rsa.ExportParameters(true);
                output = Decrypt(rsa, crypt, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public void LargeKeyCryptRoundtrip()
        {
            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                try
                {
                    rsa.ImportParameters(TestData.RSA16384Params);
                }
                catch (CryptographicException)
                {
                    // The key is pretty big, perhaps it was refused.
                    return;
                }

                byte[] crypt = Encrypt(rsa, TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);

                Assert.Equal(rsa.KeySize, crypt.Length * 8);

                output = Decrypt(rsa, crypt, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public void UnusualExponentCryptRoundtrip()
        {
            byte[] crypt;
            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.UnusualExponentParameters);

                crypt = Encrypt(rsa, TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);
                output = Decrypt(rsa, crypt, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.NotEqual(crypt, output);
            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public void NotSupportedValueMethods()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.Throws<NotSupportedException>(() => rsa.DecryptValue(null));
                Assert.Throws<NotSupportedException>(() => rsa.EncryptValue(null));
            }
        }
    }
}
