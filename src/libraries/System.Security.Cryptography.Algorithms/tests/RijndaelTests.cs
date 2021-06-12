// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Test.Cryptography;
using Xunit;

#pragma warning disable SYSLIB0022 // Rijndael types are obsolete

namespace System.Security.Cryptography.Encryption.Rijndael.Tests
{
    using Rijndael = System.Security.Cryptography.Rijndael;

    /// <summary>
    /// Since RijndaelImplementation (from Rijndael.Create()) and RijndaelManaged classes wrap Aes,
    /// we only test minimally here.
    /// </summary>
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class RijndaelTests
    {
        [Fact]
        public static void VerifyDefaults()
        {
            static void test(Rijndael alg)
            {
                // The block size differs from the base
                Assert.Equal(128, alg.LegalBlockSizes[0].MinSize);
                Assert.Equal(128, alg.LegalBlockSizes[0].MaxSize);
                Assert.Equal(128, alg.BlockSize);
                Assert.Equal(128, alg.FeedbackSize);

                // Different exception since we have different supported BlockSizes than desktop
                Assert.Throws<PlatformNotSupportedException>(() => alg.BlockSize = 192);
                Assert.Throws<PlatformNotSupportedException>(() => alg.BlockSize = 256);

                // Normal exception for rest
                Assert.Throws<CryptographicException>(() => alg.BlockSize = 111);
                Assert.Throws<CryptographicException>(() => alg.FeedbackSize = 15);

                Assert.Equal(CipherMode.CBC, alg.Mode);
                Assert.Equal(PaddingMode.PKCS7, alg.Padding);
            }

            using (var alg = Rijndael.Create())
            {
                // We use an internal class for the implementation, not the public RijndaelManaged
                Assert.IsNotType<RijndaelManaged>(alg);

                test(alg);
            }

            using (var alg = new RijndaelManaged())
            {
                test(alg);
            }
        }

        [Fact]
        public static void VerifyBlocksizeIVNulling()
        {
            using (var testIVAlg = Rijndael.Create())
            {
                using (var alg = Rijndael.Create())
                {
                    alg.IV = testIVAlg.IV;
                    alg.BlockSize = 128;
                    Assert.Equal(testIVAlg.IV, alg.IV);
                }

                using (var alg = new RijndaelManaged())
                {
                    alg.IV = testIVAlg.IV;
                    alg.BlockSize = 128;
                    Assert.Equal(testIVAlg.IV, alg.IV);
                }

                using (var alg = new RijndaelLegalSizesBreaker())
                {
                    // This one should set IV to null on setting BlockSize since there is only one valid BlockSize
                    alg.IV = testIVAlg.IV;
                    alg.BlockSize = 1;
                    Assert.Throws<NotImplementedException>(() => alg.IV);
                }

                using (var alg = new RijndaelMinimal())
                {
                    alg.IV = testIVAlg.IV;
                    alg.BlockSize = 128;
                    Assert.Equal(testIVAlg.IV, alg.IV);
                }
            }
        }

        [Fact]
        public static void EncryptDecryptKnownECB192()
        {
            static void test(Rijndael alg)
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

                // The CipherMode and KeySize are different than the default values; this ensures the type
                // forwards the state properly to Aes.
                alg.Mode = CipherMode.ECB;
                alg.Key = aes192Key;

                byte[] encryptedBytes = alg.Encrypt(plainTextBytes);
                Assert.Equal(encryptedBytesExpected, encryptedBytes);

                byte[] decryptedBytes = alg.Decrypt(encryptedBytes);
                Assert.Equal(plainTextBytes, decryptedBytes);
            }

            using (var alg = Rijndael.Create())
            {
                test(alg);
            }

            using (var alg = new RijndaelManaged())
            {
                test(alg);
            }
        }

        [Fact]
        public static void TestShims()
        {
            static void test(Rijndael alg)
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

                alg.FeedbackSize = 8;
                Assert.Equal(8, alg.FeedbackSize);
            }

            using (var alg = Rijndael.Create())
            {
                test(alg);
            }

