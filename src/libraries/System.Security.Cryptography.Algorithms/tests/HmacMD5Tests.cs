// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class HmacMD5Tests : Rfc2202HmacTests
    {
        private static readonly byte[][] s_testKeys2202 =
        {
            null,
            ByteUtils.RepeatByte(0x0b, 16),
            ByteUtils.AsciiBytes("Jefe"),
            ByteUtils.RepeatByte(0xaa, 16),
            ByteUtils.HexToByteArray("0102030405060708090a0b0c0d0e0f10111213141516171819"),
            ByteUtils.RepeatByte(0x0c, 16),
            ByteUtils.RepeatByte(0xaa, 80),
            ByteUtils.RepeatByte(0xaa, 80),
        };

        private static readonly byte[][] s_testMacs2202 =
        {
            null,
            ByteUtils.HexToByteArray("9294727a3638bb1c13f48ef8158bfc9d"),
            ByteUtils.HexToByteArray("750c783e6ab0b503eaa86e310a5db738"),
            ByteUtils.HexToByteArray("56be34521d144c88dbb8c733f0e8b3f6"),
            ByteUtils.HexToByteArray("697eaf0aca3a3aea3a75164746ffaa79"),
            ByteUtils.HexToByteArray("56461ef2342edc00f9bab995690efd4c"),
            ByteUtils.HexToByteArray("6b1ab7fe4bd7bf8f0b62e6ce61b9d0cd"),
            ByteUtils.HexToByteArray("6f630fad67cda0ee1fb1f562db3aa53e"),
        };

        public HmacMD5Tests()
            : base(s_testKeys2202, s_testMacs2202)
        {
        }

        protected override int BlockSize => 64;
        protected override int MacSize => 16;

        protected override HMAC Create() => new HMACMD5();
        protected override HashAlgorithm CreateHashAlgorithm() => MD5.Create();
        protected override byte[] HashDataOneShot(byte[] key, byte[] source) =>
            HMACMD5.HashData(key, source);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source) =>
            HMACMD5.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination) =>
            HMACMD5.HashData(key, source, destination);

        protected override bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written) =>
            HMACMD5.TryHashData(key, source, destination, out written);

        [Fact]
        public void HmacMD5_Rfc2202_1()
        {
            VerifyHmac(1, s_testMacs2202[1]);
        }

        [Fact]
        public void HmacMD5_Rfc2202_2()
        {
            VerifyHmac(2, s_testMacs2202[2]);
        }

        [Fact]
        public void HmacMD5_Rfc2202_3()
        {
            VerifyHmac(3, s_testMacs2202[3]);
        }

        [Fact]
        public void HmacMD5_Rfc2202_4()
        {
            VerifyHmac(4, s_testMacs2202[4]);
        }

        [Fact]
        public void HmacMD5_Rfc2202_5()
        {
            VerifyHmac(5, s_testMacs2202[5]);
        }

        [Fact]
        public void HmacMD5_Rfc2202_6()
        {
            VerifyHmac(6, s_testMacs2202[6]);
        }

        [Fact]
        public void HmacMD5_Rfc2202_7()
        {
            VerifyHmac(7, s_testMacs2202[7]);
        }

        [Fact]
        public void HMacMD5_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [Fact]
        public void HMacMD5_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACMD5(null));
        }
    }
}
