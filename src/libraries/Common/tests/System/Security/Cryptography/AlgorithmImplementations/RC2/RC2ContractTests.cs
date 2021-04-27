// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class RC2ContractTests
    {
        [Theory]
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
        public static void InvalidCFBFeedbackSizes(int feedbackSize, bool discoverableInSetter)
        {
            using (RC2 rc2 = RC2Factory.Create())
            {
                rc2.GenerateKey();
                rc2.Mode = CipherMode.CFB;

                if (discoverableInSetter)
                {
                    // there are some key sizes that are invalid for any of the modes,
                    // so the exception is thrown in the setter
                    Assert.Throws<CryptographicException>(() =>
                    {
                        rc2.FeedbackSize = feedbackSize;
                    });
                }
                else
                {
                    rc2.FeedbackSize = feedbackSize;

                    // however, for CFB only few sizes are valid. Those should throw in the
                    // actual RC2 instantiation.

                    Assert.Throws<CryptographicException>(() => rc2.CreateDecryptor());
                    Assert.Throws<CryptographicException>(() => rc2.CreateEncryptor());
                }
            }
        }
    }
}
