// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public static class RC2CipherTests
    {
        // These are the expected output of many decryptions. Changing these values requires re-generating test input.
        private static readonly string s_multiBlockString = new ASCIIEncoding().GetBytes(
            "This is a sentence that is longer than a block, it ensures that multi-block functions work.").ByteArrayToHex();
        private static readonly string s_multiBlockString_8 = new ASCIIEncoding().GetBytes(
            "This is a sentence that is longer than a block,but exactly an even block multiplier of 8").ByteArrayToHex();
        private static readonly string s_multiBlockStringPaddedZeros =
            "5468697320697320612073656E74656E63652074686174206973206C6F6E676572207468616E206120626C6F636B2C20" +
            "697420656E73757265732074686174206D756C74692D626C6F636B2066756E6374696F6E7320776F726B2E0000000000";

        private static readonly string s_randomKey_64 = "87FF0737F868378F";
        private static readonly string s_randomIv_64 = "E531E789E3E1BB6F";

        public static IEnumerable<object[]> RC2TestData
        {
            get
            {
                // RFC 2268 test
                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.None,
                    "3000000000000000",
                    null,
                    "1000000000000001",
                    null,
                    "30649EDF9BE7D2C2"
                };

                // RFC 2268 test
                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.None,
                    "FFFFFFFFFFFFFFFF",
                    null,
                    "FFFFFFFFFFFFFFFF",
                    null,
                    "278B27E42E2F0D49"
                };

                // RFC 2268 test
                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.None,
                    "88bca90e90875a7f0f79c384627bafb2",
                    null,
                    "0000000000000000",
                    null,
                    "2269552ab0f85ca6"
                };

                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.None,
                    s_randomKey_64,
                    null,
                    s_multiBlockString_8,
                    null,
                    "F6DF2E83811D6CB0C8A5830069D16F6A51C985D7003852539051FABC3C6EA7CF46BD3DBD5527003A13BC850E32BB598F" +
                    "1AC96E96401EBBCDAEEF21D6C05B8DF2637B938CFDB8814B3CC47E30640BD0396B2AC6D7D9977499"
                };

                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.PKCS7,
                    s_randomKey_64,
                    null,
                    s_multiBlockString,
                    null,
                    "F6DF2E83811D6CB0C8A5830069D16F6A51C985D7003852539051FABC3C6EA7CF46BD3DBD5527003A789B76CBE4D40A73" +
                    "620F04ED9F0AA1AEC7FEC90E7934F69E0568F6DF1F38B2198821D0A771D68A3F8220C8822E387721AEB21E183555CE07"
                };

                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.Zeros,
                    s_randomKey_64,
                    null,
                    s_multiBlockString,
                    s_multiBlockStringPaddedZeros,
                    "F6DF2E83811D6CB0C8A5830069D16F6A51C985D7003852539051FABC3C6EA7CF46BD3DBD5527003A789B76CBE4D40A73" +
                    "620F04ED9F0AA1AEC7FEC90E7934F69E0568F6DF1F38B2198821D0A771D68A3F8220C8822E387721C669B2B62A6BF492"
                };

                yield return new object[]
                {
                    CipherMode.CBC,
                    PaddingMode.None,
                    s_randomKey_64,
                    s_randomIv_64,
                    s_multiBlockString_8,
                    null,
                    "85B5D998F35ECD98DB886798170F64BA2DBA4FE902791CDE900EEB0B35728FEE35FB6CADC41DF67F6056044F15B5C7ED" +
                    "4FAB086053D7DC458C206145AE9655F1590C590FBDE76365FA488CADBCDA67B325A35E7CCBC1B9A1"
                };

                yield return new object[]
                {
                    CipherMode.CBC,
                    PaddingMode.PKCS7,
                    s_randomKey_64,
                    s_randomIv_64,
                    s_multiBlockString,
                    null,
                    "85B5D998F35ECD98DB886798170F64BA2DBA4FE902791CDE900EEB0B35728FEE35FB6CADC41DF67FBB691B45D92B876A" +
                    "13FD18229E5ACB797D21D7B257520910360E00FEECDE3433FDC6F15233AE6B5CAC01289AC8B57A9A6B5DA734C2E7E733"
                };

                yield return new object[]
                {
                    CipherMode.CBC,
                    PaddingMode.Zeros,
                    s_randomKey_64,
                    s_randomIv_64,
                    s_multiBlockString,
                    s_multiBlockStringPaddedZeros,
                    "85B5D998F35ECD98DB886798170F64BA2DBA4FE902791CDE900EEB0B35728FEE35FB6CADC41DF67FBB691B45D92B876A" +
                    "13FD18229E5ACB797D21D7B257520910360E00FEECDE3433FDC6F15233AE6B5CAC01289AC8B57A9A6A1BB012ED20DADA"
                };

            }
        }

        [ActiveIssue(12926)]
        [Theory, MemberData(nameof(RC2TestData))]
        public static void RC2RoundTrip(CipherMode cipherMode, PaddingMode paddingMode, string key, string iv, string textHex, string expectedDecrypted, string expectedEncrypted)
        {
            byte[] expectedDecryptedBytes = expectedDecrypted == null ? textHex.HexToByteArray() : expectedDecrypted.HexToByteArray();
            byte[] expectedEncryptedBytes = expectedEncrypted.HexToByteArray();
            byte[] keyBytes = key.HexToByteArray();

            using (RC2 alg = RC2Factory.Create())
            {
                alg.Key = keyBytes;
                alg.Padding = paddingMode;
                alg.Mode = cipherMode;
                if (iv != null)
                    alg.IV = iv.HexToByteArray();

                byte[] cipher = alg.Encrypt(textHex.HexToByteArray());
                Assert.Equal<byte>(expectedEncryptedBytes, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                Assert.Equal<byte>(expectedDecryptedBytes, decrypted);
            }
        }

        [ActiveIssue(12926)]
        [Fact]
        public static void RC2ReuseEncryptorDecryptor()
        {
            using (RC2 alg = RC2Factory.Create())
            {
                alg.Key = s_randomKey_64.HexToByteArray();
                alg.IV = s_randomIv_64.HexToByteArray();
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.CBC;

                using (ICryptoTransform encryptor = alg.CreateEncryptor())
                using (ICryptoTransform decryptor = alg.CreateDecryptor())
                {
                    for (int i = 0; i < 2; i++)
                    {
                        byte[] plainText1 = s_multiBlockString.HexToByteArray();
                        byte[] cipher1 = encryptor.Transform(plainText1);
                        byte[] expectedCipher1 = (
                            "85B5D998F35ECD98DB886798170F64BA2DBA4FE902791CDE900EEB0B35728FEE35FB6CADC41DF67FBB691B45D92B876A" +
                            "13FD18229E5ACB797D21D7B257520910360E00FEECDE3433FDC6F15233AE6B5CAC01289AC8B57A9A6B5DA734C2E7E733").HexToByteArray();
                        Assert.Equal<byte>(expectedCipher1, cipher1);

                        byte[] decrypted1 = decryptor.Transform(cipher1);
                        byte[] expectedDecrypted1 = s_multiBlockString.HexToByteArray();
                        Assert.Equal<byte>(expectedDecrypted1, decrypted1);

                        byte[] plainText2 = s_multiBlockString_8.HexToByteArray();
                        byte[] cipher2 = encryptor.Transform(plainText2);
                        byte[] expectedCipher2 = (
                            "85B5D998F35ECD98DB886798170F64BA2DBA4FE902791CDE900EEB0B35728FEE35FB6CADC41DF67F6056044F15B5C7ED" +
                            "4FAB086053D7DC458C206145AE9655F1590C590FBDE76365FA488CADBCDA67B325A35E7CCBC1B9A15E5EBE2879C7AEC2").HexToByteArray();
                        Assert.Equal<byte>(expectedCipher2, cipher2);

                        byte[] decrypted2 = decryptor.Transform(cipher2);
                        byte[] expectedDecrypted2 = s_multiBlockString_8.HexToByteArray();
                        Assert.Equal<byte>(expectedDecrypted2, decrypted2);
                    }
                }
            }
        }

        [ActiveIssue(12926)]
        [Fact]
        public static void RC2ExplicitEncryptorDecryptor_WithIV()
        {
            using (RC2 alg = RC2Factory.Create())
            {
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.CBC;
                using (ICryptoTransform encryptor = alg.CreateEncryptor(s_randomKey_64.HexToByteArray(), s_randomIv_64.HexToByteArray()))
                {
                    byte[] plainText1 = s_multiBlockString.HexToByteArray();
                    byte[] cipher1 = encryptor.Transform(plainText1);
                    byte[] expectedCipher1 = (
                        "85B5D998F35ECD98DB886798170F64BA2DBA4FE902791CDE900EEB0B35728FEE35FB6CADC41DF67FBB691B45D92B876A" +
                        "13FD18229E5ACB797D21D7B257520910360E00FEECDE3433FDC6F15233AE6B5CAC01289AC8B57A9A6B5DA734C2E7E733").HexToByteArray();
                    Assert.Equal<byte>(expectedCipher1, cipher1);
                }
            }
        }

        [ActiveIssue(12926)]
        [Fact]
        public static void RC2ExplicitEncryptorDecryptor_NoIV()
        {
            using (RC2 alg = RC2Factory.Create())
            {
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.ECB;
                using (ICryptoTransform encryptor = alg.CreateEncryptor(s_randomKey_64.HexToByteArray(), null))
                {
                    byte[] plainText1 = s_multiBlockString.HexToByteArray();
                    byte[] cipher1 = encryptor.Transform(plainText1);
                    byte[] expectedCipher1 = (
                        "F6DF2E83811D6CB0C8A5830069D16F6A51C985D7003852539051FABC3C6EA7CF46BD3DBD5527003A789B76CBE4D40A73" +
                        "620F04ED9F0AA1AEC7FEC90E7934F69E0568F6DF1F38B2198821D0A771D68A3F8220C8822E387721AEB21E183555CE07").HexToByteArray();
                    Assert.Equal<byte>(expectedCipher1, cipher1);
                }
            }
        }
    }
}
