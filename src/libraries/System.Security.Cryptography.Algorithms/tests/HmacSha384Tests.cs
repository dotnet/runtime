// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class HmacSha384Tests : Rfc4231HmacTests
    {
        protected override HMAC Create()
        {
            return new HMACSHA384();
        }

        protected override HashAlgorithm CreateHashAlgorithm()
        {
            return SHA384.Create();
        }

        protected override int BlockSize { get { return 128; } }

        [Fact]
        public void ProduceLegacyHmacValues()
        {
            using (var h = new HMACSHA384())
            {
                Assert.False(h.ProduceLegacyHmacValues);
                h.ProduceLegacyHmacValues = false; // doesn't throw
                Assert.Throws<PlatformNotSupportedException>(() => h.ProduceLegacyHmacValues = true);
            }
        }

        [Fact]
        public void HmacSha384_Rfc4231_1()
        {
            VerifyHmac(1, "afd03944d84895626b0825f4ab46907f15f9dadbe4101ec682aa034c7cebc59cfaea9ea9076ede7f4af152e8b2fa9cb6");
        }

        [Fact]
        public void HmacSha384_Rfc4231_2()
        {
            VerifyHmac(2, "af45d2e376484031617f78d2b58a6b1b9c7ef464f5a01b47e42ec3736322445e8e2240ca5e69e2c78b3239ecfab21649");
        }

        [Fact]
        public void HmacSha384_Rfc4231_3()
        {
            VerifyHmac(3, "88062608d3e6ad8a0aa2ace014c8a86f0aa635d947ac9febe83ef4e55966144b2a5ab39dc13814b94e3ab6e101a34f27");
        }

        [Fact]
        public void HmacSha384_Rfc4231_4()
        {
            VerifyHmac(4, "3e8a69b7783c25851933ab6290af6ca77a9981480850009cc5577c6e1f573b4e6801dd23c4a7d679ccf8a386c674cffb");
        }

        [Fact]
        public void HmacSha384_Rfc4231_5()
        {
            // RFC 4231 only defines the first 128 bits of this value.
            VerifyHmac(5, "3abf34c3503b2a23a46efc619baef897", 128 / 8);
        }

        [Fact]
        public void HmacSha384_Rfc4231_6()
        {
            VerifyHmac(6, "4ece084485813e9088d2c63a041bc5b44f9ef1012a2b588f3cd11f05033ac4c60c2ef6ab4030fe8296248df163f44952");
        }

        [Fact]
        public void HmacSha384_Rfc4231_7()
        {
            VerifyHmac(7, "6617178e941f020d351e2f254e8fd32c602420feb0b8fb9adccebb82461e99c5a678cc31e799176d3860e6110c46523e");
        }

        [Fact]
        public void HMacSha384_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [Fact]
        public void HmacSha384_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACSHA384(null));
        }
    }
}
