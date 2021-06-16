// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class TripleDESContractTests
    {

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows7))]
        public static void Windows7DoesNotSupportCFB64()
        {
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.GenerateKey();
                tdes.Mode = CipherMode.CFB;
                tdes.FeedbackSize = 64;

                Assert.ThrowsAny<CryptographicException>(() => tdes.CreateDecryptor());
                Assert.ThrowsAny<CryptographicException>(() => tdes.CreateEncryptor());
            }
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(7, true)]
        [InlineData(9, true)]
        [InlineData(-1, true)]
        [InlineData(int.MaxValue, true)]
        [InlineData(int.MinValue, true)]
        [InlineData(256, true)]
        [InlineData(128, true)]
        [InlineData(127, true)]
        public static void InvalidCFBFeedbackSizes(int feedbackSize, bool discoverableInSetter)
        {
            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.GenerateKey();
                tdes.Mode = CipherMode.CFB;

                if (discoverableInSetter)
                {
                    // there are some key sizes that are invalid for any of the modes,
                    // so the exception is thrown in the setter
                    Assert.Throws<CryptographicException>(() =>
                    {
                        tdes.FeedbackSize = feedbackSize;
                    });
                }
                else
                {
                    tdes.FeedbackSize = feedbackSize;

                    // however, for CFB only few sizes are valid. Those should throw in the
                    // actual AES instantiation.

                    Assert.Throws<CryptographicException>(() => tdes.CreateDecryptor());
                    Assert.Throws<CryptographicException>(() => tdes.CreateEncryptor());
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(8)]
        [InlineData(64)]
        public static void ValidCFBFeedbackSizes(int feedbackSize)
        {
            // Windows 7 only supports CFB8.
            if (feedbackSize != 8 && PlatformDetection.IsWindows7)
            {
                return;
            }

            using (TripleDES tdes = TripleDESFactory.Create())
            {
                tdes.GenerateKey();
                tdes.Mode = CipherMode.CFB;

                tdes.FeedbackSize = feedbackSize;

                using var decryptor = tdes.CreateDecryptor();
                using var encryptor = tdes.CreateEncryptor();
                Assert.NotNull(decryptor);
                Assert.NotNull(encryptor);
            }
        }
    }
}
