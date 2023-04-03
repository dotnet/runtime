// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class DecryptorReusability
    {
        // See https://github.com/dotnet/runtime/issues/21354 for details
        [ConditionalFact(nameof(ShouldDecryptorBeReusable))]
        public static void TestDecryptorReusability()
        {
            byte[] expectedPlainText = new byte[]
            {
                0x14, 0x30, 0x71, 0xad, 0xed, 0x8e, 0x58, 0x84,
                0x81, 0x6f, 0x01, 0xa2, 0x0b, 0xea, 0x9e, 0x8b,
                0x14, 0x0f, 0x0f, 0x10, 0x11, 0xb5, 0x22, 0x3d,
                0x79, 0x58, 0x77, 0x17, 0xff, 0xd9, 0xec, 0x3a,
            };

            byte[] cipher = new byte[expectedPlainText.Length];
            byte[] output1 = new byte[expectedPlainText.Length];
            byte[] output2 = new byte[expectedPlainText.Length];

            using (Aes aes = AesFactory.Create())
            {
                aes.Key = new byte[16];
                aes.IV = new byte[] { 0x00, 0x3f, 0x7e, 0xbd, 0xfc, 0x3b, 0x7a, 0xb9, 0xf8, 0x37, 0x76, 0xb5, 0xf4, 0x33, 0x72, 0xb1 };
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    if (!decryptor.CanReuseTransform)
                    {
                        return;
                    }

                    int len = decryptor.TransformBlock(cipher, 0, cipher.Length, output1, 0);
                    byte[] remainder = decryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                    if (len != cipher.Length)
                    {
                        Assert.NotNull(remainder);
                        Assert.Equal(cipher.Length - len, remainder.Length);
                        Buffer.BlockCopy(remainder, 0, output1, len, remainder.Length);
                    }

                    Assert.Equal(expectedPlainText, output1);

                    // Decryptor is now re-initialized, because TransformFinalBlock was called.
                    len = decryptor.TransformBlock(cipher, 0, cipher.Length, output2, 0);
                    remainder = decryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                    if (len != cipher.Length)
                    {
                        Assert.NotNull(remainder);
                        Assert.Equal(cipher.Length - len, remainder.Length);
                        Buffer.BlockCopy(remainder, 0, output2, len, remainder.Length);
                    }

                    Assert.Equal(expectedPlainText, output2);
                }
            }
        }

        private static bool ShouldDecryptorBeReusable()
        {
            if (!PlatformDetection.IsNetFramework)
                return true;

            bool doNotResetDecryptor;
            if (AppContext.TryGetSwitch("Switch.System.Security.Cryptography.AesCryptoServiceProvider.DontCorrectlyResetDecryptor", out doNotResetDecryptor))
            {
                return !doNotResetDecryptor;
            }

            return false;
        }
    }
}
