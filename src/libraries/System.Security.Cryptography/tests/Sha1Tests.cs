// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class Sha1Tests : HashAlgorithmTestDriver<Sha1Tests.Traits>
    {
        public sealed class Traits : IHashTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => SHA1.HashSizeInBytes;
        }

        protected override HashAlgorithm Create() => SHA1.Create();
        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA1;

        protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return SHA1.TryHashData(source, destination, out bytesWritten);
        }

        protected override byte[] HashData(byte[] source) => SHA1.HashData(source);

        protected override byte[] HashData(ReadOnlySpan<byte> source) => SHA1.HashData(source);

        protected override int HashData(ReadOnlySpan<byte> source, Span<byte> destination) =>
            SHA1.HashData(source, destination);

        protected override int HashData(Stream source, Span<byte> destination) =>
            SHA1.HashData(source, destination);

        protected override byte[] HashData(Stream source) => SHA1.HashData(source);

        protected override ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken) =>
            SHA1.HashDataAsync(source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken) =>
            SHA1.HashDataAsync(source, cancellationToken);

        [Fact]
        public void Sha1_Empty()
        {
            Verify(Array.Empty<byte>(), "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        [Fact]
        public void Sha1_Empty_Stream()
        {
            VerifyRepeating("", 0, "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        [Fact]
        public async Task Sha1_Empty_Stream_Async()
        {
            await VerifyRepeatingAsync("", 0, "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        [Fact]
        public void Sha1_VerifyLargeStream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -sha1
            VerifyRepeating("0102030405060708", 1024, "fc8053215c935a5e9cdc51b94bb40b3e66128d41");
        }

        [Fact]
        public void Sha1_VerifyLargeStream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -sha1
            VerifyRepeating("0102030405060708", 1025, "18c6aa8d255c47941958729faaae9614c9793bb2");
        }

        [Fact]
        public async Task Sha1_VerifyLargeStream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -sha1
            await VerifyRepeatingAsync("0102030405060708", 1025, "18c6aa8d255c47941958729faaae9614c9793bb2");
        }

        [Fact]
        public async Task Sha1_VerifyLargeStream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -sha1
            await VerifyRepeatingAsync("0102030405060708", 1024, "fc8053215c935a5e9cdc51b94bb40b3e66128d41");
        }

        // SHA1 tests are defined somewhat obliquely within RFC 3174, section 7.3
        // The same tests appear in http://csrc.nist.gov/publications/fips/fips180-2/fips180-2.pdf Appendix A
        [Fact]
        public void Sha1_Rfc3174_1()
        {
            Verify("abc", "A9993E364706816ABA3E25717850C26C9CD0D89D");
        }

        [Fact]
        public void Sha1_Rfc3174_MultiBlock()
        {
            VerifyMultiBlock("ab", "c", "A9993E364706816ABA3E25717850C26C9CD0D89D", "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709");
        }

        [Fact]
        public void Sha1_Rfc3174_2()
        {
            Verify("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "84983E441C3BD26EBAAE4AA1F95129E5E54670F1");
        }

        [Fact]
        public void Sha1_Rfc3174_3()
        {
            VerifyRepeating("a", 1000000, "34AA973CD4C4DAA4F61EEB2BDBAD27316534016F");
        }

        [Fact]
        public void Sha1_Rfc3174_4()
        {
            VerifyRepeating("0123456701234567012345670123456701234567012345670123456701234567", 10, "DEA356A2CDDD90C7A7ECEDC5EBB563934F460452");
        }

        [Fact]
        public async Task Sha1_Rfc3174_3_Async()
        {
            await VerifyRepeatingAsync("a", 1000000, "34AA973CD4C4DAA4F61EEB2BDBAD27316534016F");
        }

        [Fact]
        public async Task Sha1_Rfc3174_4_Async()
        {
            await VerifyRepeatingAsync("0123456701234567012345670123456701234567012345670123456701234567", 10, "DEA356A2CDDD90C7A7ECEDC5EBB563934F460452");
        }

        [Fact]
        public void Sha1_HashSizes()
        {
            Assert.Equal(160, SHA1.HashSizeInBits);
            Assert.Equal(20, SHA1.HashSizeInBytes);
        }
    }
}
