// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class Sha384Tests : HashAlgorithmTestDriver<Sha384Tests.Traits>
    {
        public sealed class Traits : IHashTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => SHA384.HashSizeInBytes;
        }

        protected override HashAlgorithm Create() => SHA384.Create();
        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA384;

        protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return SHA384.TryHashData(source, destination, out bytesWritten);
        }

        protected override byte[] HashData(byte[] source) => SHA384.HashData(source);

        protected override byte[] HashData(ReadOnlySpan<byte> source) => SHA384.HashData(source);

        protected override int HashData(ReadOnlySpan<byte> source, Span<byte> destination) =>
            SHA384.HashData(source, destination);

        protected override int HashData(Stream source, Span<byte> destination) =>
            SHA384.HashData(source, destination);

        protected override byte[] HashData(Stream source) => SHA384.HashData(source);

        protected override ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken) =>
            SHA384.HashDataAsync(source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken) =>
            SHA384.HashDataAsync(source, cancellationToken);

        [Fact]
        public void Sha384_Empty()
        {
            Verify(
                Array.Empty<byte>(),
                "38B060A751AC96384CD9327EB1B1E36A21FDB71114BE07434C0CC7BF63F6E1DA274EDEBFE76F65FBD51AD2F14898B95B");
        }

        [Fact]
        public void Sha384_Empty_Stream()
        {
            VerifyRepeating(
                "",
                0,
                "38B060A751AC96384CD9327EB1B1E36A21FDB71114BE07434C0CC7BF63F6E1DA274EDEBFE76F65FBD51AD2F14898B95B");
        }

        [Fact]
        public async Task Sha384_Empty_Stream_Async()
        {
            await VerifyRepeatingAsync(
                "",
                0,
                "38B060A751AC96384CD9327EB1B1E36A21FDB71114BE07434C0CC7BF63F6E1DA274EDEBFE76F65FBD51AD2F14898B95B");
        }

        [Fact]
        public void Sha384_VerifyLargeStream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -sha384
            VerifyRepeating(
                "0102030405060708",
                1024,
                "d9deec18b8ec0d31270eaeaaf3bcb1de55f1d81482a55d2c023bad873175f1694d8c28e8138d9147dc180e679cd79c58");
        }

        [Fact]
        public void Sha384_VerifyLargeStream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -sha384
            VerifyRepeating(
                "0102030405060708",
                1025,
                "35cf18493364379093c7def8477330f817f9045d2e311d721730b24d98c9d9e9761c7f27821742e0c236509627aea7fa");
        }

        [Fact]
        public async Task Sha384_VerifyLargeStream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -sha384
            await VerifyRepeatingAsync(
                "0102030405060708",
                1025,
                "35cf18493364379093c7def8477330f817f9045d2e311d721730b24d98c9d9e9761c7f27821742e0c236509627aea7fa");
        }

        [Fact]
        public async Task Sha384_VerifyLargeStream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -sha384
            await VerifyRepeatingAsync(
                "0102030405060708",
                1024,
                "d9deec18b8ec0d31270eaeaaf3bcb1de55f1d81482a55d2c023bad873175f1694d8c28e8138d9147dc180e679cd79c58");
        }

        // These test cases are from http://csrc.nist.gov/groups/ST/toolkit/documents/Examples/SHA_All.pdf
        [Fact]
        public void Sha384_NistShaAll_1()
        {
            Verify(
                "abc",
                "CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED8086072BA1E7CC2358BAECA134C825A7");
        }

        [Fact]
        public void Sha384_Fips180_MultiBlock()
        {
            VerifyMultiBlock(
                "a",
                "bc",
                "CB00753F45A35E8BB5A03D699AC65007272C32AB0EDED1631A8B605A43FF5BED8086072BA1E7CC2358BAECA134C825A7",
                "38B060A751AC96384CD9327EB1B1E36A21FDB71114BE07434C0CC7BF63F6E1DA274EDEBFE76F65FBD51AD2F14898B95B");
        }

        [Fact]
        public void Sha384_NistShaAll_2()
        {
            Verify(
                "abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu",
                "09330C33F71147E83D192FC782CD1B4753111B173B3B05D22FA08086E3B0F712FCC7C71A557E2DB966C3E9FA91746039");
        }

        [Fact]
        public void Sha384_HashSizes()
        {
            Assert.Equal(384, SHA384.HashSizeInBits);
            Assert.Equal(48, SHA384.HashSizeInBytes);
        }
    }
}
