// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class Sha256Tests : HashAlgorithmTestDriver<Sha256Tests.Traits>
    {
        public sealed class Traits : IHashTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => SHA256.HashSizeInBytes;
        }

        protected override HashAlgorithm Create() => SHA256.Create();
        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA256;

        protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return SHA256.TryHashData(source, destination, out bytesWritten);
        }

        protected override byte[] HashData(byte[] source) => SHA256.HashData(source);

        protected override byte[] HashData(ReadOnlySpan<byte> source) => SHA256.HashData(source);

        protected override int HashData(ReadOnlySpan<byte> source, Span<byte> destination) =>
            SHA256.HashData(source, destination);

        protected override int HashData(Stream source, Span<byte> destination) =>
            SHA256.HashData(source, destination);

        protected override byte[] HashData(Stream source) => SHA256.HashData(source);

        protected override ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken) =>
            SHA256.HashDataAsync(source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken) =>
            SHA256.HashDataAsync(source, cancellationToken);

        [Fact]
        public void Sha256_Empty()
        {
            Verify(
                Array.Empty<byte>(),
                "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        }

        [Fact]
        public void Sha256_Empty_Stream()
        {
            VerifyRepeating("", 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        }

        [Fact]
        public async Task Sha256_Empty_Stream_Async()
        {
            await VerifyRepeatingAsync("", 0, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        }

        [Fact]
        public void Sha256_VerifyLargeStream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -sha256
            VerifyRepeating(
                "0102030405060708",
                1024,
                "cedca4ad2cce0d0b399931708684800cd16be396ffa5af51297a091650aa3610");
        }

        [Fact]
        public void Sha256_VerifyLargeStream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -sha256
            VerifyRepeating(
                "0102030405060708",
                1025,
                "9e2e99445f5349c379ceb4c995dde401f63012422183a411d02eb251b1e02e65");
        }

        [Fact]
        public async Task Sha256_VerifyLargeStream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -sha256
            await VerifyRepeatingAsync(
                "0102030405060708",
                1025,
                "9e2e99445f5349c379ceb4c995dde401f63012422183a411d02eb251b1e02e65");
        }

        [Fact]
        public async Task Sha256_VerifyLargeStream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -sha256
            await VerifyRepeatingAsync(
                "0102030405060708",
                1024,
                "cedca4ad2cce0d0b399931708684800cd16be396ffa5af51297a091650aa3610");
        }

        // These test cases are from http://csrc.nist.gov/publications/fips/fips180-2/fips180-2.pdf Appendix B
        [Fact]
        public void Sha256_Fips180_1()
        {
            Verify(
                "abc",
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        }

        [Fact]
        public void Sha256_Fips180_MultiBlock()
        {
            VerifyMultiBlock(
                "ab",
                "c",
                "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
                "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        }

        [Fact]
        public void Sha256_Fips180_2()
        {
            Verify(
                "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
                "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1");
        }

        [Fact]
        public void Sha256_Fips180_3()
        {
            VerifyRepeating(
                "a",
                1000000,
                "cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0");
        }

        [Fact]
        public async Task Sha256_Fips180_3_Async()
        {
            await VerifyRepeatingAsync(
                "a",
                1000000,
                "cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0");
        }

        [Fact]
        public void Sha256_HashSizes()
        {
            Assert.Equal(256, SHA256.HashSizeInBits);
            Assert.Equal(32, SHA256.HashSizeInBytes);
        }
    }
}
