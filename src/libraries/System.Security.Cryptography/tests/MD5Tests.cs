// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class MD5Tests : HashAlgorithmTestDriver<MD5Tests.Traits>
    {
        public sealed class Traits : IHashTrait
        {
            public static bool IsSupported => true;
            public static int HashSizeInBytes => MD5.HashSizeInBytes;
        }

        protected override HashAlgorithm Create() => MD5.Create();
        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.MD5;

        protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return MD5.TryHashData(source, destination, out bytesWritten);
        }

        protected override byte[] HashData(byte[] source) => MD5.HashData(source);

        protected override byte[] HashData(ReadOnlySpan<byte> source) => MD5.HashData(source);

        protected override int HashData(ReadOnlySpan<byte> source, Span<byte> destination) =>
            MD5.HashData(source, destination);

        protected override int HashData(Stream source, Span<byte> destination) =>
            MD5.HashData(source, destination);

        protected override byte[] HashData(Stream source) => MD5.HashData(source);

        protected override ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken) =>
            MD5.HashDataAsync(source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken) =>
            MD5.HashDataAsync(source, cancellationToken);

        [Fact]
        public void MD5_VerifyLargeStream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -md5
            VerifyRepeating("0102030405060708", 1024, "5fc6366852074da6e4795a014574282c");
        }

        [Fact]
        public void MD5_VerifyLargeStream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -md5
            VerifyRepeating("0102030405060708", 1025, "c5f6181a24446a583b14282f32786513");
        }

        [Fact]
        public async Task MD5_VerifyLargeStream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -md5
            await VerifyRepeatingAsync("0102030405060708", 1025, "c5f6181a24446a583b14282f32786513");
        }

        [Fact]
        public async Task MD5_VerifyLargeStream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -md5
            await VerifyRepeatingAsync("0102030405060708", 1024, "5fc6366852074da6e4795a014574282c");
        }

        // Test cases are defined in RFC 1321, section A.5

        [Fact]
        public void MD5_Rfc1321_1()
        {
            Verify("", "d41d8cd98f00b204e9800998ecf8427e");
        }

        [Fact]
        public void MD5_Rfc1321_2()
        {
            Verify("a", "0cc175b9c0f1b6a831c399e269772661");
        }

        [Fact]
        public void MD5_Rfc1321_3()
        {
            Verify("abc", "900150983cd24fb0d6963f7d28e17f72");
        }

        [Fact]
        public void MD5_Rfc1321_MultiBlock()
        {
            VerifyMultiBlock(
                "a",
                "bc",
                "900150983cd24fb0d6963f7d28e17f72",
                "d41d8cd98f00b204e9800998ecf8427e");
        }

        [Fact]
        public void MD5_Rfc1321_4()
        {
            Verify("message digest", "f96b697d7cb7938d525a2f31aaf161d0");
        }

        [Fact]
        public void MD5_Rfc1321_5()
        {
            Verify("abcdefghijklmnopqrstuvwxyz", "c3fcd3d76192e4007dfb496cca67e13b");
        }

        [Fact]
        public void MD5_Rfc1321_6()
        {
            Verify("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", "d174ab98d277d9f5a5611c2c9f419d9f");
        }

        [Fact]
        public void MD5_Rfc1321_7()
        {
            Verify("12345678901234567890123456789012345678901234567890123456789012345678901234567890", "57edf4a22be3c955ac49da2e2107b67a");
        }

        [Fact]
        public void MD5_Rfc1321_1_AsStream()
        {
            VerifyRepeating(string.Empty, 0, "d41d8cd98f00b204e9800998ecf8427e");
        }

        [Fact]
        public void MD5_Rfc1321_7_AsStream()
        {
            VerifyRepeating("1234567890", 8, "57edf4a22be3c955ac49da2e2107b67a");
        }

        [Fact]
        public async Task MD5_Rfc1321_1_AsStream_Async()
        {
            await VerifyRepeatingAsync(string.Empty, 0, "d41d8cd98f00b204e9800998ecf8427e");
        }

        [Fact]
        public async Task MD5_Rfc1321_7_AsStream_Async()
        {
            await VerifyRepeatingAsync("1234567890", 8, "57edf4a22be3c955ac49da2e2107b67a");
        }

        [Fact]
        public void MD5_HashSizes()
        {
            Assert.Equal(128, MD5.HashSizeInBits);
            Assert.Equal(16, MD5.HashSizeInBytes);
        }
    }
}
