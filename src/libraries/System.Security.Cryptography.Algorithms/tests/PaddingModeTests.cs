// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class PaddingModeTests
    {
        [Theory]
        [InlineData(PaddingMode.Zeros, 0, "", "")] // no block is added in this case!
        [InlineData(PaddingMode.Zeros, 1,    "46785BDE46622B92FF7C8EBB91508A", "1BE8AA365A15D11FC7826B3A10602D09" )]
        [InlineData(PaddingMode.Zeros, 13, "E505A2", "0A2E62938B03E5822EE251117A4CE066")]
        [InlineData(PaddingMode.PKCS7, 1,    "46785BDE46622B92FF7C8EBB91508A", "DB5B7829CCE732BFE609140CF45A8843")]
        [InlineData(PaddingMode.PKCS7, 13, "E505A2", "46785BDE46622B92FF7C8EBB91508A4D")]
        [InlineData(PaddingMode.PKCS7, 16, "", "D5450767BCC31793FE5065251B96B715")]
        [InlineData(PaddingMode.ANSIX923, 1, "46785BDE46622B92FF7C8EBB91508A", "DB5B7829CCE732BFE609140CF45A8843" )]
        [InlineData(PaddingMode.ANSIX923, 13, "E505A2", "43B27D41A9FDE73CA5DB22C0FDA76CB1")]
        [InlineData(PaddingMode.ANSIX923, 16, "", "A3D32A3A9DCA71B6F961F5A8ED7E414F")]
        public static void ValidatePaddingMode_NonISO10126(PaddingMode paddingMode, int expectedPaddingSize, string plainTextStr, string expectedCipherStr)
        {
            Assert.True(paddingMode != PaddingMode.ISO10126, "This tests only non-ISO10126 padding");

            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = plainTextStr.HexToByteArray();
            byte[] expectedCipher = expectedCipherStr == null ? Array.Empty<byte>() : expectedCipherStr.HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = paddingMode;

                byte[] cipher = a.Encrypt(plainText);

                Assert.Equal(expectedCipherStr, cipher.ByteArrayToHex());

                // decrypt it with PaddingMode.None so that we can inspect the padding manually
                a.Padding = PaddingMode.None;
                byte[] decrypted = a.Decrypt(cipher);
                ValidatePadding(decrypted, paddingMode, expectedPaddingSize);
            }
        }

        [Theory]
        [InlineData(1, "46785BDE46622B92FF7C8EBB91508A")]
        [InlineData(13, "E505A2")]
        [InlineData(16, "")]
        public static void ValidatePaddingMode_ISO10126(int expectedPaddingSize, string plainTextStr)
        {
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = plainTextStr.HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.ISO10126;

                // for ISO10126 we are going to encrypt it twice and assert that the ciphers produced are going to be different
                byte[] cipher = a.Encrypt(plainText);
                byte[] secondCipher = a.Encrypt(plainText);

                // decrypt it with PaddingMode.None so that we can inspect the padding manually
                a.Padding = PaddingMode.None;
                byte[] decrypted = a.Decrypt(cipher);

                if (expectedPaddingSize >= 5)
                {
                    byte[] secondDecrypted = a.Decrypt(secondCipher);

                    // after we decrypted, the two ciphers are going to be different
                    Assert.NotEqual(decrypted.ByteArrayToHex(), secondDecrypted.ByteArrayToHex());
                }

                ValidatePadding(decrypted, PaddingMode.ISO10126, expectedPaddingSize);
            }
        }        

        private static void ValidatePadding(byte[] buffer, PaddingMode paddingMode, int expectedPaddingSize)
        {
            switch (paddingMode)
            {
                case PaddingMode.PKCS7:
                    ValidatePKCS7Padding(buffer, expectedPaddingSize);
                    break;
                case PaddingMode.ANSIX923:
                    ValidateANSIX923Padding(buffer, expectedPaddingSize);
                    break;
                case PaddingMode.ISO10126:
                    ValidateISO10126Padding(buffer, expectedPaddingSize);
                    break;
                case PaddingMode.Zeros:
                    ValidateZerosPadding(buffer, expectedPaddingSize);
                    break;
                case PaddingMode.None:
                    break;
                default:
                    break;
            }
        }

        private static void ValidateZerosPadding(byte[] buffer, int expectedPaddingSize)
        {
            for (int i = buffer.Length - 1; i > buffer.Length - 1 - expectedPaddingSize; i--)
            {
                Assert.Equal(0, buffer[i]);
            }
        }

        private static void ValidatePKCS7Padding(byte[] buffer, int expectedPaddingSize)
        {
            for (int i = buffer.Length - 1; i > buffer.Length - 1 - expectedPaddingSize; i--)
            {
                Assert.Equal(expectedPaddingSize, buffer[i]);
            }
        }

        private static void ValidateANSIX923Padding(byte[] buffer, int expectedPaddingSize)
        {
            Assert.Equal(buffer[buffer.Length - 1], expectedPaddingSize);

            for (int i = buffer.Length - expectedPaddingSize; i < buffer.Length - 1; i++)
            {
                Assert.Equal(0, buffer[i]);
            }
        }

        private static void ValidateISO10126Padding(byte[] buffer, int expectedPaddingSize)
        {
            // there is nothing else to validate as all the other padding bytes are random.
            Assert.Equal(buffer[buffer.Length - 1], expectedPaddingSize);
        }
    }
}
