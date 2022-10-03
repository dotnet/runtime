// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public partial class AesTests
    {
        private static byte[] s_plainText = new byte[] { 0x50, 0x68, 0x12, 0xA4, 0x5F, 0x08, 0xC8, 0x89, 0xB9, 0x7F, 0x59, 0x80, 0x03, 0x8B, 0x83, 0x59 };
        private static byte[] s_iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static byte[] s_destination = new byte[s_plainText.Length];

        [Fact]
        public static void AesThrows_PlatformNotSupported_CipherMode_Browser()
        {
            using (Aes aes = Aes.Create())
            {
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptEcb(s_plainText, PaddingMode.PKCS7));
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptEcb(s_plainText.AsSpan(), PaddingMode.PKCS7));
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptEcb(s_plainText.AsSpan(), s_destination, PaddingMode.PKCS7));
                Assert.Throws<PlatformNotSupportedException>(() => aes.DecryptEcb(s_plainText, PaddingMode.PKCS7));
                Assert.Throws<PlatformNotSupportedException>(() => aes.DecryptEcb(s_plainText.AsSpan(), PaddingMode.PKCS7));
                Assert.Throws<PlatformNotSupportedException>(() => aes.DecryptEcb(s_plainText.AsSpan(), s_destination, PaddingMode.PKCS7));

                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptCfb(s_plainText, s_iv));
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptCfb(s_plainText.AsSpan(), s_iv.AsSpan()));
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptCfb(s_plainText.AsSpan(), s_iv, s_destination));
                Assert.Throws<PlatformNotSupportedException>(() => aes.DecryptCfb(s_plainText, s_iv));
                Assert.Throws<PlatformNotSupportedException>(() => aes.DecryptCfb(s_plainText.AsSpan(), s_iv.AsSpan()));
                Assert.Throws<PlatformNotSupportedException>(() => aes.DecryptCfb(s_plainText.AsSpan(), s_iv, s_destination));

                aes.Mode = CipherMode.ECB;
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor(s_iv, s_iv));
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor(s_iv, s_iv));

                aes.Mode = CipherMode.CFB;
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor(s_iv, s_iv));
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor(s_iv, s_iv));
            }
        }

        // Browser's SubtleCrypto doesn't support AES-192
        [Fact]
        public static void Aes_InvalidKeySize_192_Browser()
        {
            byte[] key192 = new byte[192 / 8];
            using (Aes aes = Aes.Create())
            {
                Assert.False(aes.ValidKeySize(192));
                Assert.Throws<CryptographicException>(() => aes.Key = key192);
                Assert.Throws<CryptographicException>(() => aes.KeySize = 192);
                Assert.Throws<ArgumentException>(() => aes.CreateEncryptor(key192, s_iv));
                Assert.Throws<ArgumentException>(() => aes.CreateDecryptor(key192, s_iv));
            }
        }

        [Fact]
        public static void EnsureSubtleCryptoIsUsed()
        {
            bool canUseSubtleCrypto = (bool)Type.GetType("Interop+BrowserCrypto, System.Security.Cryptography")
                .GetField("CanUseSubtleCrypto", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

            bool expectedCanUseSubtleCrypto = Environment.GetEnvironmentVariable("TEST_EXPECT_SUBTLE_CRYPTO") == "true";

            Assert.Equal(expectedCanUseSubtleCrypto, canUseSubtleCrypto);
        }
    }
}
