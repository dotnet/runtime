// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public partial class AesCipherTests
    {
        [Fact]
        public static void RandomKeyRoundtrip_Default()
        {
            using (Aes aes = AesFactory.Create())
            {
                RandomKeyRoundtrip(aes);
            }
        }

        [Fact]
        public static void RandomKeyRoundtrip_128()
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.KeySize = 128;

                RandomKeyRoundtrip(aes);
            }
        }

        [Fact]
        public static void RandomKeyRoundtrip_192()
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.KeySize = 192;

                RandomKeyRoundtrip(aes);
            }
        }

        [Fact]
        public static void RandomKeyRoundtrip_256()
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.KeySize = 256;

                RandomKeyRoundtrip(aes);
            }
        }

        [Fact]
        public static void DecryptKnownCBC256()
        {
            byte[] encryptedBytes = new byte[]
            {
                0x6C, 0xBC, 0xE1, 0xAF, 0x8A, 0xAC, 0xE0, 0xA2,
                0x2E, 0xAD, 0xB2, 0x9C, 0x28, 0x40, 0x72, 0x72,
                0xAE, 0x38, 0xFD, 0xA0, 0xE9, 0xE0, 0xE6, 0xD3,
                0x28, 0xFB, 0xBF, 0x21, 0xDE, 0xCC, 0xCC, 0x22,
                0x31, 0x46, 0x35, 0xF4, 0x18, 0xE9, 0x01, 0x98,
                0xF0, 0x6F, 0x35, 0x3F, 0xA4, 0x61, 0x3D, 0x4A,
                0x20, 0x27, 0xB4, 0xCA, 0x67, 0x31, 0x0D, 0x38,
                0x49, 0x0D, 0xCE, 0xD5, 0x92, 0x3A, 0x78, 0x77,
                0x00, 0x5E, 0xF9, 0x60, 0xE3, 0x10, 0x8D, 0x14,
                0x8F, 0xDC, 0x68, 0x80, 0x0D, 0xEC, 0xFA, 0x5F,
                0x19, 0xFE, 0x8E, 0x94, 0x57, 0x87, 0x2B, 0xED,
                0x08, 0x0F, 0xB4, 0x99, 0x0D, 0x1A, 0xE1, 0x41,
            };

            TestAesDecrypt(CipherMode.CBC, s_aes256Key, s_aes256CbcIv, encryptedBytes, s_multiBlockBytes);
        }
        
        [Fact]
        public static void DecryptKnownECB192()
        {
            byte[] encryptedBytes = new byte[]
            {
                0xC9, 0x7F, 0xA5, 0x5B, 0xC3, 0x92, 0xDC, 0xA6,
                0xE4, 0x9F, 0x2D, 0x1A, 0xEF, 0x7A, 0x27, 0x03,
                0x04, 0x9C, 0xFB, 0x56, 0x63, 0x38, 0xAE, 0x4F,
                0xDC, 0xF6, 0x36, 0x98, 0x28, 0x05, 0x32, 0xE9,
                0xF2, 0x6E, 0xEC, 0x0C, 0x04, 0x9D, 0x12, 0x17,
                0x18, 0x35, 0xD4, 0x29, 0xFC, 0x01, 0xB1, 0x20,
                0xFA, 0x30, 0xAE, 0x00, 0x53, 0xD4, 0x26, 0x25,
                0xA4, 0xFD, 0xD5, 0xE6, 0xED, 0x79, 0x35, 0x2A,
                0xE2, 0xBB, 0x95, 0x0D, 0xEF, 0x09, 0xBB, 0x6D,
                0xC5, 0xC4, 0xDB, 0x28, 0xC6, 0xF4, 0x31, 0x33,
                0x9A, 0x90, 0x12, 0x36, 0x50, 0xA0, 0xB7, 0xD1,
                0x35, 0xC4, 0xCE, 0x81, 0xE5, 0x2B, 0x85, 0x6B,
            };

            TestAesDecrypt(CipherMode.ECB, s_aes192Key, null, encryptedBytes, s_multiBlockBytes);
        }
        
        [Fact]
        public static void VerifyInPlaceEncryption()
        {
            byte[] expectedCipherText = new byte[]
            {
                0x08, 0x58, 0x26, 0x94, 0xf3, 0x4f, 0x7f, 0xc9,
                0x0a, 0x59, 0x1a, 0x51, 0xa6, 0x56, 0x97, 0x4e,
                0x95, 0x07, 0x1a, 0x94, 0x0e, 0x53, 0x8d, 0x8a,
                0x48, 0xb4, 0x30, 0x6b, 0x08, 0xe0, 0x89, 0x3b
            };

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                aes.Key = new byte[]
                {
                    0x00, 0x04, 0x08, 0x0c, 0x10, 0x14, 0x18, 0x1c,
                    0x20, 0x24, 0x28, 0x2c, 0x30, 0x34, 0x38, 0x3c,
                    0x40, 0x44, 0x48, 0x4c, 0x50, 0x54, 0x58, 0x5c,
                    0x60, 0x64, 0x68, 0x6c, 0x70, 0x74, 0x78, 0x7c,
                };

                aes.IV = new byte[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75 };

                // buffer[1 .. Length-1] is "input" (all zeroes)
                // buffer[0 .. Length-2] is "output"
                byte[] buffer = new byte[expectedCipherText.Length + 1];
                int bytesWritten;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    bytesWritten = encryptor.TransformBlock(buffer, 1, expectedCipherText.Length, buffer, 0);
                }

                // Most implementations of AES would be expected to return expectedCipherText.Length here,
                // because AES encryption doesn't have to hold back a block in case it was the final, padded block.
                //
                // But, there's nothing in AES that requires this to be true. An implementation could exist
                // that saves up all of the data from TransformBlock and waits until TransformFinalBlock to give
                // anything back. Or encrypt could also hold one block in reserve. Or any other reason.
                //
                // But, if TransformBlock writes non-zero bytes, they should be correct, even when writing back
                // to the same array that was originally input.

                byte[] expectedSlice = expectedCipherText;

                if (bytesWritten != expectedCipherText.Length)
                {
                    expectedSlice = new byte[bytesWritten];
                    Buffer.BlockCopy(expectedCipherText, 0, expectedSlice, 0, bytesWritten);
                }

                byte[] actualCipherText = new byte[bytesWritten];
                Buffer.BlockCopy(buffer, 0, actualCipherText, 0, bytesWritten);

                Assert.Equal(expectedSlice, actualCipherText);
            }
        }

        [Fact]
        public static void VerifyInPlaceDecryption()
        {
            byte[] key = "1ed2f625c187b993256a8b3ccf9dcbfa5b44b4795c731012f70e4e64732efd5d".HexToByteArray();
            byte[] iv = "47d1e060ba3c8643f9f8b65feeda4b30".HexToByteArray();
            byte[] plainText = "f238882f6530ae9191c294868feed0b0df4058b322377dec14690c3b6bbf6ad1dd5b7c063a28e2cca2a6dce8cc2e668ea6ce80cee4c1a1a955ff46c530f3801b".HexToByteArray();
            byte[] cipher = "7c6e1bcd3c30d2fb2d92e3346048307dc6719a6b96a945b4d987af09469ec68f5ca535fab7f596fffa80f7cfaeb26eefaf8d4ca8be190393b2569249d673f042".HexToByteArray();

            using (Aes a = AesFactory.Create())
            using (MemoryStream cipherStream = new MemoryStream(cipher))
            {
                a.Key = key;
                a.IV = iv;
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.None;

                int blockSizeBytes = a.BlockSize / 8;
                List<byte> decrypted = new List<byte>(plainText.Length);

                using (ICryptoTransform decryptor = a.CreateDecryptor())
                {
                    while (true)
                    {
                        byte[] buffer = new byte[blockSizeBytes];
                        int numRead = cipherStream.Read(buffer, 0, blockSizeBytes);

                        if (numRead == 0)
                        {
                            break;
                        }

                        Assert.Equal(blockSizeBytes, numRead);
                        int numBytesWritten = decryptor.TransformBlock(buffer, 0, blockSizeBytes, buffer, 0);
                        Array.Resize(ref buffer, numBytesWritten);
                        decrypted.AddRange(buffer);
                    }

                    decrypted.AddRange(decryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0));

                    Assert.Equal(plainText, decrypted.ToArray());
                }
            }
        }

        [Fact]
        public static void VerifyKnownTransform_ECB128_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.ECB,
                PaddingMode.None,
                key: new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12 },
                iv: null,
                plainBytes: new byte[] { 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59 },
                cipherBytes: new byte[] { 0xD8, 0xF5, 0x32, 0x53, 0x82, 0x89, 0xEF, 0x7D, 0x06, 0xB5, 0x06, 0xA4, 0xFD, 0x5B, 0xE9, 0xC9 });
        }

        [Fact]
        public static void VerifyKnownTransform_ECB256_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.ECB,
                PaddingMode.None,
                key: new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12, 0x14, 0x15, 0x16, 0x17, 0x19, 0x1A, 0x1B, 0x1C, 0x1E, 0x1F, 0x20, 0x21, 0x23, 0x24, 0x25, 0x26 },
                iv: null,
                plainBytes: new byte[] { 0x83, 0x4E, 0xAD, 0xFC, 0xCA, 0xC7, 0xE1, 0xB3, 0x06, 0x64, 0xB1, 0xAB, 0xA4, 0x48, 0x15, 0xAB },
                cipherBytes: new byte[] { 0x19, 0x46, 0xDA, 0xBF, 0x6A, 0x03, 0xA2, 0xA2, 0xC3, 0xD0, 0xB0, 0x50, 0x80, 0xAE, 0xD6, 0xFC });
        }

        [Fact]
        public static void VerifyKnownTransform_ECB128_NoPadding_2()
        {
            TestAesTransformDirectKey(
                CipherMode.ECB,
                PaddingMode.None,
                key: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: null,
                plainBytes: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0x0E, 0xDD, 0x33, 0xD3, 0xC6, 0x21, 0xE5, 0x46, 0x45, 0x5B, 0xD8, 0xBA, 0x14, 0x18, 0xBE, 0xC8 });
        }

        [Fact]
        public static void VerifyKnownTransform_ECB128_NoPadding_3()
        {
            TestAesTransformDirectKey(
                CipherMode.ECB,
                PaddingMode.None,
                key: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: null,
                plainBytes: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0x3A, 0xD7, 0x8E, 0x72, 0x6C, 0x1E, 0xC0, 0x2B, 0x7E, 0xBF, 0xE9, 0x2B, 0x23, 0xD9, 0xEC, 0x34 });
        }

        [Fact]
        public static void VerifyKnownTransform_ECB192_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.ECB,
                PaddingMode.None,
                key: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: null,
                plainBytes: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0xDE, 0x88, 0x5D, 0xC8, 0x7F, 0x5A, 0x92, 0x59, 0x40, 0x82, 0xD0, 0x2C, 0xC1, 0xE1, 0xB4, 0x2C });
        }

        [Fact]
        public static void VerifyKnownTransform_ECB192_NoPadding_2()
        {
            TestAesTransformDirectKey(
                CipherMode.ECB,
                PaddingMode.None,
                key: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: null,
                plainBytes: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0x6C, 0xD0, 0x25, 0x13, 0xE8, 0xD4, 0xDC, 0x98, 0x6B, 0x4A, 0xFE, 0x08, 0x7A, 0x60, 0xBD, 0x0C });
        }

        [Fact]
        public static void VerifyKnownTransform_CBC128_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CBC,
                PaddingMode.None,
                key: new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12 },
                iv: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                plainBytes: new byte[] { 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59 },
                cipherBytes: new byte[] { 0xD8, 0xF5, 0x32, 0x53, 0x82, 0x89, 0xEF, 0x7D, 0x06, 0xB5, 0x06, 0xA4, 0xFD, 0x5B, 0xE9, 0xC9 });
        }

        [Fact]
        public static void VerifyKnownTransform_CBC256_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CBC,
                PaddingMode.None,
                key: new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12, 0x14, 0x15, 0x16, 0x17, 0x19, 0x1A, 0x1B, 0x1C, 0x1E, 0x1F, 0x20, 0x21, 0x23, 0x24, 0x25, 0x26 },
                iv: new byte[] { 0x83, 0x4E, 0xAD, 0xFC, 0xCA, 0xC7, 0xE1, 0xB3, 0x06, 0x64, 0xB1, 0xAB, 0xA4, 0x48, 0x15, 0xAB },
                plainBytes: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0x19, 0x46, 0xDA, 0xBF, 0x6A, 0x03, 0xA2, 0xA2, 0xC3, 0xD0, 0xB0, 0x50, 0x80, 0xAE, 0xD6, 0xFC });
        }

        [Fact]
        public static void VerifyKnownTransform_CBC128_NoPadding_2()
        {
            TestAesTransformDirectKey(
                CipherMode.CBC,
                PaddingMode.None,
                key: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 },
                plainBytes: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 },
                cipherBytes: new byte[] { 0x0E, 0xDD, 0x33, 0xD3, 0xC6, 0x21, 0xE5, 0x46, 0x45, 0x5B, 0xD8, 0xBA, 0x14, 0x18, 0xBE, 0xC8 });
        }

        [Fact]
        public static void VerifyKnownTransform_CBC128_NoPadding_3()
        {
            TestAesTransformDirectKey(
                CipherMode.CBC,
                PaddingMode.None,
                key: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 0x90, 5, 0, 0, 0, 60, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                plainBytes: new byte[] { 0x10, 5, 0, 0, 0, 60, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0x3A, 0xD7, 0x8E, 0x72, 0x6C, 0x1E, 0xC0, 0x2B, 0x7E, 0xBF, 0xE9, 0x2B, 0x23, 0xD9, 0xEC, 0x34 });
        }

        [Fact]
        public static void VerifyKnownTransform_CBC192_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CBC,
                PaddingMode.None,
                key: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
                plainBytes: new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
                cipherBytes: new byte[] { 0xDE, 0x88, 0x5D, 0xC8, 0x7F, 0x5A, 0x92, 0x59, 0x40, 0x82, 0xD0, 0x2C, 0xC1, 0xE1, 0xB4, 0x2C });
        }

        [Fact]
        public static void VerifyKnownTransform_CBC192_NoPadding_2()
        {
            TestAesTransformDirectKey(
                CipherMode.CBC,
                PaddingMode.None,
                key: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 0x81, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                plainBytes: new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0x6C, 0xD0, 0x25, 0x13, 0xE8, 0xD4, 0xDC, 0x98, 0x6B, 0x4A, 0xFE, 0x08, 0x7A, 0x60, 0xBD, 0x0C });
        }

        [Fact]
        public static void WrongKeyFailDecrypt()
        {
            // The test:
            // Using the encrypted bytes from the AES-192-ECB test, try decrypting
            // with the Key/IV from the AES-256-CBC test.  That would only work if
            // the implementation of AES was "return s_multiBlockBytes".
            // For this specific key/data combination, we actually expect a padding exception.
            byte[] encryptedBytes = new byte[]
            {
                0xC9, 0x7F, 0xA5, 0x5B, 0xC3, 0x92, 0xDC, 0xA6,
                0xE4, 0x9F, 0x2D, 0x1A, 0xEF, 0x7A, 0x27, 0x03,
                0x04, 0x9C, 0xFB, 0x56, 0x63, 0x38, 0xAE, 0x4F,
                0xDC, 0xF6, 0x36, 0x98, 0x28, 0x05, 0x32, 0xE9,
                0xF2, 0x6E, 0xEC, 0x0C, 0x04, 0x9D, 0x12, 0x17,
                0x18, 0x35, 0xD4, 0x29, 0xFC, 0x01, 0xB1, 0x20,
                0xFA, 0x30, 0xAE, 0x00, 0x53, 0xD4, 0x26, 0x25,
                0xA4, 0xFD, 0xD5, 0xE6, 0xED, 0x79, 0x35, 0x2A,
                0xE2, 0xBB, 0x95, 0x0D, 0xEF, 0x09, 0xBB, 0x6D,
                0xC5, 0xC4, 0xDB, 0x28, 0xC6, 0xF4, 0x31, 0x33,
                0x9A, 0x90, 0x12, 0x36, 0x50, 0xA0, 0xB7, 0xD1,
                0x35, 0xC4, 0xCE, 0x81, 0xE5, 0x2B, 0x85, 0x6B,
            };

            byte[] decryptedBytes;

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Key = s_aes256Key;
                aes.IV = s_aes256CbcIv;

                Assert.Throws<CryptographicException>(() =>
                {
                    using (MemoryStream input = new MemoryStream(encryptedBytes))
                    using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (MemoryStream output = new MemoryStream())
                    {
                        cryptoStream.CopyTo(output);
                        decryptedBytes = output.ToArray();
                    }
                });
            }
        }

        [Fact]
        public static void WrongKeyFailDecrypt_2()
        {
            // The test:
            // Using the encrypted bytes from the AES-192-ECB test, try decrypting
            // with the first 192 bits from the AES-256-CBC test.  That would only work if
            // the implementation of AES was "return s_multiBlockBytes".
            // For this specific key/data combination, we actually expect a padding exception.
            byte[] encryptedBytes = new byte[]
            {
                0xC9, 0x7F, 0xA5, 0x5B, 0xC3, 0x92, 0xDC, 0xA6,
                0xE4, 0x9F, 0x2D, 0x1A, 0xEF, 0x7A, 0x27, 0x03,
                0x04, 0x9C, 0xFB, 0x56, 0x63, 0x38, 0xAE, 0x4F,
                0xDC, 0xF6, 0x36, 0x98, 0x28, 0x05, 0x32, 0xE9,
                0xF2, 0x6E, 0xEC, 0x0C, 0x04, 0x9D, 0x12, 0x17,
                0x18, 0x35, 0xD4, 0x29, 0xFC, 0x01, 0xB1, 0x20,
                0xFA, 0x30, 0xAE, 0x00, 0x53, 0xD4, 0x26, 0x25,
                0xA4, 0xFD, 0xD5, 0xE6, 0xED, 0x79, 0x35, 0x2A,
                0xE2, 0xBB, 0x95, 0x0D, 0xEF, 0x09, 0xBB, 0x6D,
                0xC5, 0xC4, 0xDB, 0x28, 0xC6, 0xF4, 0x31, 0x33,
                0x9A, 0x90, 0x12, 0x36, 0x50, 0xA0, 0xB7, 0xD1,
                0x35, 0xC4, 0xCE, 0x81, 0xE5, 0x2B, 0x85, 0x6B,
            };

            byte[] decryptedBytes;

            // Load key as the first 192 bits of s_aes256Key.
            // It has the correct cipher block size, but the wrong value.
            byte[] key = new byte[s_aes192Key.Length];
            Buffer.BlockCopy(s_aes256Key, 0, key, 0, key.Length);

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Key = key;

                Assert.Throws<CryptographicException>(() =>
                {
                    using (MemoryStream input = new MemoryStream(encryptedBytes))
                    using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (MemoryStream output = new MemoryStream())
                    {
                        cryptoStream.CopyTo(output);
                        decryptedBytes = output.ToArray();
                    }
                });
            }
        }

        [Fact]
        public static void AesZeroPad()
        {
            byte[] decryptedBytes;
            byte[] expectedAnswer;

            using (Aes aes = AesFactory.Create())
            {
                aes.Padding = PaddingMode.Zeros;

                int blockBytes = aes.BlockSize / 8;
                int missingBytes = blockBytes - (s_multiBlockBytes.Length % blockBytes);

                // Zero-padding doesn't have enough information to remove the trailing zeroes.
                // Therefore we expect the answer of ZeroPad(s_multiBlockBytes).
                // So, make a long enough array, and copy s_multiBlockBytes to the beginning of it.
                expectedAnswer = new byte[s_multiBlockBytes.Length + missingBytes];
                Buffer.BlockCopy(s_multiBlockBytes, 0, expectedAnswer, 0, s_multiBlockBytes.Length);

                byte[] encryptedBytes;

                using (MemoryStream input = new MemoryStream(s_multiBlockBytes))
                using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateEncryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cryptoStream.CopyTo(output);
                    encryptedBytes = output.ToArray();
                }

                using (MemoryStream input = new MemoryStream(encryptedBytes))
                using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cryptoStream.CopyTo(output);
                    decryptedBytes = output.ToArray();
                }
            }

            Assert.Equal(expectedAnswer, decryptedBytes);
        }

        [Fact]
        public static void StableEncryptDecrypt()
        {
            byte[] encrypted;
            byte[] encrypted2;
            byte[] decrypted;
            byte[] decrypted2;

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Key = s_aes256Key;
                aes.IV = s_aes256CbcIv;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    encrypted = encryptor.TransformFinalBlock(s_helloBytes, 0, s_helloBytes.Length);
                }

                // Use a new encryptor for encrypted2 so that this test doesn't depend on CanReuseTransform
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    encrypted2 = encryptor.TransformFinalBlock(s_helloBytes, 0, s_helloBytes.Length);
                }

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                }

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    decrypted2 = decryptor.TransformFinalBlock(encrypted2, 0, encrypted2.Length);
                }
            }

            Assert.Equal(encrypted, encrypted2);
            Assert.Equal(decrypted, decrypted2);
            Assert.Equal(s_helloBytes, decrypted);
        }

        private static void RandomKeyRoundtrip(Aes aes)
        {
            byte[] decryptedBytes;
            byte[] encryptedBytes;

            using (MemoryStream input = new MemoryStream(s_multiBlockBytes))
            using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateEncryptor(), CryptoStreamMode.Read))
            using (MemoryStream output = new MemoryStream())
            {
                cryptoStream.CopyTo(output);
                encryptedBytes = output.ToArray();
            }

            Assert.NotEqual(s_multiBlockBytes, encryptedBytes);

            using (MemoryStream input = new MemoryStream(encryptedBytes))
            using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (MemoryStream output = new MemoryStream())
            {
                cryptoStream.CopyTo(output);
                decryptedBytes = output.ToArray();
            }

            Assert.Equal(s_multiBlockBytes, decryptedBytes);
        }

        private static void TestAesDecrypt(
            CipherMode mode,
            byte[] key,
            byte[] iv,
            byte[] encryptedBytes,
            byte[] expectedAnswer)
        {
            byte[] decryptedBytes;

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = mode;
                aes.Key = key;

                if (iv != null)
                {
                    aes.IV = iv;
                }

                using (MemoryStream input = new MemoryStream(encryptedBytes))
                using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cryptoStream.CopyTo(output);
                    decryptedBytes = output.ToArray();
                }
            }

            Assert.NotEqual(encryptedBytes, decryptedBytes);
            Assert.Equal(expectedAnswer, decryptedBytes);
        }

        private static void TestAesTransformDirectKey(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[] iv,
            byte[] plainBytes,
            byte[] cipherBytes)
        {
            byte[] liveEncryptBytes;
            byte[] liveDecryptBytes;

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = cipherMode;
                aes.Padding = paddingMode;

                liveEncryptBytes = AesEncryptDirectKey(aes, key, iv, plainBytes);
                liveDecryptBytes = AesDecryptDirectKey(aes, key, iv, cipherBytes);
            }

            Assert.Equal(plainBytes, liveDecryptBytes);
            Assert.Equal(cipherBytes, liveEncryptBytes);
        }

        private static byte[] AesEncryptDirectKey(Aes aes, byte[] key, byte[] iv, byte[] plainBytes)
        {
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();

                return output.ToArray();
            }
        }

        private static byte[] AesDecryptDirectKey(Aes aes, byte[] key, byte[] iv, byte[] cipherBytes)
        {
            using (MemoryStream output = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateDecryptor(key, iv), CryptoStreamMode.Write))
            {
                cryptoStream.Write(cipherBytes, 0, cipherBytes.Length);
                cryptoStream.FlushFinalBlock();

                return output.ToArray();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void EncryptWithLargeOutputBuffer(bool blockAlignedOutput)
        {
            using (Aes alg = AesFactory.Create())
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
            // The CreateDecryptor call reads the Key/IV property to initialize them, bypassing an
            // uninitialized state protection.
            using (Aes alg = AesFactory.Create())
            using (ICryptoTransform xform = encrypt ? alg.CreateEncryptor() : alg.CreateDecryptor(alg.Key, alg.IV))
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
            const string ExpectedOutput = "This is a 128-bit block test";

            int outputPadding = blockAlignedOutput ? 0 : 3;
            byte[] key = "0123456789ABCDEFFEDCBA9876543210".HexToByteArray();
            byte[] iv = "0123456789ABCDEF0123456789ABCDEF".HexToByteArray();
            byte[] outputBytes = new byte[iv.Length * 2 + outputPadding];
            byte[] input = "D1BF87C650FCD10B758445BE0E0A99D14652480DF53423A8B727D30C8C010EDE".HexToByteArray();
            int outputOffset = 0;

            using (Aes alg = AesFactory.Create())
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
