// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public static class AesCornerTests
    {
        [Fact]
        public static void EncryptorReusability()
        {
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "f238882f6530ae9191c294868feed0b0df4058b322377dec14690c3b6bbf6ad1dd5b7c063a28e2cca2a6dce8cc2e668ea6ce80cee4c1a1a955ff46c530f3801b".HexToByteArray();
            // CBC, Padding.None
            byte[] expectedCipher = "7c6e1bcd3c30d2fb2d92e3346048307dc6719a6b96a945b4d987af09469ec68f5ca535fab7f596fffa80f7cfaeb26eefaf8d4ca8be190393b2569249d673f042".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.None;

                ICryptoTransform encryptor = a.CreateEncryptor();
                Assert.True(encryptor.CanReuseTransform);

                for (int i = 0; i < 4; i++)
                {
                    byte[] cipher = encryptor.Transform(plainText, 1);
                    Assert.Equal<byte>(expectedCipher, cipher);
                }
            }
        }

        [Fact]
        public static void TransformStateSeparation()
        {
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "f238882f6530ae9191c294868feed0b0df4058b322377dec14690c3b6bbf6ad1dd5b7c063a28e2cca2a6dce8cc2e668ea6ce80cee4c1a1a955ff46c530f3801b".HexToByteArray();
            // CBC, Padding.None
            byte[] cipher = "7c6e1bcd3c30d2fb2d92e3346048307dc6719a6b96a945b4d987af09469ec68f5ca535fab7f596fffa80f7cfaeb26eefaf8d4ca8be190393b2569249d673f042".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.None;

                // To ensure that each ICryptoTransform maintains an independent encryption state, we'll create two encryptors and two decryptors. 
                // Then we'll feed them one block each in an interleaved fashion. At the end, they'd better still come up with the correct result.

                MemoryStream plain1 = new MemoryStream(plainText);
                MemoryStream plain2 = new MemoryStream(plainText);
                MemoryStream cipher1 = new MemoryStream(cipher);
                MemoryStream cipher2 = new MemoryStream(cipher);

                ICryptoTransform encryptor1 = a.CreateEncryptor();
                ICryptoTransform encryptor2 = a.CreateEncryptor();
                ICryptoTransform decryptor1 = a.CreateDecryptor();
                ICryptoTransform decryptor2 = a.CreateDecryptor();

                List<byte> encryptionCollector1 = new List<byte>();
                List<byte> encryptionCollector2 = new List<byte>();
                List<byte> decryptionCollector1 = new List<byte>();
                List<byte> decryptionCollector2 = new List<byte>();

                int blockSize = a.BlockSize / 8;

                encryptionCollector1.Collect(encryptor1, plain1, 1 * blockSize);
                encryptionCollector2.Collect(encryptor2, plain2, 1 * blockSize);
                encryptionCollector1.Collect(encryptor1, plain1, 1 * blockSize);
                decryptionCollector1.Collect(decryptor1, cipher1, 1 * blockSize);
                decryptionCollector2.Collect(decryptor2, cipher2, 1 * blockSize);
                decryptionCollector2.Collect(decryptor2, cipher2, 1 * blockSize);
                encryptionCollector1.Collect(encryptor1, plain1, 1 * blockSize);
                decryptionCollector1.Collect(decryptor1, cipher1, 1 * blockSize);
                decryptionCollector2.Collect(decryptor2, cipher2, 1 * blockSize);
                decryptionCollector2.Collect(decryptor2, cipher2, 1 * blockSize);
                encryptionCollector2.Collect(encryptor2, plain2, 1 * blockSize);
                decryptionCollector1.Collect(decryptor1, cipher1, 1 * blockSize);
                decryptionCollector2.AddRange(decryptor2.TransformFinalBlock(new byte[0], 0, 0));
                decryptionCollector1.Collect(decryptor1, cipher1, 1 * blockSize);
                encryptionCollector2.Collect(encryptor2, plain2, 1 * blockSize);
                decryptionCollector1.AddRange(decryptor1.TransformFinalBlock(new byte[0], 0, 0));
                encryptionCollector1.Collect(encryptor1, plain1, 1 * blockSize);
                encryptionCollector1.AddRange(encryptor1.TransformFinalBlock(new byte[0], 0, 0));
                encryptionCollector2.Collect(encryptor2, plain2, 1 * blockSize);
                encryptionCollector2.AddRange(encryptor2.TransformFinalBlock(new byte[0], 0, 0));

                Assert.Equal<byte>(cipher, encryptionCollector1.ToArray());
                Assert.Equal<byte>(cipher, encryptionCollector2.ToArray());
                Assert.Equal<byte>(plainText, decryptionCollector1.ToArray());
                Assert.Equal<byte>(plainText, decryptionCollector2.ToArray());
            }
        }

        private static void Collect(this List<byte> l, ICryptoTransform transform, Stream input, int count)
        {
            byte[] buffer = new byte[count];
            int numRead = input.Read(buffer, 0, count);
            Assert.Equal(count, numRead);
            byte[] buffer2 = new byte[count];
            int numBytesWritten = transform.TransformBlock(buffer, 0, count, buffer2, 0);
            Array.Resize(ref buffer2, numBytesWritten);
            l.AddRange(buffer2);
        }

        [Fact]
        public static void MultipleBlockTransformNoPad()
        {
            // Ensure that multiple blocks can be transformed with one call (the no padding code path)

            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "f238882f6530ae9191c294868feed0b0df4058b322377dec14690c3b6bbf6ad1dd5b7c063a28e2cca2a6dce8cc2e668ea6ce80cee4c1a1a955ff46c530f3801b".HexToByteArray();
            // CBC, Padding.None
            byte[] expectedCipher = "7c6e1bcd3c30d2fb2d92e3346048307dc6719a6b96a945b4d987af09469ec68f5ca535fab7f596fffa80f7cfaeb26eefaf8d4ca8be190393b2569249d673f042".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = a.CreateEncryptor())
                {
                    Assert.True(encryptor.CanTransformMultipleBlocks);
                    byte[] cipher = encryptor.Transform(plainText, blockSizeMultipler: 2);
                    Assert.Equal<byte>(expectedCipher, cipher);
                }

                using (ICryptoTransform decryptor = a.CreateDecryptor())
                {
                    Assert.True(decryptor.CanTransformMultipleBlocks);
                    byte[] decrypted = decryptor.Transform(expectedCipher, blockSizeMultipler: 2);
                    Assert.Equal<byte>(plainText, decrypted);
                }
            }
        }

        [Fact]
        public static void MultipleBlockTransformPKCS7()
        {
            // Ensure that multiple blocks can be transformed with one call. (the PKCS7 code path)

            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "f238882f6530ae9191c294868feed0b0df4058b322377dec14690c3b6bbf6ad1dd5b7c063a28e2cca2a6dce8cc2e668ea6ce80cee4c1a1a955ff46c530f3801b".HexToByteArray();
            // CBC, Padding.PKCS7
            byte[] expectedCipher = "7c6e1bcd3c30d2fb2d92e3346048307dc6719a6b96a945b4d987af09469ec68f5ca535fab7f596fffa80f7cfaeb26eefaf8d4ca8be190393b2569249d673f042a6a223f1c1069aa1d3c19d6bc454c205".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = a.CreateEncryptor())
                {
                    Assert.True(encryptor.CanTransformMultipleBlocks);
                    byte[] cipher = encryptor.Transform(plainText, blockSizeMultipler: 2);
                    Assert.Equal<byte>(expectedCipher, cipher);
                }

                using (ICryptoTransform decryptor = a.CreateDecryptor())
                {
                    Assert.True(decryptor.CanTransformMultipleBlocks);
                    byte[] decrypted = decryptor.Transform(expectedCipher, blockSizeMultipler: 2);
                    Assert.Equal<byte>(plainText, decrypted);
                }
            }
        }

        [Fact]
        public static void FinalOnlyTransformNoPad()
        {
            // Use no TransformBlock calls() - do the entire transform using only TransformFinalBlock().
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "f238882f6530ae9191c294868feed0b0df4058b322377dec14690c3b6bbf6ad1dd5b7c063a28e2cca2a6dce8cc2e668ea6ce80cee4c1a1a955ff46c530f3801b".HexToByteArray();
            // CBC, Padding.None
            byte[] expectedCipher = "7c6e1bcd3c30d2fb2d92e3346048307dc6719a6b96a945b4d987af09469ec68f5ca535fab7f596fffa80f7cfaeb26eefaf8d4ca8be190393b2569249d673f042".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = a.CreateEncryptor())
                {
                    Assert.True(encryptor.CanTransformMultipleBlocks);
                    byte[] cipher = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
                    Assert.Equal<byte>(expectedCipher, cipher);
                }

                using (ICryptoTransform decryptor = a.CreateDecryptor())
                {
                    Assert.True(decryptor.CanTransformMultipleBlocks);
                    byte[] decrypted = decryptor.TransformFinalBlock(expectedCipher, 0, expectedCipher.Length);
                    Assert.Equal<byte>(plainText, decrypted);
                }
            }
        }

        [Fact]
        public static void FinalOnlyTransformPKCS7()
        {
            // Use no TransformBlock calls() - do the entire transform using only TransformFinalBlock().
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "f238882f6530ae9191c294868feed0b0df4058b322377dec14690c3b6bbf6ad1dd5b7c063a28e2cca2a6dce8cc2e668ea6ce80cee4c1a1a955ff46c530f3801b".HexToByteArray();
            // CBC, Padding.PKCS7
            byte[] expectedCipher = "7c6e1bcd3c30d2fb2d92e3346048307dc6719a6b96a945b4d987af09469ec68f5ca535fab7f596fffa80f7cfaeb26eefaf8d4ca8be190393b2569249d673f042a6a223f1c1069aa1d3c19d6bc454c205".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = a.CreateEncryptor())
                {
                    Assert.True(encryptor.CanTransformMultipleBlocks);
                    byte[] cipher = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
                    Assert.Equal<byte>(expectedCipher, cipher);
                }

                using (ICryptoTransform decryptor = a.CreateDecryptor())
                {
                    Assert.True(decryptor.CanTransformMultipleBlocks);
                    byte[] decrypted = decryptor.TransformFinalBlock(expectedCipher, 0, expectedCipher.Length);
                    Assert.Equal<byte>(plainText, decrypted);
                }
            }
        }

        [Fact]
        public static void ZeroLengthTransformNoPad()
        {
            // Use no TransformBlock calls() - do the entire transform using only TransformFinalBlock().
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "".HexToByteArray();
            // CBC, Padding.None
            byte[] expectedCipher = "".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.None;

                using (ICryptoTransform encryptor = a.CreateEncryptor())
                {
                    Assert.True(encryptor.CanTransformMultipleBlocks);
                    byte[] cipher = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
                    Assert.Equal<byte>(expectedCipher, cipher);
                }

                using (ICryptoTransform decryptor = a.CreateDecryptor())
                {
                    Assert.True(decryptor.CanTransformMultipleBlocks);
                    byte[] decrypted = decryptor.TransformFinalBlock(expectedCipher, 0, expectedCipher.Length);
                    Assert.Equal<byte>(plainText, decrypted);
                }
            }
        }

        [Fact]
        public static void ZeroLengthTransformPKCS7()
        {
            // Use no TransformBlock calls() - do the entire transform using only TransformFinalBlock().
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "".HexToByteArray();
            // CBC, Padding.PKCS7
            byte[] expectedCipher = "d5450767bcc31793fe5065251b96b715".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = a.CreateEncryptor())
                {
                    Assert.True(encryptor.CanTransformMultipleBlocks);
                    byte[] cipher = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
                    Assert.Equal<byte>(expectedCipher, cipher);
                }

                using (ICryptoTransform decryptor = a.CreateDecryptor())
                {
                    Assert.True(decryptor.CanTransformMultipleBlocks);
                    byte[] decrypted = decryptor.TransformFinalBlock(expectedCipher, 0, expectedCipher.Length);
                    Assert.Equal<byte>(plainText, decrypted);
                }
            }
        }

