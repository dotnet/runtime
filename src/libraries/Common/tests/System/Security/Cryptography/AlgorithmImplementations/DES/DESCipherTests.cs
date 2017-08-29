// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public static class DesCipherTests
    {
        // These are the expected output of many decryptions. Changing these values requires re-generating test input.
        private static readonly string s_multiBlockString = new ASCIIEncoding().GetBytes(
            "This is a sentence that is longer than a block, it ensures that multi-block functions work.").ByteArrayToHex();
        private static readonly string s_multiBlockStringPaddedZeros =
             "5468697320697320612073656E74656E63652074686174206973206C6F6E676572207468616E206120626C6F636B2C20" +
             "697420656E73757265732074686174206D756C74692D626C6F636B2066756E6374696F6E7320776F726B2E0000000000";
        private static readonly string s_multiBlockString_8 = new ASCIIEncoding().GetBytes(
            "This is a sentence that is longer than a block,but exactly an even block multiplier of 8").ByteArrayToHex();
        private static readonly string s_randomKey_64 = "87FF0737F868378F";
        private static readonly string s_randomIv_64 = "E531E789E3E1BB6F";

        public static IEnumerable<object[]> DesTestData
        {
            get
            {
                // FIPS81 ECB with plaintext "Now is the time for all "
                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.None,
                    "0123456789abcdef",
                    null,
                    "4e6f77206973207468652074696d6520666f7220616c6c20",
                    null,
                    "3fa40e8a984d48156a271787ab8883f9893d51ec4b563b53"
                };

                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.None,
                    s_randomKey_64,
                    null,
                    s_multiBlockString_8,
                    null,
                    "4E42A439ED50C7998CD626B8BE1ECC0A82B985EA772030E87C96BFAE1B97A7666505B8AE96745DE2921F6868897C20F2" +
                    "BC8C7B284FD1E9A0A2E49DDAB7A3978233423377C88177CB2D92475EE4DC1FF9E6DFA135DE648E1B"
                };

                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.PKCS7,
                    s_randomKey_64,
                    null,
                    s_multiBlockString,
                    null,
                    "4E42A439ED50C7998CD626B8BE1ECC0A82B985EA772030E87C96BFAE1B97A7666505B8AE96745DE249C1EC3338BBAD41" +
                    "93A9B792205F345E22D45A9A996F21CE24697E5A45F600E8C6E71FC7114A3E96EC4EACC9F652DEBC679D22DE7141F67F"
                };

                yield return new object[]
                {
                    CipherMode.ECB,
                    PaddingMode.Zeros,
                    s_randomKey_64,
                    null,
                    s_multiBlockString,
                    s_multiBlockStringPaddedZeros,
                    "4E42A439ED50C7998CD626B8BE1ECC0A82B985EA772030E87C96BFAE1B97A7666505B8AE96745DE249C1EC3338BBAD41" +
                    "93A9B792205F345E22D45A9A996F21CE24697E5A45F600E8C6E71FC7114A3E96EC4EACC9F652DEBC471DF9564F29C738"
                };

                // FIPS81 CBC with plaintext "Now is the time for all "
                yield return new object[]
                {
                    CipherMode.CBC,
                    PaddingMode.None,
                    "0123456789abcdef",
                    "1234567890abcdef",
                    "4e6f77206973207468652074696d6520666f7220616c6c20",
                    null,
                    "e5c7cdde872bf27c43e934008c389c0f683788499a7c05f6"
                };

                yield return new object[]
                {
                    CipherMode.CBC,
                    PaddingMode.None,
                    s_randomKey_64,
                    s_randomIv_64,
                    s_multiBlockString_8,
                    null,
                    "7264319AE3C504148CD4A19B4FDC7D2ACCCB0A08D60CBE2B885DCB2C1A86ED9CA51006E33859B03EEB61CF5219D769C1" +
                    "ABF1A1FDE0EF87D3B3C4D567D9C8960DDA55DBE13341928FEF38B938E1F62FAD1D05E355E440E012"
                };

                yield return new object[]
                {
                    CipherMode.CBC,
                    PaddingMode.Zeros,
                    s_randomKey_64,
                    s_randomIv_64,
                    s_multiBlockString,
                    s_multiBlockStringPaddedZeros,
                    "7264319AE3C504148CD4A19B4FDC7D2ACCCB0A08D60CBE2B885DCB2C1A86ED9CA51006E33859B03E00F5B57801EFF745" +
                    "F7A577842461CF39AC143505EC326233E66343A46FEADE9E8456D8AC6A84A1C32E6792857F062400740CB21A333D334D"
                };

                yield return new object[]
                {
                    CipherMode.CBC,
                    PaddingMode.PKCS7,
                    s_randomKey_64,
                    s_randomIv_64,
                    s_multiBlockString,
                    null,
                    "7264319AE3C504148CD4A19B4FDC7D2ACCCB0A08D60CBE2B885DCB2C1A86ED9CA51006E33859B03E00F5B57801EFF745" +
                    "F7A577842461CF39AC143505EC326233E66343A46FEADE9E8456D8AC6A84A1C32E6792857F062400EA9053D17AD3C35D"
                };
            }
        }

        [Theory, MemberData(nameof(DesTestData))]
        public static void DesRoundTrip(CipherMode cipherMode, PaddingMode paddingMode, string key, string iv, string textHex, string expectedDecrypted, string expectedEncrypted)
        {
            byte[] expectedDecryptedBytes = expectedDecrypted == null ? textHex.HexToByteArray() : expectedDecrypted.HexToByteArray();
            byte[] expectedEncryptedBytes = expectedEncrypted.HexToByteArray();
            byte[] keyBytes = key.HexToByteArray();

            using (DES alg = DESFactory.Create())
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

        [Fact]
        public static void DesReuseEncryptorDecryptor()
        {
            using (DES alg = DESFactory.Create())
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
                            "7264319AE3C504148CD4A19B4FDC7D2ACCCB0A08D60CBE2B885DCB2C1A86ED9CA51006E33859B03E00F5B57801EFF745" +
                            "F7A577842461CF39AC143505EC326233E66343A46FEADE9E8456D8AC6A84A1C32E6792857F062400EA9053D17AD3C35D").HexToByteArray();
                        Assert.Equal<byte>(expectedCipher1, cipher1);

                        byte[] decrypted1 = decryptor.Transform(cipher1);
                        byte[] expectedDecrypted1 = s_multiBlockString.HexToByteArray();
                        Assert.Equal<byte>(expectedDecrypted1, decrypted1);

                        byte[] plainText2 = s_multiBlockString_8.HexToByteArray();
                        byte[] cipher2 = encryptor.Transform(plainText2);
                        byte[] expectedCipher2 = (
                            "7264319AE3C504148CD4A19B4FDC7D2ACCCB0A08D60CBE2B885DCB2C1A86ED9CA51006E33859B03EEB61CF5219D769C1" +
                            "ABF1A1FDE0EF87D3B3C4D567D9C8960DDA55DBE13341928FEF38B938E1F62FAD1D05E355E440E012A0FFAB00B7AEE64D").HexToByteArray();
                        Assert.Equal<byte>(expectedCipher2, cipher2);

                        byte[] decrypted2 = decryptor.Transform(cipher2);
                        byte[] expectedDecrypted2 = s_multiBlockString_8.HexToByteArray();
                        Assert.Equal<byte>(expectedDecrypted2, decrypted2);
                    }
                }
            }
        }

        [Fact]
        public static void DesExplicitEncryptorDecryptor_WithIV()
        {
            using (DES alg = DESFactory.Create())
            {
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.CBC;
                using (ICryptoTransform encryptor = alg.CreateEncryptor(s_randomKey_64.HexToByteArray(), s_randomIv_64.HexToByteArray()))
                {
                    byte[] plainText1 = s_multiBlockString.HexToByteArray();
                    byte[] cipher1 = encryptor.Transform(plainText1);
                    byte[] expectedCipher1 = (
                        "7264319AE3C504148CD4A19B4FDC7D2ACCCB0A08D60CBE2B885DCB2C1A86ED9CA51006E33859B03E00F5B57801EFF745" +
                        "F7A577842461CF39AC143505EC326233E66343A46FEADE9E8456D8AC6A84A1C32E6792857F062400EA9053D17AD3C35D").HexToByteArray();
                    Assert.Equal<byte>(expectedCipher1, cipher1);
                }
            }
        }

        [Fact]
        public static void DesExplicitEncryptorDecryptor_NoIV()
        {
            using (DES alg = DESFactory.Create())
            {
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.ECB;
                using (ICryptoTransform encryptor = alg.CreateEncryptor(s_randomKey_64.HexToByteArray(), null))
                {
                    byte[] plainText1 = s_multiBlockString.HexToByteArray();
                    byte[] cipher1 = encryptor.Transform(plainText1);
                    byte[] expectedCipher1 = (
                        "4E42A439ED50C7998CD626B8BE1ECC0A82B985EA772030E87C96BFAE1B97A7666505B8AE96745DE249C1EC3338BBAD41" +
                        "93A9B792205F345E22D45A9A996F21CE24697E5A45F600E8C6E71FC7114A3E96EC4EACC9F652DEBC679D22DE7141F67F").HexToByteArray();
                    Assert.Equal<byte>(expectedCipher1, cipher1);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void EncryptWithLargeOutputBuffer(bool blockAlignedOutput)
        {
            using (DES alg = DESFactory.Create())
            using (ICryptoTransform xform = alg.CreateEncryptor())
            {
                // 8 blocks, plus maybe three bytes
                int outputPadding = blockAlignedOutput ? 0 : 3;
                byte[] output = new byte[alg.BlockSize + outputPadding];
                // 2 blocks of 0x00
                byte[] input = new byte[alg.BlockSize / 4];
                int outputOffset = 0;

                outputOffset += xform.TransformBlock(input, 0, input.Length, output, outputOffset);
                byte[] overflow = xform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Buffer.BlockCopy(overflow, 0, output, outputOffset, overflow.Length);
                outputOffset += overflow.Length;

                Assert.Equal(3 * (alg.BlockSize / 8), outputOffset);
                string outputAsHex = output.ByteArrayToHex();
                Assert.NotEqual(new string('0', outputOffset * 2), outputAsHex.Substring(0, outputOffset * 2));
                Assert.Equal(new string('0', (output.Length - outputOffset) * 2), outputAsHex.Substring(outputOffset * 2));
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public static void TransformWithTooShortOutputBuffer(bool encrypt, bool blockAlignedOutput)
        {
            using (DES alg = DESFactory.Create())
            using (ICryptoTransform xform = encrypt ? alg.CreateEncryptor() : alg.CreateDecryptor())
            {
                // 1 block, plus maybe three bytes
                int outputPadding = blockAlignedOutput ? 0 : 3;
                byte[] output = new byte[alg.BlockSize / 8 + outputPadding];
                // 3 blocks of 0x00
                byte[] input = new byte[3 * (alg.BlockSize / 8)];

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => xform.TransformBlock(input, 0, input.Length, output, 0));
                
                Assert.Equal(new byte[output.Length], output);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void MultipleBlockDecryptTransform(bool blockAlignedOutput)
        {
            const string ExpectedOutput = "This is a test";

            int outputPadding = blockAlignedOutput ? 0 : 3;
            byte[] key = "87FF0737F868378F".HexToByteArray();
            byte[] iv = "0123456789ABCDEF".HexToByteArray();
            byte[] outputBytes = new byte[iv.Length * 2 + outputPadding];
            byte[] input = "CB67F70BA8B50EED2C0691298988865F".HexToByteArray();
            int outputOffset = 0;

            using (DES alg = DESFactory.Create())
            using (ICryptoTransform xform = alg.CreateDecryptor(key, iv))
            {
                Assert.Equal(2 * alg.BlockSize, (outputBytes.Length - outputPadding) * 8);
                outputOffset += xform.TransformBlock(input, 0, input.Length, outputBytes, outputOffset);
                byte[] overflow = xform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Buffer.BlockCopy(overflow, 0, outputBytes, outputOffset, overflow.Length);
                outputOffset += overflow.Length;
            }

            string decrypted = Encoding.ASCII.GetString(outputBytes, 0, outputOffset);
            Assert.Equal(ExpectedOutput, decrypted);
        }
    }
}
