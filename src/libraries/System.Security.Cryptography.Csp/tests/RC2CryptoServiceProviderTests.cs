// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using Csp.Tests;
    using RC2 = System.Security.Cryptography.RC2;

    public static partial class RC2CryptoServiceProviderTests
    {
        [Fact]
        public static void RC2KeySize()
        {
            using (RC2 rc2 = new RC2CryptoServiceProvider())
            {
                Assert.Equal(128, rc2.KeySize);

                rc2.KeySize = 40;
                Assert.Equal(40 / 8, rc2.Key.Length);
                Assert.Equal(40, rc2.KeySize);

                rc2.KeySize = 128;
                Assert.Equal(128 / 8, rc2.Key.Length);
                Assert.Equal(128, rc2.KeySize);

                Assert.Throws<CryptographicException>(() => rc2.KeySize = 40 - 8);
                Assert.Throws<CryptographicException>(() => rc2.KeySize = 128 + 8);
            }
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void UseSalt_Success()
        {
            using (var rc2 = new RC2CryptoServiceProvider())
            {
                Assert.False(rc2.UseSalt);
                rc2.UseSalt = true;
                Assert.True(rc2.UseSalt);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void UseSalt_Throws_Unix()
        {
            using (var rc2 = new RC2CryptoServiceProvider())
            {
                rc2.UseSalt = rc2.UseSalt; // Ensure we can assign false
                Assert.False(rc2.UseSalt);
                Assert.Throws<PlatformNotSupportedException>(() => (rc2.UseSalt = true));
            }
        }

        [Fact]
        public static void TestShimProperties()
        {
            // Test the Unix shims; but also run on Windows to ensure behavior is consistent.
            using (var alg = new RC2CryptoServiceProvider())
            {
                ShimHelpers.TestSymmetricAlgorithmProperties(alg, blockSize: 64, keySize: 128);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Only Unix has _impl shim pattern
        public static void TestShimOverrides_Unix()
        {
            ShimHelpers.VerifyAllBaseMembersOverridden(typeof(RC2CryptoServiceProvider));
        }
    }
}
