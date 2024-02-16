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
    public class HmacSha3_384Tests : HmacTests<HmacSha3_384Tests.Traits>
    {
        public sealed class Traits : IHmacTrait
        {
            public static bool IsSupported => HMACSHA3_384.IsSupported;
            public static int HashSizeInBytes => HMACSHA3_384.HashSizeInBytes;
        }

        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA3_384;

        protected override int BlockSize => 104;
        protected override int MacSize => HMACSHA3_384.HashSizeInBytes;

        protected override HMAC Create() => new HMACSHA3_384();
        protected override HMAC Create(byte[] key) => new HMACSHA3_384(key);
        protected override HashAlgorithm CreateHashAlgorithm() => SHA3_384.Create();
        protected override byte[] HashDataOneShot(byte[] key, byte[] source) =>
            HMACSHA3_384.HashData(key, source);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source) =>
            HMACSHA3_384.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination) =>
            HMACSHA3_384.HashData(key, source, destination);

        protected override bool TryHashDataOneShot(ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination, out int written) =>
            HMACSHA3_384.TryHashData(key, source, destination, out written);

        protected override byte[] HashDataOneShot(ReadOnlySpan<byte> key, Stream source) =>
            HMACSHA3_384.HashData(key, source);

        protected override byte[] HashDataOneShot(byte[] key, Stream source) =>
            HMACSHA3_384.HashData(key, source);

        protected override int HashDataOneShot(ReadOnlySpan<byte> key, Stream source, Span<byte> destination) =>
            HMACSHA3_384.HashData(key, source, destination);

        protected override ValueTask<int> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken) => HMACSHA3_384.HashDataAsync(key, source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            ReadOnlyMemory<byte> key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA3_384.HashDataAsync(key, source, cancellationToken);

        protected override ValueTask<byte[]> HashDataOneShotAsync(
            byte[] key,
            Stream source,
            CancellationToken cancellationToken) => HMACSHA3_384.HashDataAsync(key, source, cancellationToken);

        private static readonly byte[][] s_testKeys = new byte[][]
        {
            // From: https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Standards-and-Guidelines/documents/examples/HMAC_SHA3-384.pdf
            null,
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f1011121314151617" +
                "18191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f"),
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f" +
                "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f" +
                "6061626364656667"),
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f" +
                "404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f" +
                "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f" +
                "808182838485868788898a8b8c8d8e8f9091929394959697"),
            ByteUtils.HexToByteArray(
                "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f" +
                "202122232425262728292a2b2c2d2e2f"),
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
                "d588a3c51f3f2d906e8298c1199aa8ff6296218127f6b38a" +
                "90b6afe2c5617725bc99987f79b22a557b6520db710b7f42"),
            ByteUtils.HexToByteArray(
                "a27d24b592e8c8cbf6d4ce6fc5bf62d8fc98bf2d486640d9" +
                "eb8099e24047837f5f3bffbe92dcce90b4ed5b1e7e44fa90"),
            ByteUtils.HexToByteArray(
                "e5ae4c739f455279368ebf36d4f5354c95aa184c899d3870" +
                "e460ebc288ef1f9470053f73f7c6da2a71bcaec38ce7d6ac"),
            ByteUtils.HexToByteArray("25f4bf53606e91af79d24a4bb1fd6aecd44414a30c8ebb0a"), // Truncated
        };


        public HmacSha3_384Tests() : base(s_testKeys, s_testData, s_testMacs)
        {
        }

        [ConditionalTheory(nameof(IsSupported))]
        [MemberData(nameof(TestCaseIds))]
        public void HmacSha3_384_VerifyTestCases(int caseId)
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
        public void HmacSha3_384_Rfc2104_2()
        {
            VerifyHmacRfc2104_2();
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_384_ThrowsArgumentNullForNullConstructorKey()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", () => new HMACSHA3_384(null));
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_384_EmptyKey()
        {
            VerifyRepeating(
                input: "Crypto is fun!",
                1,
                hexKey: "",
                output: "16C079F7D15505E9E541421E63C432A063F39C1E3E953E6DC7B8A81FE5620AFFA430C3E6BE6A0F605755C7C5EE4E347E");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_384_Stream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha3-384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "17D03C8153AF1719F5829C8CBF328D4200900ED1AB038A399B4F256A490BD4D2AB311C71D2ED0C20ABA96E57768CCA6E");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_384_Stream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha3-384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "5360719A6A9DFEB1143C2866A7F72EA29404C3DBF37F244A0497F400DA126B2728118863454288F26E3796BE72238958");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void HmacSha3_384_Stream_Empty()
        {
            // Verfied with:
            // echo -n "" | openssl sha3-384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            VerifyRepeating(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "5B0196779BDAF859E99869A63C9FDF3821E9100A370B5E9B88F76B9DA87410F99846E7DBB4F8A69368C5C5A834B3128D");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_384_Stream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl sha3-384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1024,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "17D03C8153AF1719F5829C8CBF328D4200900ED1AB038A399B4F256A490BD4D2AB311C71D2ED0C20ABA96E57768CCA6E");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_384_Stream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl sha3-384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "0102030405060708",
                1025,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "5360719A6A9DFEB1143C2866A7F72EA29404C3DBF37F244A0497F400DA126B2728118863454288F26E3796BE72238958");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task HmacSha3_384_Stream_Empty_Async()
        {
            // Verfied with:
            // echo -n "" | openssl sha3-384 -hex -mac HMAC -macopt hexkey:000102030405060708090A0B0C0D0E0F
            await VerifyRepeatingAsync(
                input: "",
                0,
                hexKey: "000102030405060708090A0B0C0D0E0F",
                output: "5B0196779BDAF859E99869A63C9FDF3821E9100A370B5E9B88F76B9DA87410F99846E7DBB4F8A69368C5C5A834B3128D");
        }

        [Fact]
        public void HmacSha3_384_HashSizes()
        {
            Assert.Equal(384, HMACSHA3_384.HashSizeInBits);
            Assert.Equal(48, HMACSHA3_384.HashSizeInBytes);
        }

        [Fact]
        public void HmacSha3_384_IsSupported_AgreesWithPlatformVersion()
        {
            Assert.Equal(PlatformDetection.SupportsSha3, HMACSHA3_384.IsSupported);
        }
    }
}
