// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class HmacSha3_256Tests : HmacTests<HmacSha3_256Tests.Traits>
    {
        public sealed class Traits : IHmacTrait
        {
            public static bool IsSupported => HMACSHA3_256.IsSupported;
            public static int HashSizeInBytes => HMACSHA3_256.HashSizeInBytes;
        }

        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA3_256;

        protected override int BlockSize => 136;
        protected override int MacSize => HMACSHA3_256.HashSizeInBytes;

        protected override HMAC Create() => new HMACSHA3_256();
        protected override HMAC Create(byte[] key) => new HMACSHA3_256(key);
        protected override HashAlgorithm CreateHashAlgorithm() => SHA3_256.Create();
        protected override byte[] HashDataOneShot(byte[] key, byte[] source) =>
            HMACSHA3_256.HashData(key, source);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source) =>
            HMACSHA3_256.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination) =>
            HMACSHA3_256.HashData(key, source, destination);

        protected override bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written) =>
            HMACSHA3_256.TryHashData(key, source, destination, out written);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, Stream source) =>
            HMACSHA3_256.HashData(key, source);

        protected override byte[] HashDataOneShot(byte[] key, Stream source) =>
            HMACSHA3_256.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, Stream source, Span<byte> destination) =>
            HMACSHA3_256.HashData(key, source, destination);

        protected override ValueTask<int> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken) => HMACSHA3_256.HashDataAsync(key, source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA3_256.HashDataAsync(key, source, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            byte[] key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA3_256.HashDataAsync(key, source, cancellationToken);

        private static readonly byte[][] s_testKeys = new byte[][]
        {
            // From: https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Standards-and-Guidelines/documents/examples/HMAC_SHA3-256.pdf
            null,
            ByteUtils.HexToByteArray("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"),
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f" +
                "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f" +
                "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f" +
                "8081828384858687"),
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f" +
                "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f" +
                "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f" +
                "808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f" +
                "a0a1a2a3a4a5a6a7"),
            ByteUtils.HexToByteArray("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"),
        };

        private static readonly byte[][] s_testData = new byte[][]
        {
            null,
            ByteUtils.HexToByteArray("53616d706c65206d65737361676520666f72206b65796c656e3c626c6f636b6c656e"),
            ByteUtils.HexToByteArray("53616d706c65206d65737361676520666f72206b65796c656e3d626c6f636b6c656e"),
            ByteUtils.HexToByteArray("53616d706c65206d65737361676520666f72206b65796c656e3e626c6f636b6c656e"),
            ByteUtils.HexToByteArray("53616d706c65206d65737361676520666f72206b65796c656e3c626c6f636b6c656e2c2077697468207472756e636174656420746167"),
        };

        private static readonly byte[][] s_testMacs = new byte[][]
        {
            null,
            ByteUtils.HexToByteArray("4fe8e202c4f058e8dddc23d8c34e467343e23555e24fc2f025d598f558f67205"),
            ByteUtils.HexToByteArray("68b94e2e538a9be4103bebb5aa016d47961d4d1aa906061313b557f8af2c3faa"),
            ByteUtils.HexToByteArray("9bcf2c238e235c3ce88404e813bd2f3a97185ac6f238c63d6229a00b07974258"),
            ByteUtils.HexToByteArray("c8dc7148d8c1423aa549105dafdf9cad"), // Truncated test case.
        };


        public HmacSha3_256Tests() : base(s_testKeys, s_testData, s_testMacs)
        {
        }

        [ConditionalTheory(nameof(IsSupported))]
        [MemberData(nameof(TestCaseIds))]
        public void HmacSha3_256_VerifyTestCases(int caseId)
        {
            VerifyHmac(caseId, s_testMacs[caseId]);
        }

        public static IEnumerable<object[]> TestCaseIds
        {
            get
            {
                for (int i = 1; i < s_testKeys.Length; i++)
                {
                    yield return new object[] { i };
                }
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_256_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_256_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACSHA3_256(null));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_256_EmptyKey()
        {
            VerifyRepeating(
                input: "Crypto is fun!",
                1,
                hexKey: "",
                output: "c49c24ae6dce7e90d5e2853ad9e647d89ac3dd04eb71aa0912ab4b4b1068ba6a");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_256_Stream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha3-256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "F09DD1B814BF4A576FF8AEAFF69509E3093895426F441A428953221F3CB9E421");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_256_Stream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha3-256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "F1DB96FF2B53CBA0338FF519BFA10153731DB48A63C26EB66294895B220F72B5");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_256_Stream_Empty()
        {
            // Verfied with:
            // echo -n "" | openssl sha3-256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "72756291FF30F3E916BEF99EC9CF5938B25D90BBCAC1BDB1E1E6564E8EC6FDA5");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_256_Stream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha3-256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "F09DD1B814BF4A576FF8AEAFF69509E3093895426F441A428953221F3CB9E421");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_256_Stream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha3-256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "F1DB96FF2B53CBA0338FF519BFA10153731DB48A63C26EB66294895B220F72B5");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_256_Stream_Empty_Async()
        {
            // Verfied with:
            // echo -n "" | openssl sha3-256 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "72756291FF30F3E916BEF99EC9CF5938B25D90BBCAC1BDB1E1E6564E8EC6FDA5");
        }

        [Fact]
        public void HmacSha3_256_HashSizes()
        {
            Assert.Equal(256, HMACSHA3_256.HashSizeInBits);
            Assert.Equal(32, HMACSHA3_256.HashSizeInBytes);
        }

        [Fact]
        public void HmacSha3_256_IsSupported_AgreesWithPlatformVersion()
        {
            Assert.Equal(PlatformDetection.SupportsSha3, HMACSHA3_256.IsSupported);
        }
    }
}
