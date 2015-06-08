// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public static class DecryptorReusabilty
    {
        [Fact]
        [ActiveIssue(1965, PlatformID.AnyUnix)]
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

            using (Aes aes = Aes.Create())
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

                    decryptor.TransformBlock(cipher, 0, cipher.Length, output1, 0);
                    decryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                    Assert.Equal(expectedPlainText, output1);

                    // Decryptor is now re-initialized, because TransformFinalBlock was called.
                    decryptor.TransformBlock(cipher, 0, cipher.Length, output2, 0);
                    decryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                    Assert.Equal(expectedPlainText, output2);
                }
            }
        }
    }
}
