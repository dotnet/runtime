// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public static class TripleDESCipherTests
    {
        [Fact]
        public static void TripleDESDefaults()
        {
            using (TripleDES des = TripleDESFactory.Create())
            {
                Assert.Equal(192, des.KeySize);
                Assert.Equal(64, des.BlockSize);
            }
        }

        [Fact]
        public static void TripleDESGenerate128Key()
        {
            using (TripleDES des = TripleDESFactory.Create())
            {
                des.KeySize = 128;
                byte[] key = des.Key;
                Assert.Equal(128, key.Length * 8);
            }
        }

        [Fact]
        public static void TripleDESInvalidKeySizes()
        {
            using (TripleDES des = TripleDESFactory.Create())
            {
                Assert.Throws<CryptographicException>(() => des.KeySize = 128 - des.BlockSize);
                Assert.Throws<CryptographicException>(() => des.KeySize = 192 + des.BlockSize);
            }
        }

        [Theory]
        [InlineData(192, "e56f72478c7479d169d54c0548b744af5b53efb1cdd26037", "c5629363d957054eba793093b83739bb78711db221a82379")]
        [InlineData(128, "1387b981dbb40f34b915c4ed89fd681a740d3b4869c0b575", "c5629363d957054eba793093b83739bb")]
        [InlineData(192, "1387b981dbb40f34b915c4ed89fd681a740d3b4869c0b575", "c5629363d957054eba793093b83739bbc5629363d957054e")]
        public static void TripleDESRoundTripNoneECB(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.None;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "de7d2dddea96b691e979e647dc9d3ca27d7f1ad673ca9570".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "de7d2dddea96b691e979e647dc9d3ca27d7f1ad673ca9570".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "dea36279600f19c602b6ed9bf3ffdac5ebf25c1c470eb61c", "b43eaf0260813fb47c87ae073a146006d359ad04061eb0e6")]
        [InlineData(128, "a25e55381f0cc45541741b9ce6e96b7799aa1e0db70780f7", "b43eaf0260813fb47c87ae073a146006")]
        [InlineData(192, "a25e55381f0cc45541741b9ce6e96b7799aa1e0db70780f7", "b43eaf0260813fb47c87ae073a146006b43eaf0260813fb4")]
        public static void TripleDESRoundTripNoneCBC(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();
            byte[] iv = "5fbc5bc21b8597d8".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.IV = iv;
                alg.Padding = PaddingMode.None;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "79a86903608e133e020e1dc68c9835250c2f17b0ebeed91b".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "149ec32f558b27c7e4151e340d8184f18b4e25d2518f69d9", "9da5b265179d65f634dfc95513f25094411e51bb3be877ef")]
        [InlineData(128, "02ac5db31cfada874f6042c4e92b09175fd08e93a20f936b", "9da5b265179d65f634dfc95513f25094")]
        [InlineData(192, "02ac5db31cfada874f6042c4e92b09175fd08e93a20f936b", "9da5b265179d65f634dfc95513f250949da5b265179d65f6")]
        public static void TripleDESRoundTripZerosECB(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.Zeros;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8000000".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "9da5b265179d65f634dfc95513f25094411e51bb3be877ef")]
        [InlineData(128, "9da5b265179d65f634dfc95513f25094")]
        [InlineData(192, "9da5b265179d65f634dfc95513f250949da5b265179d65f6")]
        public static void TripleDESRoundTripISO10126ECB(int keySize, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.ISO10126;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);

                // the padding data for ISO10126 is made up of random bytes, so we cannot actually test
                // the full encrypted text. We need to strip the padding and then compare
                byte[] decrypted = alg.Decrypt(cipher);

                Assert.Equal<byte>(plainText, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "149ec32f558b27c7e4151e340d8184f1c90f0a499e20fda9", "9da5b265179d65f634dfc95513f25094411e51bb3be877ef")]
        [InlineData(128, "02ac5db31cfada874f6042c4e92b091783620e54a1e75957", "9da5b265179d65f634dfc95513f25094")]
        [InlineData(192, "02ac5db31cfada874f6042c4e92b091783620e54a1e75957", "9da5b265179d65f634dfc95513f250949da5b265179d65f6")]
        public static void TripleDESRoundTripANSIX923ECB(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.ANSIX923;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);

                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                Assert.Equal<byte>(plainText, decrypted);
            }
        }

        [Fact]
        public static void TripleDES_FailureToRoundTrip192Bits_DifferentPadding_ANSIX923_ZerosECB()
        {
            byte[] key = "9da5b265179d65f634dfc95513f25094411e51bb3be877ef".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.Padding = PaddingMode.ANSIX923;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);

                byte[] expectedCipher = "149ec32f558b27c7e4151e340d8184f1c90f0a499e20fda9".HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                alg.Padding = PaddingMode.Zeros;
                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "77a8b2efb45addb38d7ef3aa9e6ab5d71957445ab8".HexToByteArray();

                // They should not decrypt to the same value
                Assert.NotEqual<byte>(plainText, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "65f3dc211876a9daad238aa7d0c7ed7a3662296faf77dff9", "5e970c0d2323d53b28fa3de507d6d20f9f0cd97123398b4d")]
        [InlineData(128, "2f55ff6bd8270f1d68dcb342bb674f914d9e1c0e61017a77", "5e970c0d2323d53b28fa3de507d6d20f")]
        [InlineData(192, "2f55ff6bd8270f1d68dcb342bb674f914d9e1c0e61017a77", "5e970c0d2323d53b28fa3de507d6d20f5e970c0d2323d53b")]
        public static void TripleDESRoundTripZerosCBC(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();
            byte[] iv = "95498b5bf570f4c8".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.IV = iv;
                alg.Padding = PaddingMode.Zeros;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "f9e9a1385bf3bd056d6a06eac662736891bd3e6837".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "f9e9a1385bf3bd056d6a06eac662736891bd3e6837000000".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "7b8d982ee0c14821daf1b8cf4e407c2eb328627b696ac36e", "155425f12109cd89378795a4ca337b3264689dca497ba2fa")]
        [InlineData(128, "ce7daa4723c4f880fb44c2809821fc2183b46f0c32084620", "155425f12109cd89378795a4ca337b32")]
        [InlineData(192, "ce7daa4723c4f880fb44c2809821fc2183b46f0c32084620", "155425f12109cd89378795a4ca337b32155425f12109cd89")]
        public static void TripleDESRoundTripPKCS7ECB(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.ECB;

                byte[] plainText = "5bd3c4e16a723a17ac60dd0efdb158e269cddfd0fa".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "5bd3c4e16a723a17ac60dd0efdb158e269cddfd0fa".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(192, "446f57875e107702afde16b57eaf250b87b8110bef29af89", "6b42da08f93e819fbd26fce0785b0eec3d0cb6bfa053c505")]
        [InlineData(128, "ebf995606ceceddf5c90a7302521bc1f6d31f330969cb768", "6b42da08f93e819fbd26fce0785b0eec")]
        [InlineData(192, "ebf995606ceceddf5c90a7302521bc1f6d31f330969cb768", "6b42da08f93e819fbd26fce0785b0eec6b42da08f93e819f")]
        public static void TripleDESRoundTripPKCS7CBC(int keySize, string expectedCipherHex, string keyHex)
        {
            byte[] key = keyHex.HexToByteArray();
            byte[] iv = "8fc67ce5e7f28cde".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                Assert.Equal(keySize, alg.KeySize);

                alg.IV = iv;
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.CBC;

                byte[] plainText = "e867f915e275eab27d6951165d26dec6dd0acafcfc".HexToByteArray();
                byte[] cipher = alg.Encrypt(plainText);
                byte[] expectedCipher = expectedCipherHex.HexToByteArray();
                Assert.Equal<byte>(expectedCipher, cipher);

                byte[] decrypted = alg.Decrypt(cipher);
                byte[] expectedDecrypted = "e867f915e275eab27d6951165d26dec6dd0acafcfc".HexToByteArray();
                Assert.Equal<byte>(expectedDecrypted, decrypted);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void EncryptWithLargeOutputBuffer(bool blockAlignedOutput)
        {
            using (TripleDES alg = TripleDESFactory.Create())
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
            using (TripleDES alg = TripleDESFactory.Create())
            using (ICryptoTransform xform = encrypt ? alg.CreateEncryptor() : alg.CreateDecryptor())
            {
                // 1 block, plus maybe three bytes
                int outputPadding = blockAlignedOutput ? 0 : 3;
                byte[] output = new byte[alg.BlockSize / 8 + outputPadding];
                // 3 blocks of 0x00
                byte[] input = new byte[3 * (alg.BlockSize / 8)];

                Type exceptionType = typeof(ArgumentOutOfRangeException);

                // TripleDESCryptoServiceProvider doesn't throw the ArgumentOutOfRangeException,
                // giving a CryptographicException when CAPI reports the destination too small.
                if (PlatformDetection.IsFullFramework)
                {
                    exceptionType = typeof(CryptographicException);
                }

                Assert.Throws(
                    exceptionType,
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
            byte[] key = "0123456789ABCDEFFEDCBA9876543210ABCDEF0123456789".HexToByteArray();
            byte[] iv = "0123456789ABCDEF".HexToByteArray();
            byte[] outputBytes = new byte[iv.Length * 2 + outputPadding];
            byte[] input = "A61C8F1D393202E1E3C71DCEAB9B08DB".HexToByteArray();
            int outputOffset = 0;

            using (TripleDES alg = TripleDESFactory.Create())
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
