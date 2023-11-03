// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class HmacSha384Tests : Rfc4231HmacTests<HmacSha384Tests.Traits>
    {
        public sealed class Traits : IHmacTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => HMACSHA384.HashSizeInBytes;
        }


        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA384;
        protected override int BlockSize => 128;
        protected override int MacSize => HMACSHA384.HashSizeInBytes;

        protected override HMAC Create() => new HMACSHA384();
        protected override HMAC Create(byte[] key) => new HMACSHA384(key);
        protected override HashAlgorithm CreateHashAlgorithm() => SHA384.Create();
        protected override byte[] HashDataOneShot(byte[] key, byte[] source) =>
            HMACSHA384.HashData(key, source);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source) =>
            HMACSHA384.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination) =>
            HMACSHA384.HashData(key, source, destination);

        protected override bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written) =>
            HMACSHA384.TryHashData(key, source, destination, out written);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, Stream source) =>
            HMACSHA384.HashData(key, source);

        protected override byte[] HashDataOneShot(byte[] key, Stream source) =>
            HMACSHA384.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, Stream source, Span<byte> destination) =>
            HMACSHA384.HashData(key, source, destination);

        protected override ValueTask<int> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken) => HMACSHA384.HashDataAsync(key, source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA384.HashDataAsync(key, source, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            byte[] key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA384.HashDataAsync(key, source, cancellationToken);

        private static byte[][] s_testMacs4231 =
        {
            null,
            ByteUtils.HexToByteArray("afd03944d84895626b0825f4ab46907f15f9dadbe4101ec682aa034c7cebc59cfaea9ea9076ede7f4af152e8b2fa9cb6"),
            ByteUtils.HexToByteArray("af45d2e376484031617f78d2b58a6b1b9c7ef464f5a01b47e42ec3736322445e8e2240ca5e69e2c78b3239ecfab21649"),
            ByteUtils.HexToByteArray("88062608d3e6ad8a0aa2ace014c8a86f0aa635d947ac9febe83ef4e55966144b2a5ab39dc13814b94e3ab6e101a34f27"),
            ByteUtils.HexToByteArray("3e8a69b7783c25851933ab6290af6ca77a9981480850009cc5577c6e1f573b4e6801dd23c4a7d679ccf8a386c674cffb"),
            // RFC 4231 only defines the first 128 bits of this value.
            ByteUtils.HexToByteArray("3abf34c3503b2a23a46efc619baef897"),
            ByteUtils.HexToByteArray("4ece084485813e9088d2c63a041bc5b44f9ef1012a2b588f3cd11f05033ac4c60c2ef6ab4030fe8296248df163f44952"),
            ByteUtils.HexToByteArray("6617178e941f020d351e2f254e8fd32c602420feb0b8fb9adccebb82461e99c5a678cc31e799176d3860e6110c46523e"),
        };

        public HmacSha384Tests() : base(s_testMacs4231)
        {
        }

        [Fact]
        public void ProduceLegacyHmacValues()
        {
            using (var h = new HMACSHA384())
            {
#pragma warning disable SYSLIB0029 // ProduceLegacyHmacValues is obsolete
                Assert.False(h.ProduceLegacyHmacValues);
                h.ProduceLegacyHmacValues = false; // doesn't throw
                Assert.Throws<PlatformNotSupportedException>(() => h.ProduceLegacyHmacValues = true);
#pragma warning restore SYSLIB0029
            }
        }

        [Fact]
        public void HmacSha384_Rfc4231_1()
        {
            VerifyHmac(1, s_testMacs4231[1]);
        }

        [Fact]
        public void HmacSha384_Rfc4231_2()
        {
            VerifyHmac(2, s_testMacs4231[2]);
        }

        [Fact]
        public void HmacSha384_Rfc4231_3()
        {
            VerifyHmac(3, s_testMacs4231[3]);
        }

        [Fact]
        public void HmacSha384_Rfc4231_4()
        {
            VerifyHmac(4, s_testMacs4231[4]);
        }

        [Fact]
        public void HmacSha384_Rfc4231_5()
        {
            VerifyHmac(5, s_testMacs4231[5]);
        }

        [Fact]
        public void HmacSha384_Rfc4231_6()
        {
            VerifyHmac(6, s_testMacs4231[6]);
        }

        [Fact]
        public void HmacSha384_Rfc4231_7()
        {
            VerifyHmac(7, s_testMacs4231[7]);
        }

        [Fact]
        public void HmacSha384_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [Fact]
        public void HmacSha384_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACSHA384(null));
        }

        [Fact]
        public void HmacSha384_EmptyKey()
        {
            VerifyRepeating(
                input: "Crypto is fun!",
                1,
                hexKey: "",
                output: "CFEB81812C8DB4EDB385FCC7CB81E4D715685741AAB1E470FB0B395A414F89867E510E4A2BA2F1F11D7005849FA0DF11");
        }

        [Fact]
        public void HmacSha384_Stream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "6C4977B00CCE179ADCC06EB35E48ABDE9B5604FC9E8B6B25F65E0234EE6394DE40F0C6C6B727B58B19ADFC1E5BB2E84D");
        }

        [Fact]
        public void HmacSha384_Stream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "45303A3B5F02C8462D6FA438893DAA05EFFA850B853DF614D45F343D20AFE6D6DBAC6D0656788C4398EDF0AEC01488D4");
        }

        [Fact]
        public void HmacSha384_Stream_Empty()
        {
            // Verfied with:
            // echo -n "" | openssl sha384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "6A0FDC1C54C664AD91C7C157D2670C5D44E4D44EBAD2359A0206974C7088B1A867F76971E6C240C33B33A66BA295BB56");
        }

        [Fact]
        public async Task HmacSha384_Stream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "6C4977B00CCE179ADCC06EB35E48ABDE9B5604FC9E8B6B25F65E0234EE6394DE40F0C6C6B727B58B19ADFC1E5BB2E84D");
        }

        [Fact]
        public async Task HmacSha384_Stream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "45303A3B5F02C8462D6FA438893DAA05EFFA850B853DF614D45F343D20AFE6D6DBAC6D0656788C4398EDF0AEC01488D4");
        }

        [Fact]
        public async Task HmacSha384_Stream_Empty_Async()
        {
            // Verfied with:
            // echo -n "" | openssl sha384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "6A0FDC1C54C664AD91C7C157D2670C5D44E4D44EBAD2359A0206974C7088B1A867F76971E6C240C33B33A66BA295BB56");
        }

        [Fact]
        public void HmacSha384_HashSizes()
        {
            Assert.Equal(384, HMACSHA384.HashSizeInBits);
            Assert.Equal(48, HMACSHA384.HashSizeInBytes);
        }
    }
}
