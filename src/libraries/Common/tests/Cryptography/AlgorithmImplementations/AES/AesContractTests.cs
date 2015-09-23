// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public class AesContractTests
    {
        [Fact]
        public static void VerifyDefaults()
        {
            using (Aes aes = Aes.Create())
            {
                Assert.Equal(128, aes.BlockSize);
                Assert.Equal(256, aes.KeySize);
                Assert.Equal(CipherMode.CBC, aes.Mode);
                Assert.Equal(PaddingMode.PKCS7, aes.Padding);
            }
        }

        [Fact]
        public static void LegalBlockSizes()
        {
            using (Aes aes = Aes.Create())
            {
                KeySizes[] blockSizes = aes.LegalBlockSizes;

                Assert.NotNull(blockSizes);
                Assert.Equal(1, blockSizes.Length);

                KeySizes blockSizeLimits = blockSizes[0];

                Assert.Equal(128, blockSizeLimits.MinSize);
                Assert.Equal(128, blockSizeLimits.MaxSize);
                Assert.Equal(0, blockSizeLimits.SkipSize);
            }
        }

        [Fact]
        public static void LegalKeySizes()
        {
            using (Aes aes = Aes.Create())
            {
                KeySizes[] keySizes = aes.LegalKeySizes;

                Assert.NotNull(keySizes);
                Assert.Equal(1, keySizes.Length);

                KeySizes keySizeLimits = keySizes[0];

                Assert.Equal(128, keySizeLimits.MinSize);
                Assert.Equal(256, keySizeLimits.MaxSize);
                Assert.Equal(64, keySizeLimits.SkipSize);
            }
        }

        [Theory]
        [InlineData(64)]        // too small
        [InlineData(129)]       // in valid range but not valid increment
        [InlineData(384)]       // too large
        [InlineData(536870928)] // number of bits overflows and wraps around to a valid size
        public static void InvalidKeySizes(int invalidKeySize)
        {
            using (Aes aes = Aes.Create())
            {
                // Test KeySize property
                Assert.Throws<CryptographicException>(() => aes.KeySize = invalidKeySize);

                // Test passing a key to CreateEncryptor and CreateDecryptor
                aes.GenerateIV();
                byte[] iv = aes.IV;
                byte[] key;
                try
                {
                    key = new byte[invalidKeySize];
                }
                catch (OutOfMemoryException) // in case there isn't enough memory at test-time to allocate the large array
                {
                    return;
                }
                Assert.Throws<ArgumentException>("key", () => aes.CreateEncryptor(key, iv));
                Assert.Throws<ArgumentException>("key", () => aes.CreateDecryptor(key, iv));
            }
        }

        [Theory]
        [InlineData(64)]        // smaller than default BlockSize
        [InlineData(129)]       // larger than default BlockSize
        [InlineData(536870928)] // number of bits overflows and wraps around to default BlockSize
        public static void InvalidIVSizes(int invalidIvSize)
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                byte[] key = aes.Key;
                byte[] iv;
                try
                {
                    iv = new byte[invalidIvSize];
                }
                catch (OutOfMemoryException) // in case there isn't enough memory at test-time to allocate the large array
                {
                    return;
                }
                Assert.Throws<ArgumentException>("iv", () => aes.CreateEncryptor(key, iv));
                Assert.Throws<ArgumentException>("iv", () => aes.CreateDecryptor(key, iv));
            }
        }

        [Fact]
        public static void VerifyKeyGeneration_Default()
        {
            using (Aes aes = Aes.Create())
            {
                VerifyKeyGeneration(aes);
            }
        }

        [Fact]
        public static void VerifyKeyGeneration_128()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 128;
                VerifyKeyGeneration(aes);
            }
        }

        [Fact]
        public static void VerifyKeyGeneration_192()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 192;
                VerifyKeyGeneration(aes);
            }
        }

        [Fact]
        public static void VerifyKeyGeneration_256()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                VerifyKeyGeneration(aes);
            }
        }

        [Fact]
        public static void VerifyIVGeneration()
        {
            using (Aes aes = Aes.Create())
            {
                int blockSize = aes.BlockSize;
                aes.GenerateIV();

                byte[] iv = aes.IV;

                Assert.NotNull(iv);
                Assert.Equal(blockSize, aes.BlockSize);
                Assert.Equal(blockSize, iv.Length * 8);

                // Standard randomness caveat: There's a very low chance that the generated IV -is-
                // all zeroes.  This works out to 1/2^128, which is more unlikely than 1/10^38.
                Assert.NotEqual(new byte[iv.Length], iv);
            }
        }

        [Fact]
        public static void ValidateEncryptorProperties()
        {
            using (Aes aes = Aes.Create())
            {
                ValidateTransformProperties(aes, aes.CreateEncryptor());
            }
        }

        [Fact]
        public static void ValidateDecryptorProperties()
        {
            using (Aes aes = Aes.Create())
            {
                ValidateTransformProperties(aes, aes.CreateDecryptor());
            }
        }

        [Fact]
        public static void CreateTransformExceptions()
        {
            byte[] key;
            byte[] iv;

            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                key = aes.Key;
                iv = aes.IV;
            }

            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;

                Assert.Throws<ArgumentNullException>(() => aes.CreateEncryptor(null, iv));
                Assert.Throws<ArgumentNullException>(() => aes.CreateEncryptor(null, null));

                Assert.Throws<ArgumentNullException>(() => aes.CreateDecryptor(null, iv));
                Assert.Throws<ArgumentNullException>(() => aes.CreateDecryptor(null, null));

                // CBC requires an IV.
                Assert.Throws<CryptographicException>(() => aes.CreateEncryptor(key, null));

                Assert.Throws<CryptographicException>(() => aes.CreateDecryptor(key, null));
            }

            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;

                Assert.Throws<ArgumentNullException>(() => aes.CreateEncryptor(null, iv));
                Assert.Throws<ArgumentNullException>(() => aes.CreateEncryptor(null, null));

                Assert.Throws<ArgumentNullException>(() => aes.CreateDecryptor(null, iv));
                Assert.Throws<ArgumentNullException>(() => aes.CreateDecryptor(null, null));

                // ECB will accept an IV (but ignore it), and doesn't require it.
                using (ICryptoTransform didNotThrow = aes.CreateEncryptor(key, null))
                {
                    Assert.NotNull(didNotThrow);
                }

                using (ICryptoTransform didNotThrow = aes.CreateDecryptor(key, null))
                {
                    Assert.NotNull(didNotThrow);
                }
            }
        }

        [Fact]
        public static void ValidateOffsetAndCount()
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                // aes.BlockSize is in bits, new byte[] is in bytes, so we have 8 blocks.
                byte[] full = new byte[aes.BlockSize];
                int blockByteCount = aes.BlockSize / 8;

                for (int i = 0; i < full.Length; i++)
                {
                    full[i] = unchecked((byte)i);
                }

                byte[] firstBlock = new byte[blockByteCount];
                byte[] middleHalf = new byte[4 * blockByteCount];

                // Copy the first blockBytes of full into firstBlock.
                Buffer.BlockCopy(full, 0, firstBlock, 0, blockByteCount);

                // [Skip][Skip][Take][Take][Take][Take][Skip][Skip] => "middle half"
                Buffer.BlockCopy(full, 2 * blockByteCount, middleHalf, 0, middleHalf.Length);

                byte[] firstBlockEncrypted;
                byte[] firstBlockEncryptedFromCount;
                byte[] middleHalfEncrypted;
                byte[] middleHalfEncryptedFromOffsetAndCount;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    firstBlockEncrypted = encryptor.TransformFinalBlock(firstBlock, 0, firstBlock.Length);
                }

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    firstBlockEncryptedFromCount = encryptor.TransformFinalBlock(full, 0, firstBlock.Length);
                }

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    middleHalfEncrypted = encryptor.TransformFinalBlock(middleHalf, 0, middleHalf.Length);
                }

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    middleHalfEncryptedFromOffsetAndCount = encryptor.TransformFinalBlock(full, 2 * blockByteCount, middleHalf.Length);
                }

                Assert.Equal(firstBlockEncrypted, firstBlockEncryptedFromCount);
                Assert.Equal(middleHalfEncrypted, middleHalfEncryptedFromOffsetAndCount);
            }
        }

        private static void ValidateTransformProperties(Aes aes, ICryptoTransform transform)
        {
            Assert.NotNull(transform);
            Assert.Equal(aes.BlockSize, transform.InputBlockSize * 8);
            Assert.Equal(aes.BlockSize, transform.OutputBlockSize * 8);
            Assert.True(transform.CanTransformMultipleBlocks);
        }

        private static void VerifyKeyGeneration(Aes aes)
        {
            int keySize = aes.KeySize;
            aes.GenerateKey();

            byte[] key = aes.Key;

            Assert.NotNull(key);
            Assert.Equal(keySize, aes.KeySize);
            Assert.Equal(keySize, key.Length * 8);

            // Standard randomness caveat: There's a very low chance that the generated key -is-
            // all zeroes.  For a 128-bit key this is 1/2^128, which is more unlikely than 1/10^38.
            Assert.NotEqual(new byte[key.Length], key);
        }
    }
}
