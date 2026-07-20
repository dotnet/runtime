// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Net.Http.Functional.Tests
{
    // Brotli and Zstandard rely on native libraries that are not available on browser/wasi, so both
    // types are annotated [UnsupportedOSPlatform("browser"/"wasi")]. This file is excluded from those
    // builds; the gzip tests and shared helpers live in CompressedContentTest.cs.
    public partial class CompressedContentTest
    {
        [Fact]
        public void BrotliZstd_Ctor_NullContent_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("content", () => new BrotliCompressedContent(null));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new BrotliCompressedContent(null, new BrotliCompressionOptions()));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new BrotliCompressedContent(null, CompressionLevel.SmallestSize));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new ZstandardCompressedContent(null));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new ZstandardCompressedContent(null, new ZstandardCompressionOptions()));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new ZstandardCompressedContent(null, CompressionLevel.SmallestSize));
        }

        [Fact]
        public void BrotliZstd_Ctor_NullOptions_ThrowsArgumentNullException()
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            AssertExtensions.Throws<ArgumentNullException>("compressionOptions", () => new BrotliCompressedContent(inner, null));
            AssertExtensions.Throws<ArgumentNullException>("compressionOptions", () => new ZstandardCompressedContent(inner, null));
        }

        [Theory]
        [InlineData((CompressionLevel)(-1))]
        [InlineData((CompressionLevel)4)]
        [InlineData((CompressionLevel)99)]
        public void BrotliZstd_Ctor_InvalidCompressionLevel_ThrowsArgumentOutOfRangeException(CompressionLevel compressionLevel)
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            AssertExtensions.Throws<ArgumentOutOfRangeException>("compressionLevel", () => new BrotliCompressedContent(inner, compressionLevel));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("compressionLevel", () => new ZstandardCompressedContent(inner, compressionLevel));
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.NoCompression)]
        [InlineData(CompressionLevel.SmallestSize)]
        public void BrotliZstd_Ctor_ValidCompressionLevel_DoesNotThrow(CompressionLevel compressionLevel)
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            _ = new BrotliCompressedContent(inner, compressionLevel);
            _ = new ZstandardCompressedContent(inner, compressionLevel);
        }

        [Theory]
        [InlineData("br")]
        [InlineData("zstd")]
        public void BrotliZstd_Ctor_SetsContentEncodingHeader(string encoding)
        {
            HttpContent content = CreateBrotliOrZstdContent(encoding, new ByteArrayContent(Array.Empty<byte>()));

            Assert.Equal(new[] { encoding }, content.Headers.ContentEncoding);
        }

        [Fact]
        public void BrotliZstd_Ctor_NestedContent_StacksContentEncoding()
        {
            var inner = new GZipCompressedContent(new ByteArrayContent(Array.Empty<byte>()));
            var outer = new BrotliCompressedContent(inner);

            Assert.Equal(new[] { "gzip", "br" }, outer.Headers.ContentEncoding);
        }

        [Theory]
        [InlineData("br", true)]
        [InlineData("br", false)]
        [InlineData("zstd", true)]
        [InlineData("zstd", false)]
        public async Task BrotliZstd_SerializeToStream_RoundTrips_MatchesOriginal(string encoding, bool async)
        {
            byte[] original = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. " + new string('a', 1024));
            HttpContent content = CreateBrotliOrZstdContent(encoding, new ByteArrayContent(original));

            byte[] compressed = await SerializeAsync(content, async);

            Assert.Equal(original, DecompressBrotliOrZstd(compressed, encoding));
        }

        [Theory]
        [InlineData("br")]
        [InlineData("zstd")]
        public async Task BrotliZstd_SerializeToStream_WithOptions_RoundTrips(string encoding)
        {
            byte[] original = Encoding.UTF8.GetBytes(new string('a', 4096));
            HttpContent content = encoding == "br"
                ? new BrotliCompressedContent(new ByteArrayContent(original), new BrotliCompressionOptions { Quality = 5 })
                : new ZstandardCompressedContent(new ByteArrayContent(original), new ZstandardCompressionOptions { Quality = 5 });

            Assert.Equal(original, DecompressBrotliOrZstd(await SerializeAsync(content, async: true), encoding));
        }

        [ConditionalTheory]
        [InlineData("br", CompressionLevel.NoCompression)]
        [InlineData("br", CompressionLevel.Fastest)]
        [InlineData("br", CompressionLevel.Optimal)]
        [InlineData("br", CompressionLevel.SmallestSize)]
        [InlineData("zstd", CompressionLevel.NoCompression)]
        [InlineData("zstd", CompressionLevel.Fastest)]
        [InlineData("zstd", CompressionLevel.Optimal)]
        [InlineData("zstd", CompressionLevel.SmallestSize)]
        public async Task BrotliZstd_SerializeToStream_WithCompressionLevel_RoundTrips(string encoding, CompressionLevel compressionLevel)
        {
            if (PlatformDetection.Is32BitProcess && compressionLevel == CompressionLevel.SmallestSize && encoding == "zstd")
            {
                // Zstandard smallest size requires too much working memory
                // (800+ MB) and causes intermittent allocation errors on 32-bit
                // processes in CI.
                throw new SkipTestException($"Skipping {encoding} with {compressionLevel} on 32-bit process due to excessive memory requirements.");
            }

            byte[] original = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. ", 4096)));
            HttpContent content = encoding == "br"
                ? new BrotliCompressedContent(new ByteArrayContent(original), compressionLevel)
                : new ZstandardCompressedContent(new ByteArrayContent(original), compressionLevel);

            Assert.Equal(original, DecompressBrotliOrZstd(await SerializeAsync(content, async: true), encoding));
        }

        [Theory]
        [InlineData("br")]
        [InlineData("zstd")]
        public async Task BrotliZstd_SerializeToStream_RepeatableInnerContent_CanSerializeMultipleTimes(string encoding)
        {
            byte[] original = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. " + new string('a', 512));
            HttpContent content = CreateBrotliOrZstdContent(encoding, new ByteArrayContent(original));

            byte[] first = await SerializeAsync(content, async: true);
            byte[] second = await SerializeAsync(content, async: false);

            Assert.Equal(original, DecompressBrotliOrZstd(first, encoding));
            Assert.Equal(original, DecompressBrotliOrZstd(second, encoding));
        }

        private static HttpContent CreateBrotliOrZstdContent(string encoding, HttpContent inner) => encoding switch
        {
            "br" => new BrotliCompressedContent(inner),
            "zstd" => new ZstandardCompressedContent(inner),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };

        private static byte[] DecompressBrotliOrZstd(byte[] compressed, string encoding)
        {
            using var source = new MemoryStream(compressed);
            using Stream decompressor = encoding switch
            {
                "br" => new BrotliStream(source, CompressionMode.Decompress),
                // RFC 9659 requires the "zstd" content coding to be decodable with an 8 MB (2^23) window.
                "zstd" => new ZstandardStream(source, new ZstandardDecompressionOptions { MaxWindowLog2 = 23 }),
                _ => throw new ArgumentOutOfRangeException(nameof(encoding))
            };

            using var result = new MemoryStream();
            decompressor.CopyTo(result);
            return result.ToArray();
        }
    }
}
