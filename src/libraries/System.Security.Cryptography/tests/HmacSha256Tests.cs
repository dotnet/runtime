// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class HmacSha256Tests : Rfc4231HmacTests<HmacSha256Tests.Traits>
    {
        public sealed class Traits : IHmacTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => HMACSHA256.HashSizeInBytes;
        }

        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA256;

        protected override int BlockSize => 64;
        protected override int MacSize => HMACSHA256.HashSizeInBytes;

        protected override HMAC Create() => new HMACSHA256();
        protected override HMAC Create(byte[] key) => new HMACSHA256(key);
        protected override HashAlgorithm CreateHashAlgorithm() => SHA256.Create();
        protected override byte[] HashDataOneShot(byte[] key, byte[] source) =>
            HMACSHA256.HashData(key, source);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source) =>
            HMACSHA256.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination) =>
            HMACSHA256.HashData(key, source, destination);

        protected override bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written) =>
            HMACSHA256.TryHashData(key, source, destination, out written);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, Stream source) =>
            HMACSHA256.HashData(key, source);

        protected override byte[] HashDataOneShot(byte[] key, Stream source) =>
            HMACSHA256.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, Stream source, Span<byte> destination) =>
            HMACSHA256.HashData(key, source, destination);

        protected override ValueTask<int> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken) => HMACSHA256.HashDataAsync(key, source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA256.HashDataAsync(key, source, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            byte[] key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA256.HashDataAsync(key, source, cancellationToken);

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
        public void HmacSha256_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [Fact]
        public void HmacSha256_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACSHA256(null));
        }

        [Fact]
        public void HmacSha256_EmptyKey()
        {
            VerifyRepeating(
                input: "Crypto is fun!",
                1,
                hexKey: "",
                output: "DE26DD5A23A91021F61EACF8A8DD324AB5637977486A10D701C4DFA4AE33CB4F");
        }

        [Fact]
        public void HmacSha256_Stream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "A47B9F5BD5C2DEF403A0279D4C6C407A2D34561E7D1F006D7FE8BDC2C78227D5");
        }

        [Fact]
        public void HmacSha256_Stream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "1CF6661B6EFD25D8B6DE734AA39D5D3D44C9A56BB9377A6388EB0FC5E48A108B");
        }

        [Fact]
        public void HmacSha256_Stream_Empty()
        {
            // Verfied with:
            // echo -n "" | openssl sha256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "07EFF8B326B7798C9CCFCBDBE579489AC785A7995A04618B1A2813C26744777D");
        }

        [Fact]
        public async Task HmacSha256_Stream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "A47B9F5BD5C2DEF403A0279D4C6C407A2D34561E7D1F006D7FE8BDC2C78227D5");
        }

        [Fact]
        public async Task HmacSha256_Stream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "1CF6661B6EFD25D8B6DE734AA39D5D3D44C9A56BB9377A6388EB0FC5E48A108B");
        }

        [Fact]
        public async Task HmacSha256_Stream_Empty_Async()
        {
            // Verfied with:
            // echo -n "" | openssl sha256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "07EFF8B326B7798C9CCFCBDBE579489AC785A7995A04618B1A2813C26744777D");
        }

        [Fact]
        public void HmacSha256_HashSizes()
        {
            Assert.Equal(256, HMACSHA256.HashSizeInBits);
            Assert.Equal(32, HMACSHA256.HashSizeInBytes);
        }
    }
}
