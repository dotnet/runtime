// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor());

                aes.Mode = CipherMode.CFB;
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor());
            }
        }

        [Fact]
        public static void AesThrows_PlatformNotSupported_PaddingMode_Browser()
        {
            using (Aes aes = Aes.Create())
            {
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptCbc(s_plainText, s_iv, PaddingMode.None));
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptCbc(s_plainText, s_iv, PaddingMode.Zeros));
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptCbc(s_plainText, s_iv, PaddingMode.ANSIX923));
                Assert.Throws<PlatformNotSupportedException>(() => aes.EncryptCbc(s_plainText, s_iv, PaddingMode.ISO10126));

                aes.Padding = PaddingMode.None;
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor());

                aes.Padding = PaddingMode.Zeros;
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor());

                aes.Padding = PaddingMode.ANSIX923;
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor());

                aes.Padding = PaddingMode.ISO10126;
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateEncryptor());
                Assert.Throws<PlatformNotSupportedException>(() => aes.CreateDecryptor());
            }
        }
    }
}
