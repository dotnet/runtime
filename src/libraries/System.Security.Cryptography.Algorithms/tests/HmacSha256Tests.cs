// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class HmacSha256Tests : Rfc4231HmacTests
    {
        protected override int BlockSize => 64;
        protected override int MacSize => 32;

        protected override HMAC Create() => new HMACSHA256();
        protected override HashAlgorithm CreateHashAlgorithm() => SHA256.Create();
        protected override byte[] HashDataOneShot(byte[] key, byte[] source) =>
            HMACSHA256.HashData(key, source);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source) =>
            HMACSHA256.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination) =>
            HMACSHA256.HashData(key, source, destination);

        protected override bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written) =>
            HMACSHA256.TryHashData(key, source, destination, out written);

        private static byte[][] s_testMacs4231 =
        {
            null,
            ByteUtils.HexToByteArray("b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7"),
            ByteUtils.HexToByteArray("5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843"),
            ByteUtils.HexToByteArray("773ea91e36800e46854db8ebd09181a72959098b3ef8c122d9635514ced565fe"),
            ByteUtils.HexToByteArray("82558a389a443c0ea4cc819899f2083a85f0faa3e578f8077a2e3ff46729665b"),
            // RFC 4231 only defines the first 128 bits of this value.
            ByteUtils.HexToByteArray("a3b6167473100ee06e0c796c2955552b"),
            ByteUtils.HexToByteArray("60e431591ee0b67f0d8a26aacbf5b77f8e0bc6213728c5140546040f0ee37f54"),
            ByteUtils.HexToByteArray("9b09ffa71b942fcb27635fbcd5b0e944bfdc63644f0713938a7f51535c3a35e2"),
        };

        public HmacSha256Tests() : base(s_testMacs4231)
        {
        }

        [Fact]
        public void HmacSha256_Rfc4231_1()
        {
            VerifyHmac(1, s_testMacs4231[1]);
        }

        [Fact]
        public void HmacSha256_Rfc4231_2()
        {
            VerifyHmac(2, s_testMacs4231[2]);
        }

        [Fact]
        public void HmacSha256_Rfc4231_3()
        {
            VerifyHmac(3, s_testMacs4231[3]);
        }

        [Fact]
        public void HmacSha256_Rfc4231_4()
        {
            VerifyHmac(4, s_testMacs4231[4]);
        }

        [Fact]
        public void HmacSha256_Rfc4231_5()
        {
            VerifyHmac(5, s_testMacs4231[5]);
        }

        [Fact]
        public void HmacSha256_Rfc4231_6()
        {
            VerifyHmac(6, s_testMacs4231[6]);
        }

        [Fact]
        public void HmacSha256_Rfc4231_7()
        {
            VerifyHmac(7, s_testMacs4231[7]);
        }

        [Fact]
        public void HMacSha256_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [Fact]
        public void HmacSha256_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACSHA256(null));
        }
    }
}
