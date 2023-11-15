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
    public class HmacSha3_512Tests : HmacTests<HmacSha3_512Tests.Traits>
    {
        public sealed class Traits : IHmacTrait
        {
            public static bool IsSupported => HMACSHA3_512.IsSupported;
            public static int HashSizeInBytes => HMACSHA3_512.HashSizeInBytes;
        }

        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA3_512;

        protected override int BlockSize => 72;
        protected override int MacSize => HMACSHA3_512.HashSizeInBytes;

        protected override HMAC Create() => new HMACSHA3_512();
        protected override HMAC Create(byte[] key) => new HMACSHA3_512(key);
        protected override HashAlgorithm CreateHashAlgorithm() => SHA3_512.Create();
        protected override byte[] HashDataOneShot(byte[] key, byte[] source) =>
            HMACSHA3_512.HashData(key, source);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source) =>
            HMACSHA3_512.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination) =>
            HMACSHA3_512.HashData(key, source, destination);

        protected override bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written) =>
            HMACSHA3_512.TryHashData(key, source, destination, out written);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, Stream source) =>
            HMACSHA3_512.HashData(key, source);

        protected override byte[] HashDataOneShot(byte[] key, Stream source) =>
            HMACSHA3_512.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, Stream source, Span<byte> destination) =>
            HMACSHA3_512.HashData(key, source, destination);

        protected override ValueTask<int> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken) => HMACSHA3_512.HashDataAsync(key, source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA3_512.HashDataAsync(key, source, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            byte[] key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA3_512.HashDataAsync(key, source, cancellationToken);

        private static readonly byte[][] s_testKeys = new byte[][]
        {
            // From: https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Standards-and-Guidelines/documents/examples/HMAC_SHA3-512.pdf
            null,
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"),
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f" +
                "4041424344454647"),
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f" +
                "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f" +
                "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f" +
                "8081828384858687"),
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"),
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
            ByteUtils.HexToByteArray(
                "4efd629d6c71bf86162658f29943b1c308ce27cdfa6db0d9c3ce81763f9cbce5" +
                "f7ebe9868031db1a8f8eb7b6b95e5c5e3f657a8996c86a2f6527e307f0213196"),
            ByteUtils.HexToByteArray(
                "544e257ea2a3e5ea19a590e6a24b724ce6327757723fe2751b75bf007d80f6b3" +
                "60744bf1b7a88ea585f9765b47911976d3191cf83c039f5ffab0d29cc9d9b6da"),
            ByteUtils.HexToByteArray(
                "5f464f5e5b7848e3885e49b2c385f0694985d0e38966242dc4a5fe3fea4b37d4" +
                "6b65ceced5dcf59438dd840bab22269f0ba7febdb9fcf74602a35666b2a32915"),
            ByteUtils.HexToByteArray("7bb06d859257b25ce73ca700df34c5cbef5c898bac91029e0b27975d4e526a08"), // Truncated
        };


        public HmacSha3_512Tests() : base(s_testKeys, s_testData, s_testMacs)
        {
        }

        [ConditionalTheory(nameof(IsSupported))]
        [MemberData(nameof(TestCaseIds))]
        public void HmacSha3_512_VerifyTestCases(int caseId)
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
        public void HmacSha3_512_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_512_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACSHA3_512(null));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_512_EmptyKey()
        {
            VerifyRepeating(
                input: "Crypto is fun!",
                1,
                hexKey: "",
                output: "8F1658BB962C7048A50BEA174FA7697596F3F5F127228EEA64589DFFB0C1A07C" +
                        "98792648C97886B3DD9E63AB962581C67DA5EE04F2B15263555B1796782CB556");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_512_Stream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha3-512 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "43A635D00606EFB4797B0B50B3C2CCAACDC8C2DA38D1369EA49CDD93EDE27824" +
                        "317D9014C429DB18E5A6BFD811F7B484922471085F17ED31F6A7EB4E07BFFA97");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_512_Stream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha3-512 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "90D378A7546A66804E6C0ADF3203D3836244FD8CF628294E7F3AD95539EDF6D9" +
                        "E86DECE850D50DE76386CB293FA832778C7D6607A4F00AD666DA3EFFD6143E70");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_512_Stream_Empty()
        {
            // Verfied with:
            // echo -n "" | openssl sha3-512 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "908D1BE1A2CC5CC4B8C62E98D09AB6E967529FCB24F4177CB94CB072F5968D01" +
                        "CA58633748DC80D4615E3D21228BB3A5F535FA1CB963DF463CC28ABAF1A9B2D1");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_512_Stream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha3-512 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "43A635D00606EFB4797B0B50B3C2CCAACDC8C2DA38D1369EA49CDD93EDE27824" +
                        "317D9014C429DB18E5A6BFD811F7B484922471085F17ED31F6A7EB4E07BFFA97");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_512_Stream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha3-512 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "90D378A7546A66804E6C0ADF3203D3836244FD8CF628294E7F3AD95539EDF6D9" +
                        "E86DECE850D50DE76386CB293FA832778C7D6607A4F00AD666DA3EFFD6143E70");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_512_Stream_Empty_Async()
        {
            // Verfied with:
            // echo -n "" | openssl sha3-512 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "908D1BE1A2CC5CC4B8C62E98D09AB6E967529FCB24F4177CB94CB072F5968D01" +
                        "CA58633748DC80D4615E3D21228BB3A5F535FA1CB963DF463CC28ABAF1A9B2D1");
        }

        [Fact]
        public void HmacSha3_256_HashSizes()
        {
            Assert.Equal(512, HMACSHA3_512.HashSizeInBits);
            Assert.Equal(64, HMACSHA3_512.HashSizeInBytes);
        }

        [Fact]
        public void HmacSha3_512_IsSupported_AgreesWithPlatformVersion()
        {
            Assert.Equal(PlatformDetection.SupportsSha3, HMACSHA3_512.IsSupported);
        }
    }
}
