// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public class AesModeTests
    {
        [Fact]
        public static void SupportsCBC()
        {
            SupportsMode(CipherMode.CBC);
        }

        [Fact]
        public static void SupportsECB()
        {
            SupportsMode(CipherMode.ECB);
        }

        [Fact]
        public static void DoesNotSupportCTS()
        {
            DoesNotSupportMode(CipherMode.CTS);
        }

        private static void SupportsMode(CipherMode mode)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = mode;
                Assert.Equal(mode, aes.Mode);

                using (ICryptoTransform transform = aes.CreateEncryptor())
                {
                    transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                }
            }
        }

        private static void DoesNotSupportMode(CipherMode mode)
        {
            using (Aes aes = Aes.Create())
            {
                // One of the following should throw:
                // aes.Mode = invalidMode
                // aes.CreateEncryptor() (with an invalid Mode value)
                // transform.Transform[Final]Block() (with an invalid Mode value)

                Assert.Throws<CryptographicException>(
                    () =>
                    {
                        aes.Mode = mode;

                        // If assigning the Mode property did not fail, then it should reflect what we asked for.
                        Assert.Equal(mode, aes.Mode);

                        using (ICryptoTransform transform = aes.CreateEncryptor())
                        {
                            transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        }
                    });
            }
        }
    }
}
