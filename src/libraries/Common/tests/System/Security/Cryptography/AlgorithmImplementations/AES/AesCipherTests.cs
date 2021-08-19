// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void DecryptKnownCFB128_256()
        {
            byte[] encryptedBytes = new byte[]
            {
                0x71, 0x67, 0xD7, 0x86, 0xAA, 0xF8, 0xD9, 0x1A,
                0x3A, 0xFB, 0xA5, 0x9E, 0x41, 0xCA, 0x39, 0x32,
                0x6A, 0x42, 0xA4, 0xD2, 0x26, 0x32, 0x85, 0x05,
                0x5A, 0x98, 0xE4, 0x3A, 0xDA, 0xD7, 0x1B, 0x1A,
                0x47, 0x08, 0xF7, 0x7F, 0xC7, 0x08, 0xBF, 0x7C,
                0x57, 0xE9, 0x13, 0xD5, 0x4F, 0x8F, 0x23, 0x76,
                0xAA, 0xB5, 0x83, 0xE1, 0x5C, 0x48, 0x8A, 0x0D,
                0x4A, 0xFD, 0x10, 0x7C, 0xF1, 0x1B, 0x86, 0xA8,
                0xAB, 0x9D, 0x5B, 0x49, 0x9D, 0xA4, 0x9C, 0x9B,
                0x2B, 0xD1, 0xEC, 0xFF, 0xEB, 0xF7, 0x3B, 0x69,
                0x80, 0x0F, 0xA6, 0x26, 0x3E, 0xD9, 0x72, 0xB9,
                0x72, 0x22, 0x85, 0x50, 0x95, 0x59, 0xFA, 0x5F
            };

            TestAesDecrypt(CipherMode.CFB, s_aes256Key, s_aes256CbcIv, encryptedBytes, s_multiBlockBytes, 128);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void DecryptKnownCFB128_192()
        {
            byte[] encryptedBytes = new byte[]
            {
                0x7C, 0xC6, 0xEE, 0xD8, 0xED, 0xB5, 0x3F, 0x8A,
                0x90, 0x95, 0x12, 0xD2, 0xBC, 0x9A, 0x96, 0x1E,
                0x4E, 0xC4, 0xD1, 0x15, 0xA4, 0x7F, 0x32, 0xA4,
                0xD1, 0xFD, 0x8E, 0x02, 0x45, 0xE8, 0x93, 0x3C,
                0x3C, 0x91, 0x3F, 0xA4, 0x7F, 0x99, 0xF7, 0x3A,
                0x53, 0x0C, 0x0B, 0xFD, 0x01, 0xC5, 0xBD, 0x76,
                0xB7, 0xCF, 0x2B, 0x52, 0x34, 0xB1, 0xA6, 0xA4,
                0x29, 0x2F, 0x7D, 0x1C, 0x97, 0x3A, 0xE2, 0x75,
                0x3E, 0xEB, 0xFC, 0xB7, 0xBB, 0x7A, 0xC0, 0x66,
                0x34, 0x25, 0xCF, 0x2D, 0xE2, 0x7E, 0x23, 0x06,
                0x10, 0xFE, 0xEA, 0xB3, 0x0F, 0x1D, 0x2C, 0xDD,
                0x72, 0x64, 0x51, 0x78, 0x1D, 0x75, 0xD2, 0x17
            };

            TestAesDecrypt(CipherMode.CFB, s_aes192Key, s_aes256CbcIv, encryptedBytes, s_multiBlockBytes, 128);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void DecryptKnownCFB128_128()
        {
            byte[] encryptedBytes = new byte[]
            {
                0x5B, 0x63, 0x3D, 0x1C, 0x0C, 0x8E, 0xD4, 0xF4,
                0xE5, 0x5F, 0xA0, 0xAF, 0x2F, 0xF5, 0xAE, 0x59,
                0xB9, 0xC4, 0xFA, 0x02, 0x11, 0x37, 0xEB, 0x38,
                0x5B, 0x2F, 0x1D, 0xF5, 0x03, 0xD1, 0xFD, 0x85,
                0x4B, 0xAA, 0x4F, 0x29, 0x94, 0x09, 0x31, 0x4C,
                0x4D, 0xD6, 0x99, 0xE3, 0x4D, 0xC4, 0x3A, 0x40,
                0x97, 0x58, 0xA5, 0x26, 0x80, 0xA8, 0xCA, 0xFA,
                0x6D, 0x19, 0x3B, 0x6B, 0x6F, 0x75, 0x76, 0x83,
                0x90, 0x31, 0x07, 0x86, 0x35, 0xD6, 0xAB, 0xB4,
                0x65, 0x07, 0x0A, 0x0A, 0xA3, 0x7A, 0xD7, 0x16,
                0xE2, 0xC5, 0x3B, 0xE0, 0x42, 0x5F, 0xFA, 0xEF,
                0xE1, 0x2E, 0x40, 0x84, 0x36, 0x66, 0xB1, 0xBA
            };

            TestAesDecrypt(CipherMode.CFB, s_aes128Key, s_aes256CbcIv, encryptedBytes, s_multiBlockBytes, 128);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_8_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12 },
                iv: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                plainBytes: new byte[] { 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59 },
                cipherBytes: new byte[] { 0xD5, 0x47, 0xC5, 0x23, 0xCF, 0x5D, 0xFF, 0x67, 0x4C, 0xB4, 0xDB, 0x03, 0x96, 0xA3, 0xEB, 0xCF },
                8);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_128_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12 },
                iv: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                plainBytes: new byte[] { 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59 },
                cipherBytes: new byte[] { 0xD5, 0x91, 0xEE, 0x44, 0xF7, 0xC4, 0x31, 0xFB, 0x40, 0x20, 0x2F, 0x03, 0x6C, 0x14, 0x7C, 0xAC },
                128);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_256_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12, 0x14, 0x15, 0x16, 0x17, 0x19, 0x1A, 0x1B, 0x1C, 0x1E, 0x1F, 0x20, 0x21, 0x23, 0x24, 0x25, 0x26 },
                iv: new byte[] { 0x83, 0x4E, 0xAD, 0xFC, 0xCA, 0xC7, 0xE1, 0xB3, 0x06, 0x64, 0xB1, 0xAB, 0xA4, 0x48, 0x15, 0xAB },
                plainBytes: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0x19, 0x46, 0xDA, 0xBF, 0x6A, 0x03, 0xA2, 0xA2, 0xC3, 0xD0, 0xB0, 0x50, 0x80, 0xAE, 0xD6, 0xFC },
                128);
        }

        [Fact]
        public static void VerifyKnownTransform_CFB8_256_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0x00, 0x01, 0x02, 0x03, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0B, 0x0C, 0x0D, 0x0F, 0x10, 0x11, 0x12, 0x14, 0x15, 0x16, 0x17, 0x19, 0x1A, 0x1B, 0x1C, 0x1E, 0x1F, 0x20, 0x21, 0x23, 0x24, 0x25, 0x26 },
                iv: new byte[] { 0x83, 0x4E, 0xAD, 0xFC, 0xCA, 0xC7, 0xE1, 0xB3, 0x06, 0x64, 0xB1, 0xAB, 0xA4, 0x48, 0x15, 0xAB },
                plainBytes: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0x19, 0x38, 0x0A, 0x23, 0x92, 0x37, 0xC2, 0x7A, 0xBA, 0xD1, 0x82, 0x62, 0xE0, 0x36, 0x83, 0x0C },
                8);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_128_NoPadding_2()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 },
                plainBytes: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 },
                cipherBytes: new byte[] { 0xC0, 0xB7, 0x81, 0xC8, 0xC9, 0x80, 0x5A, 0x87, 0x61, 0x0E, 0xB4, 0x36, 0x6D, 0xAC, 0xA1, 0x2E },
                128);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_128_NoPadding_3()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 },
                plainBytes: new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 },
                cipherBytes: new byte[] { 0xC0, 0xB7, 0x81, 0xC8, 0xC9, 0x80, 0x5A, 0x87, 0x61, 0x0E, 0xB4, 0x36, 0x6D, 0xAC, 0xA1, 0x2E },
                128);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_192_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
                plainBytes: new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
                cipherBytes: new byte[] { 0xE9, 0x6E, 0xA7, 0xDA, 0x76, 0x90, 0xCB, 0x98, 0x56, 0x54, 0xE8, 0xFB, 0x86, 0xA3, 0xEB, 0x95 },
                128);
        }

        [Fact]
        public static void VerifyKnownTransform_CFB8_192_NoPadding()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
                plainBytes: new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
                cipherBytes: new byte[] { 0xE9, 0x3E, 0xE5, 0xBF, 0x29, 0xFF, 0x95, 0x6E, 0x6B, 0xD6, 0xE8, 0x6F, 0x9F, 0x6A, 0x05, 0x62 },
                8);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_192_NoPadding_2()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                iv: new byte[] { 0x81, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                plainBytes: new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                cipherBytes: new byte[] { 0xAA, 0xB9, 0xD1, 0x9F, 0x4A, 0x66, 0xEF, 0x3A, 0x9A, 0x60, 0xAF, 0x10, 0xD8, 0x3D, 0x84, 0x10 },
                128);
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
        public static void VerifyKnownTransform_CFB8_128_NoPadding_4()
        {
            // NIST CAVP AESMMT.ZIP CFB8MMT128.rsp, [ENCRYPT] COUNT=4
            // plaintext not extended
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "5d5e7f20e0a66d3e09e0e5a9912f8a46".HexToByteArray(),
                iv: "052d7ea0ad1f2956a23b27afe1d87b6b".HexToByteArray(),
                plainBytes: "b84a90fc6d".HexToByteArray(),
                cipherBytes: "1a9a61c307".HexToByteArray(),
                feedbackSize: 8);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_128_NoPadding_4_Fails()
        {
            Assert.Throws<CryptographicException>(() =>
                TestAesTransformDirectKey(
                    CipherMode.CFB,
                    PaddingMode.None,
                    key: "5d5e7f20e0a66d3e09e0e5a9912f8a46".HexToByteArray(),
                    iv: "052d7ea0ad1f2956a23b27afe1d87b6b".HexToByteArray(),
                    plainBytes: "b84a90fc6d".HexToByteArray(),
                    cipherBytes: "1a9a61c307".HexToByteArray(),
                    feedbackSize: 128)
            );
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_128_PKCS7_4()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.PKCS7,
                key: "5d5e7f20e0a66d3e09e0e5a9912f8a46".HexToByteArray(),
                iv: "052d7ea0ad1f2956a23b27afe1d87b6b".HexToByteArray(),
                plainBytes: "b84a90fc6d".HexToByteArray(),
                cipherBytes: "1aae9ac4cd4742f28ed593b48efce7cd".HexToByteArray(),
                feedbackSize: 128);
        }

        [Fact]
        public static void VerifyKnownTransform_CFB8_128_PKCS7_4()
        {
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.PKCS7,
                key: "5d5e7f20e0a66d3e09e0e5a9912f8a46".HexToByteArray(),
                iv: "052d7ea0ad1f2956a23b27afe1d87b6b".HexToByteArray(),
                plainBytes: "b84a90fc6d".HexToByteArray(),
                cipherBytes: "1a9a61c307a4".HexToByteArray(),
                feedbackSize: 8);
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_128_NoOrZeroPadding_0_Extended(PaddingMode paddingMode)
        {
            // NIST CAVP AESMMT.ZIP CFB8MMT128.rsp, [ENCRYPT] COUNT=0
            // plaintext zero-extended to a full block, cipherBytes extended value
            // provided by .NET Framework
            TestAesTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "c57d699d89df7cfbef71c080a6b10ac3".HexToByteArray(),
                iv: "fcb2bc4c006b87483978796a2ae2c42e".HexToByteArray(),
                plainBytes: ("61" + "000000000000000000000000000000").HexToByteArray(),
                cipherBytes: ("24" + "D89FE413C3D37172D6B577E2F94997").HexToByteArray(),
                feedbackSize: 8);
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_128_NoOrZeroPadding_9_Extended(PaddingMode paddingMode)
        {
            // NIST CAVP AESMMT.ZIP CFB8MMT128.rsp, [ENCRYPT] COUNT=9
            // plaintext zero-extended to a full block, cipherBytes extended value
            // provided by .NET Framework
            TestAesTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "3a6f9159263fa6cef2a075caface5817".HexToByteArray(),
                iv: "0fc23662b7dbf73827f0c7de321ca36e".HexToByteArray(),
                plainBytes: ("87efeb8d559ed3367728" + "000000000000").HexToByteArray(),
                cipherBytes: ("8e9c50425614d540ce11" + "7DD85E93D8E0").HexToByteArray(),
                feedbackSize: 8);
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_192_NoOrZeroPadding_0_Extended(PaddingMode paddingMode)
        {
            // NIST CAVP AESMMT.ZIP CFB8MMT192.rsp, [ENCRYPT] COUNT=0
            // plaintext zero-extended to a full block, cipherBytes extended value
            // provided by .NET Framework
            TestAesTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "32a1b0e3da368db563d7316b9779d3327e53d9a6d287ed97".HexToByteArray(),
                iv: "3dd0e7e21f09d5842f3a699da9b57346".HexToByteArray(),
                plainBytes: ("54" + "000000000000000000000000000000").HexToByteArray(),
                cipherBytes: ("6d" + "B3F513638A136D73873517AF1A770F").HexToByteArray(),
                feedbackSize: 8);
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_192_NoOrZeroPadding_9_Extended(PaddingMode paddingMode)
        {
            // NIST CAVP AESMMT.ZIP CFB8MMT192.rsp, [ENCRYPT] COUNT=9
            // plaintext zero-extended to a full block, cipherBytes extended value
            // provided by .NET Framework
            TestAesTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "537e7bf661fd4024a024613f15b13690f7d0c847c1e18965".HexToByteArray(),
                iv: "3a81f9d9d3c155b0caad5d73349476fc".HexToByteArray(),
                plainBytes: ("d3d8b9b984adc24237ee" + "000000000000").HexToByteArray(),
                cipherBytes: ("3879fea72ac99929e53a" + "39552A575D73").HexToByteArray(),
                feedbackSize: 8);
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_256_NoOrZeroPadding_0_Extended(PaddingMode paddingMode)
        {
            // NIST CAVP AESMMT.ZIP CFB8MMT256.rsp, [ENCRYPT] COUNT=0
            // plaintext zero-extended to a full block, cipherBytes extended value
            // provided by .NET Framework
            TestAesTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "34e8091cee09f1bd3ebf1e8f05f51bfbd4899ef2ae006a3a0f7875052cdd46c8".HexToByteArray(),
                iv: "43eb4dcc4b04a80216a20e4a09a7abb5".HexToByteArray(),
                plainBytes: ("f9" + "000000000000000000000000000000").HexToByteArray(),
                cipherBytes: ("28" + "26199F76D20BE53AB4D146CFC6D281").HexToByteArray(),
                feedbackSize: 8);
        }

        [Theory]
        [InlineData(PaddingMode.None)]
        [InlineData(PaddingMode.Zeros)]
        public static void VerifyKnownTransform_CFB8_256_NoOrZeroPadding_9_Extended(PaddingMode paddingMode)
        {
            // NIST CAVP AESMMT.ZIP CFB8MMT256.rsp, [ENCRYPT] COUNT=9
            // plaintext zero-extended to a full block, cipherBytes extended value
            // provided by .NET Framework
            TestAesTransformDirectKey(
                CipherMode.CFB,
                paddingMode,
                key: "ebbb4566b5e182e0f072466b0b311df38f9175bc0213a5530bce2ec4d74f400d".HexToByteArray(),
                iv: "0956a48e01002c9e16376d6e308dbad1".HexToByteArray(),
                plainBytes: ("b0fe25ac8d3d28a2f471" + "000000000000").HexToByteArray(),
                cipherBytes: ("638c6823e7256fb5626e" + "5EE5C1D7FA17").HexToByteArray(),
                feedbackSize: 8);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_128_NoPadding_0()
        {
            // NIST CAVP AESMMT.ZIP CFB128MMT128.rsp, [ENCRYPT] COUNT=0
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "085b8af6788fa6bc1a0b47dcf50fbd35".HexToByteArray(),
                iv: "58cb2b12bb52c6f14b56da9210524864".HexToByteArray(),
                plainBytes: "4b5a872260293312eea1a570fd39c788".HexToByteArray(),
                cipherBytes: "e92c80e0cfb6d8b1c27fd58bc3708b16".HexToByteArray(),
                feedbackSize: 128);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_128_NoPadding_1_Extended()
        {
            // NIST CAVP AESMMT.ZIP CFB128MMT128.rsp, [ENCRYPT] COUNT=1
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "701ccc4c0e36e512ce077f5af6ccb957".HexToByteArray(),
                iv: "5337ddeaf89a00dd4d58d860de968469".HexToByteArray(),
                plainBytes: "cc1172f2f80866d0768b25f70fcf6361aab7c627c8488f97525d7d88949beeea".HexToByteArray(),
                cipherBytes: "cdcf093bb7840df225683b58a479b00d5de5553a7e85eae4b70bf46dc729dd31".HexToByteArray(),
                feedbackSize: 128);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_192_NoPadding_0_Extended()
        {
            // NIST CAVP AESMMT.ZIP CFB128MMT192.rsp, [ENCRYPT] COUNT=0
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "1bbb30016d3a908827693352ece9833415433618b1d97595".HexToByteArray(),
                iv: "b2b48e8d60240bf2d9fa05cc2f90c161".HexToByteArray(),
                plainBytes: "b4e499de51e646fad80030da9dc5e7e2".HexToByteArray(),
                cipherBytes: "8b7ba98982063a55fca3492269bbe437".HexToByteArray(),
                feedbackSize: 128);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_192_NoPadding_1_Extended()
        {
            // NIST CAVP AESMMT.ZIP CFB128MMT192.rsp, [ENCRYPT] COUNT=1
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "69f9d29885743826d7c5afc53637e6b1fa9512a10eea9ca9".HexToByteArray(),
                iv: "3743793c7144a755768437f4ef5a33c8".HexToByteArray(),
                plainBytes: "f84ebf42a758971c369949e288f775c9cf6a82ab51b286576b45652cd68c3ce6".HexToByteArray(),
                cipherBytes: "a3bd28bb817bdb3f6492827f2aa3e6e134c254129d8f20dbc92389b7d89702d6".HexToByteArray(),
                feedbackSize: 128);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(CipherMode.CBC, 0)]
        [InlineData(CipherMode.CFB, 128)]
        [InlineData(CipherMode.CFB, 8)]
        [InlineData(CipherMode.ECB, 0)]
        public static void EncryptorReuse_LeadsToSameResults(CipherMode cipherMode, int feedbackSize)
        {
            // AppleCCCryptor does not allow calling Reset on CFB cipher.
            // this test validates that the behavior is taken into consideration.
            var input = "b72606c98d8e4fabf08839abf7a0ac61".HexToByteArray();

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = cipherMode;

                if (feedbackSize > 0)
                {
                    aes.FeedbackSize = feedbackSize;
                }

                using (ICryptoTransform transform = aes.CreateEncryptor())
                {
                    byte[] output1 = transform.TransformFinalBlock(input, 0, input.Length);
                    byte[] output2 = transform.TransformFinalBlock(input, 0, input.Length);

                    Assert.Equal(output1, output2);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(CipherMode.CBC, 0)]
        [InlineData(CipherMode.CFB, 128)]
        [InlineData(CipherMode.CFB, 8)]
        [InlineData(CipherMode.ECB, 0)]
        public static void DecryptorReuse_LeadsToSameResults(CipherMode cipherMode, int feedbackSize)
        {
            // AppleCCCryptor does not allow calling Reset on CFB cipher.
            // this test validates that the behavior is taken into consideration.
            var input = "2981761d979bb1765a28b2dd19125b54".HexToByteArray();
            var key = "e1c6e6884eee69552dbfee21f22ca92685d5d08ef0e3f37e5b338c533bb8d72c".HexToByteArray();
            var iv = "cea9f23ae87a637ab0cda6381ecc1202".HexToByteArray();

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = cipherMode;
                aes.Key = key;
                aes.IV = iv;
                aes.Padding = PaddingMode.None;

                if (feedbackSize > 0)
                {
                    aes.FeedbackSize = feedbackSize;
                }

                using (ICryptoTransform transform = aes.CreateDecryptor())
                {
                    byte[] output1 = transform.TransformFinalBlock(input, 0, input.Length);
                    byte[] output2 = transform.TransformFinalBlock(input, 0, input.Length);

                    Assert.Equal(output1, output2);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_256_NoPadding_0_Extended()
        {
            // NIST CAVP AESMMT.ZIP CFB128MMT256.rsp, [ENCRYPT] COUNT=0
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "e1c6e6884eee69552dbfee21f22ca92685d5d08ef0e3f37e5b338c533bb8d72c".HexToByteArray(),
                iv: "cea9f23ae87a637ab0cda6381ecc1202".HexToByteArray(),
                plainBytes: "b72606c98d8e4fabf08839abf7a0ac61".HexToByteArray(),
                cipherBytes: "2981761d979bb1765a28b2dd19125b54".HexToByteArray(),
                feedbackSize: 128);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void VerifyKnownTransform_CFB128_256_NoPadding_1_Extended()
        {
            // NIST CAVP AESMMT.ZIP CFB128MMT256.rsp, [ENCRYPT] COUNT=1
            TestAesTransformDirectKey(
                CipherMode.CFB,
                PaddingMode.None,
                key: "ae59254c66d8f533e7f5002ced480c33984a421d7816e27be66c34c19bfbc2a8".HexToByteArray(),
                iv: "821dd21653ece3af675cd25d26017ae3".HexToByteArray(),
                plainBytes: "3cb4f17e775c2d6d06dd60f15d6c3a103e5131727f9c6cb80d13e00f316eb904".HexToByteArray(),
                cipherBytes: "ae375db9f28148c460f6c6b6665fcc2ff6b50b8eaf82c64bba8c649efd4731bc".HexToByteArray(),
                feedbackSize: 128);
        }

        [Theory]
        [InlineData(CipherMode.CBC)]
        [InlineData(CipherMode.CFB)]
        public static void AesZeroPad(CipherMode cipherMode)
        {
            if (cipherMode == CipherMode.CFB && PlatformDetection.IsWindows7)
            {
                // Windows 7 does not support CFB128.
                return;
            }

            byte[] decryptedBytes;
            byte[] expectedAnswer;

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = cipherMode;
                aes.Padding = PaddingMode.Zeros;
                aes.FeedbackSize = 128;

                int alignBytes = aes.BlockSize / 8; // Feedback size is same as block size, both are 128 bits
                int missingBytes = alignBytes - (s_multiBlockBytes.Length % alignBytes);

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
            byte[] expectedAnswer,
            int? feedbackSize = default)
        {
            byte[] decryptedBytes;
            byte[] oneShotDecryptedBytes = null;

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = mode;
                aes.Key = key;

                if (feedbackSize.HasValue)
                {
                    aes.FeedbackSize = feedbackSize.Value;
                }

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

                if (mode == CipherMode.ECB)
                {
                    oneShotDecryptedBytes = aes.DecryptEcb(encryptedBytes, aes.Padding);
                }
            }

            Assert.NotEqual(encryptedBytes, decryptedBytes);
            Assert.Equal(expectedAnswer, decryptedBytes);

            if (oneShotDecryptedBytes is not null)
            {
                Assert.Equal(expectedAnswer, oneShotDecryptedBytes);
            }
        }

        private static void TestAesTransformDirectKey(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[] iv,
            byte[] plainBytes,
            byte[] cipherBytes,
            int? feedbackSize = default)
        {
            byte[] liveEncryptBytes;
            byte[] liveDecryptBytes;
            byte[] liveOneShotDecryptBytes = null;
            byte[] liveOneShotEncryptBytes = null;

            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = cipherMode;
                aes.Padding = paddingMode;
                aes.Key = key;

                if (feedbackSize.HasValue)
                {
                    aes.FeedbackSize = feedbackSize.Value;
                }

                liveEncryptBytes = AesEncryptDirectKey(aes, key, iv, plainBytes);
                liveDecryptBytes = AesDecryptDirectKey(aes, key, iv, cipherBytes);

                if (cipherMode == CipherMode.ECB)
                {
                    liveOneShotDecryptBytes = aes.DecryptEcb(cipherBytes, paddingMode);
                    liveOneShotEncryptBytes = aes.EncryptEcb(plainBytes, paddingMode);
                }
                else if (cipherMode == CipherMode.CBC)
                {
                    liveOneShotDecryptBytes = aes.DecryptCbc(cipherBytes, iv, paddingMode);
                    liveOneShotEncryptBytes = aes.EncryptCbc(plainBytes, iv, paddingMode);
                }
                else if (cipherMode == CipherMode.CFB)
                {
                    liveOneShotDecryptBytes = aes.DecryptCfb(cipherBytes, iv, paddingMode, feedbackSizeInBits: feedbackSize.Value);
                    liveOneShotEncryptBytes = aes.EncryptCfb(plainBytes, iv, paddingMode, feedbackSizeInBits: feedbackSize.Value);
                }
            }

            Assert.Equal(cipherBytes, liveEncryptBytes);
            Assert.Equal(plainBytes, liveDecryptBytes);

            if (liveOneShotDecryptBytes is not null)
            {
                Assert.Equal(plainBytes, liveOneShotDecryptBytes);
            }

            if (liveOneShotEncryptBytes is not null)
            {
                Assert.Equal(cipherBytes, liveOneShotEncryptBytes);
            }
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
