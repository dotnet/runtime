// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class HmacMD5Tests : Rfc2202HmacTests<HmacMD5Tests.Traits>
    {
        public sealed class Traits : IHmacTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => HMACMD5.HashSizeInBytes;
        }

        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.MD5;

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
        protected override int MacSize => HMACMD5.HashSizeInBytes;

        protected override HMAC Create() => new HMACMD5();
        protected override HMAC Create(byte[] key) => new HMACMD5(key);
        protected override HashAlgorithm CreateHashAlgorithm() => MD5.Create();
        protected override byte[] HashDataOneShot(byte[] key, byte[] source) =>
            HMACMD5.HashData(key, source);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source) =>
            HMACMD5.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination) =>
            HMACMD5.HashData(key, source, destination);

        protected override bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written) =>
            HMACMD5.TryHashData(key, source, destination, out written);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, Stream source) =>
            HMACMD5.HashData(key, source);

        protected override byte[] HashDataOneShot(byte[] key, Stream source) =>
            HMACMD5.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, Stream source, Span<byte> destination) =>
            HMACMD5.HashData(key, source, destination);

        protected override ValueTask<int> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken) => HMACMD5.HashDataAsync(key, source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken) => HMACMD5.HashDataAsync(key, source, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            byte[] key,
            Stream source,
            CancellationToken cancellationToken) => HMACMD5.HashDataAsync(key, source, cancellationToken);

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

        [Fact]
        public void HMacMD5_EmptyKey()
        {
            VerifyRepeating(
                input: "Crypto is fun!",
                1,
                hexKey: "",
                output: "7554A8C4641CBA36BE2AC20CACEA1136");
        }

        [Fact]
        public void HmacMD5_Stream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl md5 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "1287EF250C2026A0C0CBA832C599AE50");
        }

        [Fact]
        public void HmacMD5_Stream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl md5 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "D10B835D95FCC9EECDF1D4BCDAB81897");
        }

        [Fact]
        public void HmacMD5_Stream_Empty()
        {
            // Verfied with:
            // echo -n "" | openssl md5 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "C91E40247251F39BDFE6A7B72A5857F9");
        }

        [Fact]
        public async Task HmacMD5_Stream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl md5 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "1287EF250C2026A0C0CBA832C599AE50");
        }

        [Fact]
        public async Task HmacMD5_Stream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl md5 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "D10B835D95FCC9EECDF1D4BCDAB81897");
        }

        [Fact]
        public async Task HmacMD5_Stream_Empty_Async()
        {
            // Verfied with:
            // echo -n "" | openssl md5 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "C91E40247251F39BDFE6A7B72A5857F9");
        }

        [Fact]
        public void HmacMD5_HashSizes()
        {
            Assert.Equal(128, HMACMD5.HashSizeInBits);
            Assert.Equal(16, HMACMD5.HashSizeInBytes);
        }
    }
}
