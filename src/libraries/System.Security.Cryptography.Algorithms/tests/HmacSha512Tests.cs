// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class HmacSha512Tests : Rfc4231HmacTests
    {
        protected override HMAC Create()
        {
            return new HMACSHA512();
        }

        protected override HashAlgorithm CreateHashAlgorithm()
        {
            return SHA512.Create();
        }

        protected override int BlockSize { get { return 128; } }

        [Fact]
        public void ProduceLegacyHmacValues()
        {
            using (var h = new HMACSHA512())
            {
                Assert.False(h.ProduceLegacyHmacValues);
                h.ProduceLegacyHmacValues = false; // doesn't throw
                Assert.Throws<PlatformNotSupportedException>(() => h.ProduceLegacyHmacValues = true);
            }
        }

        [Fact]
        public void HmacSha512_Rfc4231_1()
        {
            VerifyHmac(1, "87aa7cdea5ef619d4ff0b4241a1d6cb02379f4e2ce4ec2787ad0b30545e17cdedaa833b7d6b8a702038b274eaea3f4e4be9d914eeb61f1702e696c203a126854");
        }

        [Fact]
        public void HmacSha512_Rfc4231_2()
        {
            VerifyHmac(2, "164b7a7bfcf819e2e395fbe73b56e0a387bd64222e831fd610270cd7ea2505549758bf75c05a994a6d034f65f8f0e6fdcaeab1a34d4a6b4b636e070a38bce737");
        }

        [Fact]
        public void HmacSha512_Rfc4231_3()
        {
            VerifyHmac(3, "fa73b0089d56a284efb0f0756c890be9b1b5dbdd8ee81a3655f83e33b2279d39bf3e848279a722c806b485a47e67c807b946a337bee8942674278859e13292fb");
        }

        [Fact]
        public void HmacSha512_Rfc4231_4()
        {
            VerifyHmac(4, "b0ba465637458c6990e5a8c5f61d4af7e576d97ff94b872de76f8050361ee3dba91ca5c11aa25eb4d679275cc5788063a5f19741120c4f2de2adebeb10a298dd");
        }

        [Fact]
        public void HmacSha512_Rfc4231_5()
        {
            // RFC 4231 only defines the first 128 bits of this value.
            VerifyHmac(5, "415fad6271580a531d4179bc891d87a6", 128 / 8);
        }

        [Fact]
        public void HmacSha512_Rfc4231_6()
        {
            VerifyHmac(6, "80b24263c7c1a3ebb71493c1dd7be8b49b46d1f41b4aeec1121b013783f8f3526b56d037e05f2598bd0fd2215d6a1e5295e64f73f63f0aec8b915a985d786598");
        }

        [Fact]
        public void HmacSha512_Rfc4231_7()
        {
            VerifyHmac(7, "e37b6a775dc87dbaa4dfa9f96e5e3ffddebd71f8867289865df5a32d20cdc944b6022cac3c4982b10d5eeb55c3e4de15134676fb6de0446065c97440fa8c6a58");
        }

        [Fact]
        public void HMacSha512_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [Fact]
        public void HmacSha512_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACSHA512(null));
        }
    }
}