            using (var alg = new RijndaelManaged())
            {
                test(alg);
            }
        }

        [Fact]
        public static void RijndaelKeySize_BaseClass()
        {
            using (Rijndael alg = new RijndaelMinimal())
            {
                Assert.Equal(128, alg.LegalKeySizes[0].MinSize);
                Assert.Equal(256, alg.LegalKeySizes[0].MaxSize);
                Assert.Equal(64, alg.LegalKeySizes[0].SkipSize);
                Assert.Equal(256, alg.KeySize);

                Assert.Equal(128, alg.LegalBlockSizes[0].MinSize);
                Assert.Equal(256, alg.LegalBlockSizes[0].MaxSize);
                Assert.Equal(64, alg.LegalBlockSizes[0].SkipSize);
                Assert.Equal(128, alg.BlockSize);
            }
        }

        [Fact]
        public static void EnsureLegalSizesValuesIsolated()
        {
            new RijndaelLegalSizesBreaker().Dispose();

            using (Rijndael alg = Rijndael.Create())
            {
                Assert.Equal(128, alg.LegalKeySizes[0].MinSize);
                Assert.Equal(128, alg.LegalBlockSizes[0].MinSize);

                alg.Key = new byte[16];
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void EncryptWithLargeOutputBuffer(bool blockAlignedOutput)
        {
            using (Rijndael alg = Rijndael.Create())
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
            using (Rijndael alg = Rijndael.Create())
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
            const string ExpectedOutput = "This is a 128-bit block test";

            int outputPadding = blockAlignedOutput ? 0 : 3;
            byte[] key = "0123456789ABCDEFFEDCBA9876543210".HexToByteArray();
            byte[] iv = "0123456789ABCDEF0123456789ABCDEF".HexToByteArray();
            byte[] outputBytes = new byte[iv.Length * 2 + outputPadding];
            byte[] input = "D1BF87C650FCD10B758445BE0E0A99D14652480DF53423A8B727D30C8C010EDE".HexToByteArray();
            int outputOffset = 0;

            using (Rijndael alg = Rijndael.Create())
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

        [Theory]
        [InlineData(128)]
        [InlineData(8)]
        [InlineData(null)]
        public static void CfbFeedbackSizeIsRespected(int? feedbackSize)
        {
            // Windows 7 CFB only supports CFB8.
            if (PlatformDetection.IsWindows7 && feedbackSize != 8)
                return;

            void Test(Rijndael alg)
            {
                alg.Mode = CipherMode.CFB;

                if (feedbackSize == null)
                {
                    feedbackSize = alg.FeedbackSize;
                }
                else
                {
                    alg.FeedbackSize = feedbackSize.Value;
                }

                int feedbackSizeBytes = feedbackSize.Value / 8;
                byte[] input = new byte[feedbackSizeBytes + 1];

                using ICryptoTransform transform = alg.CreateEncryptor();

                byte[] output = transform.TransformFinalBlock(input, 0, input.Length);
                int expectedOutputSize = (input.Length / feedbackSizeBytes) * feedbackSizeBytes + feedbackSizeBytes;

                Assert.Equal(expectedOutputSize, output.Length);
            }

            using (Rijndael alg = new RijndaelManaged())
            {
                Test(alg);
            }

            using (Rijndael alg = Rijndael.Create())
            {
                Test(alg);
            }
        }

        private class RijndaelLegalSizesBreaker : RijndaelMinimal
        {
            public RijndaelLegalSizesBreaker()
            {
                LegalKeySizesValue[0] = new KeySizes(1, 1, 0);
                LegalBlockSizesValue[0] = new KeySizes(1, 1, 0);
            }
        }

        private class RijndaelMinimal : Rijndael
        {
            // If the constructor uses a virtual call to any of the property setters
            // they will fail.
            private readonly bool _ready;

            public RijndaelMinimal()
            {
                // Don't set this as a field initializer, otherwise it runs before the base ctor.
                _ready = true;
            }

            public override int KeySize
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.KeySize = value;
                }
            }

            public override int BlockSize
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.BlockSize = value;
                }
            }

            public override byte[] IV
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.IV = value;
                }
            }

            public override byte[] Key
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Key = value;
                }
            }

            public override CipherMode Mode
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Mode = value;
                }
            }

            public override PaddingMode Padding
            {
                set
                {
                    if (!_ready)
                    {
                        throw new InvalidOperationException();
                    }

                    base.Padding = value;
                }
            }

            public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override void GenerateIV()
            {
                throw new NotImplementedException();
            }

            public override void GenerateKey()
            {
                throw new NotImplementedException();
            }
        }
    }
}

#pragma warning restore SYSLIB0022
