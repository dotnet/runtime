// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    [SkipOnMono("Not supported on Browser", TestPlatforms.Browser)]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        public static void SupportsCFB()
        {
            SupportsMode(CipherMode.CFB);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows7))]
        public static void Windows7DoesNotSupportCFB()
        {
            DoesNotSupportMode(CipherMode.CFB);
        }

        [Fact]
        public static void DoesNotSupportCTS()
        {
            DoesNotSupportMode(CipherMode.CTS);
        }

        private static void SupportsMode(CipherMode mode)
        {
            using (Aes aes = AesFactory.Create())
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
            using (Aes aes = AesFactory.Create())
            {
                // One of the following should throw:
                // aes.Mode = invalidMode
                // aes.CreateEncryptor() (with an invalid Mode value)
                // transform.Transform[Final]Block() (with an invalid Mode value)

                Assert.ThrowsAny<CryptographicException>(
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