#if netstandard17
        [Fact]
        public static void PaddingMode_Validation()
        {
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            byte[] plainText = "".HexToByteArray();
            ValidatePaddingMode(plainText, Array.Empty<byte>(), PaddingMode.Zeros, 0); // no block is added in this case!
            ValidatePaddingMode(plainText, "d5450767bcc31793fe5065251b96b715".HexToByteArray(), PaddingMode.PKCS7, 16);
            ValidatePaddingMode(plainText, "a3d32a3a9dca71b6f961f5a8ed7e414f".HexToByteArray(), PaddingMode.ANSIX923, 16);
            ValidatePaddingMode(plainText, Array.Empty<byte>(), PaddingMode.ISO10126, 16);

            plainText = "e505a2".HexToByteArray();
            ValidatePaddingMode(plainText, "0a2e62938b03e5822ee251117a4ce066".HexToByteArray(), PaddingMode.Zeros, 13);
            ValidatePaddingMode(plainText, "46785bde46622b92ff7c8ebb91508a4d".HexToByteArray(), PaddingMode.PKCS7, 13);
            ValidatePaddingMode(plainText, "43b27d41a9fde73ca5db22c0fda76cb1".HexToByteArray(), PaddingMode.ANSIX923, 13);
            ValidatePaddingMode(plainText, Array.Empty<byte>(), PaddingMode.ISO10126, 13);

            plainText = "46785bde46622b92ff7c8ebb91508a".HexToByteArray();
            ValidatePaddingMode(plainText, "1be8aa365a15d11fc7826b3a10602d09".HexToByteArray(), PaddingMode.Zeros, 1);
            ValidatePaddingMode(plainText, "db5b7829cce732bfe609140cf45a8843".HexToByteArray(), PaddingMode.PKCS7, 1);
            ValidatePaddingMode(plainText, "db5b7829cce732bfe609140cf45a8843".HexToByteArray(), PaddingMode.ANSIX923, 1);
            ValidatePaddingMode(plainText, Array.Empty<byte>(), PaddingMode.ISO10126, 1);
        }

        private static void ValidatePaddingMode(byte[] plainText, byte[] expectedCipher, PaddingMode paddingMode, int expectedPaddingSize)
        {
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();

            using (Aes a = Aes.Create())
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = paddingMode;

                byte[] cipher = a.Encrypt(plainText);

                // we cannot validate the cipher in this padding mode as it consists of random data
                if (paddingMode != PaddingMode.ISO10126)
                {
                    Assert.Equal<byte>(expectedCipher, cipher);
                }

                // decrypt it with PaddingMode.None so that we can inspect the padding manually
                a.Padding = PaddingMode.None;
                byte[] decrypted = a.Decrypt(cipher);
                ValidatePadding(decrypted, paddingMode, expectedPaddingSize);
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

#endif
    }
}
