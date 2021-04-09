// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    /// <summary>
    /// Since AesManaged wraps Aes, we only test minimally here.
    /// </summary>
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class AesManagedTests
    {
        [Fact]
        public static void VerifyDefaults()
        {
            using (var alg = new AesManaged())
            {
                Assert.Equal(128, alg.BlockSize);
                Assert.Equal(256, alg.KeySize);
                Assert.Equal(CipherMode.CBC, alg.Mode);
                Assert.Equal(PaddingMode.PKCS7, alg.Padding);
            }
        }

        [Fact]
        public static void EncryptDecryptKnownECB192()
        {
            byte[] plainTextBytes =
                new ASCIIEncoding().GetBytes("This is a sentence that is longer than a block, it ensures that multi-block functions work.");

            byte[] encryptedBytesExpected = new byte[]
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

            byte[] aes192Key = new byte[]
            {
                0xA6, 0x1E, 0xC7, 0x54, 0x37, 0x4D, 0x8C, 0xA5,
                0xA4, 0xBB, 0x99, 0x50, 0x35, 0x4B, 0x30, 0x4D,
                0x6C, 0xFE, 0x3B, 0x59, 0x65, 0xCB, 0x93, 0xE3,
            };

            using (var alg = new AesManaged())
            {
                // The CipherMode and KeySize are different than the default values; this ensures the type
                // forwards the state properly to Aes.
                alg.Mode = CipherMode.ECB;
                alg.Key = aes192Key;

                byte[] encryptedBytes = alg.Encrypt(plainTextBytes);
                Assert.Equal(encryptedBytesExpected, encryptedBytes);

                byte[] decryptedBytes = alg.Decrypt(encryptedBytes);
                Assert.Equal(plainTextBytes, decryptedBytes);
            }
        }

        [Fact]
        public static void TestShims()
        {
            using (var alg = new AesManaged())
            {
                alg.BlockSize = 128;
                Assert.Equal(128, alg.BlockSize);

                var emptyIV = new byte[alg.BlockSize / 8];
                alg.IV = emptyIV;
                Assert.Equal(emptyIV, alg.IV);
                alg.GenerateIV();
                Assert.NotEqual(emptyIV, alg.IV);

                var emptyKey = new byte[alg.KeySize / 8];
                alg.Key = emptyKey;
                Assert.Equal(emptyKey, alg.Key);
                alg.GenerateKey();
                Assert.NotEqual(emptyKey, alg.Key);

                alg.KeySize = 128;
                Assert.Equal(128, alg.KeySize);

                alg.Mode = CipherMode.ECB;
                Assert.Equal(CipherMode.ECB, alg.Mode);

                alg.Padding = PaddingMode.PKCS7;
                Assert.Equal(PaddingMode.PKCS7, alg.Padding);
            }
        }
    }
}
