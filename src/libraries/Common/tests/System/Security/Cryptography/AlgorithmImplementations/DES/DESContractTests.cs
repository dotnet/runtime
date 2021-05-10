// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class DesContractTests
    {
        // cfb not available on windows 7
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows7))]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(7, true)]
        [InlineData(9, true)]
        [InlineData(-1, true)]
        [InlineData(int.MaxValue, true)]
        [InlineData(int.MinValue, true)]
        [InlineData(8, false)]
        [InlineData(64, false)]
        [InlineData(256, true)]
        [InlineData(128, true)]
        [InlineData(127, true)]
        public static void Windows7DoesNotSupportCFB(int feedbackSize, bool discoverableInSetter)
        {
            using (DES des = DESFactory.Create())
            {
                des.GenerateKey();
                des.Mode = CipherMode.CFB;

                if (discoverableInSetter)
                {
                    // there are some key sizes that are invalid for any of the modes,
                    // so the exception is thrown in the setter
                    Assert.ThrowsAny<CryptographicException>(() =>
                    {
                        des.FeedbackSize = feedbackSize;
                    });
                }
                else
                {
                    des.FeedbackSize = feedbackSize;

                    // however, for CFB only few sizes are valid. Those should throw in the
                    // actual DES instantiation.

                    Assert.ThrowsAny<CryptographicException>(() =>
                    {
                        return des.CreateDecryptor();
                    });
                    Assert.ThrowsAny<CryptographicException>(() =>
                    {
                        return des.CreateEncryptor();
                    });
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(7, true)]
        [InlineData(9, true)]
        [InlineData(-1, true)]
        [InlineData(int.MaxValue, true)]
        [InlineData(int.MinValue, true)]
        [InlineData(64, false)]
        [InlineData(256, true)]
        [InlineData(128, true)]
        [InlineData(127, true)]
        public static void InvalidCFBFeedbackSizes(int feedbackSize, bool discoverableInSetter)
        {
            using (DES des = DESFactory.Create())
            {
                des.GenerateKey();
                des.Mode = CipherMode.CFB;

                if (discoverableInSetter)
                {
                    // there are some key sizes that are invalid for any of the modes,
                    // so the exception is thrown in the setter
                    Assert.Throws<CryptographicException>(() =>
                    {
                        des.FeedbackSize = feedbackSize;
                    });
                }
                else
                {
                    des.FeedbackSize = feedbackSize;

                    // however, for CFB only few sizes are valid. Those should throw in the
                    // actual DES instantiation.

                    Assert.Throws<CryptographicException>(() => des.CreateDecryptor());
                    Assert.Throws<CryptographicException>(() => des.CreateEncryptor());
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows7))]
        [InlineData(8)]
        public static void ValidCFBFeedbackSizes(int feedbackSize)
        {
            using (DES des = DESFactory.Create())
            {
                des.GenerateKey();
                des.Mode = CipherMode.CFB;

                des.FeedbackSize = feedbackSize;

                using var decryptor = des.CreateDecryptor();
                using var encryptor = des.CreateEncryptor();
                Assert.NotNull(decryptor);
                Assert.NotNull(encryptor);
            }
        }
    }
}
