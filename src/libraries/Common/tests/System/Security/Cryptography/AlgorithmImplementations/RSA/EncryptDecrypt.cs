// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class EncryptDecrypt
    {
        [Fact]
        public static void DecryptSavedAnswer()
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
                output = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public static void DecryptSavedAnswerUnusualExponent()
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
                output = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public static void RsaCryptRoundtrip()
        {
            byte[] crypt;
            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                crypt = rsa.Encrypt(TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);
                output = rsa.Decrypt(crypt, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.NotEqual(crypt, output);
            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public static void RsaDecryptAfterExport()
        {
            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                byte[] crypt = rsa.Encrypt(TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);

                // Export the key, this should not clear/destroy the key.
                RSAParameters ignored = rsa.ExportParameters(true);
                output = rsa.Decrypt(crypt, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public static void LargeKeyCryptRoundtrip()
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

                byte[] crypt = rsa.Encrypt(TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);

                Assert.Equal(rsa.KeySize, crypt.Length * 8);

                output = rsa.Decrypt(crypt, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.Equal(TestData.HelloBytes, output);
        }

        [Fact]
        public static void UnusualExponentCryptRoundtrip()
        {
            byte[] crypt;
            byte[] output;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(TestData.UnusualExponentParameters);

                crypt = rsa.Encrypt(TestData.HelloBytes, RSAEncryptionPadding.OaepSHA1);
                output = rsa.Decrypt(crypt, RSAEncryptionPadding.OaepSHA1);
            }

            Assert.NotEqual(crypt, output);
            Assert.Equal(TestData.HelloBytes, output);
        }
    }
}
