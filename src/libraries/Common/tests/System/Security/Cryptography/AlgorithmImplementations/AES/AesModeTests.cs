// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class AesModeTests
    {
        protected abstract AesProvider AesFactory { get; }

        [Fact]
        public void SupportsCBC()
        {
            SupportsMode(CipherMode.CBC);
        }

        [Fact]
        public void SupportsECB()
        {
            SupportsMode(CipherMode.ECB);
        }

        [Fact]
        public void SupportsCFB8()
        {
            SupportsMode(CipherMode.CFB, feedbackSize: 8);
        }

        [Fact]
        public void SupportsCFB128()
        {
            SupportsMode(CipherMode.CFB, feedbackSize: 128);
        }

        [Fact]
        public void DoesNotSupportCTS()
        {
            DoesNotSupportMode(CipherMode.CTS);
        }

        private void SupportsMode(CipherMode mode, int? feedbackSize = null)
        {
            using (Aes aes = AesFactory.Create())
            {
                aes.Mode = mode;
                Assert.Equal(mode, aes.Mode);

                if (feedbackSize.HasValue)
                {
                    aes.FeedbackSize = feedbackSize.Value;
                }

                using (ICryptoTransform transform = aes.CreateEncryptor())
                {
                    transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                }
            }
        }

        private void DoesNotSupportMode(CipherMode mode, int? feedbackSize = null)
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

                        if (feedbackSize.HasValue)
                        {
                            aes.FeedbackSize = feedbackSize.Value;
                        }

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
