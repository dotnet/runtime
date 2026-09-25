// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    // GZip is supported on every platform (including browser/wasi), so the gzip tests and the shared
    // helpers live in this file, which is compiled for all targets. Brotli and Zstandard are not
    // supported on browser/wasi; their tests live in CompressedContentTest.NonBrowser.cs, which is
    // excluded from those builds.
    public partial class CompressedContentTest
    {
        [Fact]
        public void GZip_Ctor_NullContent_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("content", () => new GZipCompressedContent(null));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new GZipCompressedContent(null, new ZLibCompressionOptions()));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new GZipCompressedContent(null, CompressionLevel.SmallestSize));
        }

        [Fact]
        public void GZip_Ctor_NullOptions_ThrowsArgumentNullException()
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            AssertExtensions.Throws<ArgumentNullException>("compressionOptions", () => new GZipCompressedContent(inner, null));
        }

        [Theory]
        [InlineData((CompressionLevel)(-1))]
        [InlineData((CompressionLevel)4)]
        [InlineData((CompressionLevel)99)]
        public void GZip_Ctor_InvalidCompressionLevel_ThrowsArgumentOutOfRangeException(CompressionLevel compressionLevel)
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            AssertExtensions.Throws<ArgumentOutOfRangeException>("compressionLevel", () => new GZipCompressedContent(inner, compressionLevel));
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.NoCompression)]
        [InlineData(CompressionLevel.SmallestSize)]
        public void GZip_Ctor_ValidCompressionLevel_DoesNotThrow(CompressionLevel compressionLevel)
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            _ = new GZipCompressedContent(inner, compressionLevel);
        }

        [Fact]
        public void GZip_Ctor_SetsContentEncodingHeader()
        {
            var content = new GZipCompressedContent(new ByteArrayContent(Array.Empty<byte>()));

            Assert.Equal(new[] { "gzip" }, content.Headers.ContentEncoding);
        }

        [Fact]
        public void GZip_Ctor_RemovesContentLength()
        {
            var inner = new ByteArrayContent(new byte[10]);
            Assert.Equal(10, inner.Headers.ContentLength);

            var content = new GZipCompressedContent(inner);

            Assert.Null(content.Headers.ContentLength);
        }

        [Fact]
        public void GZip_Ctor_CopiesInnerContentHeaders()
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            inner.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var content = new GZipCompressedContent(inner);

            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
        }

        [Fact]
        public void GZip_Ctor_NestedContent_StacksContentEncoding()
        {
            var inner = new GZipCompressedContent(new ByteArrayContent(Array.Empty<byte>()));
            var outer = new GZipCompressedContent(inner);

            Assert.Equal(new[] { "gzip", "gzip" }, outer.Headers.ContentEncoding);
        }

        [Fact]
        public void GZip_Dispose_DisposesInnerContent()
        {
            var inner = new MockContent();
            var content = new GZipCompressedContent(inner);

            content.Dispose();

            Assert.Equal(1, inner.DisposeCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GZip_SerializeToStream_RoundTrips_MatchesOriginal(bool async)
        {
            byte[] original = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. " + new string('a', 1024));
            var content = new GZipCompressedContent(new ByteArrayContent(original));

            byte[] compressed = await SerializeAsync(content, async);

            Assert.Equal(original, DecompressGZip(compressed));
        }

        [Fact]
        public async Task GZip_SerializeToStream_WithOptions_RoundTrips()
        {
            byte[] original = Encoding.UTF8.GetBytes(new string('a', 4096));

            var gzip = new GZipCompressedContent(new ByteArrayContent(original), new ZLibCompressionOptions { CompressionLevel = 5 });

            Assert.Equal(original, DecompressGZip(await SerializeAsync(gzip, async: true)));
        }

        [Fact]
        public async Task GZip_SerializeToStream_WithCompressionLevel_RoundTrips()
        {
            byte[] original = Encoding.UTF8.GetBytes(new string('a', 4096));

            var content = new GZipCompressedContent(new ByteArrayContent(original), CompressionLevel.SmallestSize);

            Assert.Equal(original, DecompressGZip(await SerializeAsync(content, async: true)));
        }

        [Fact]
        public async Task GZip_SerializeToStream_RepeatableInnerContent_CanSerializeMultipleTimes()
        {
            byte[] original = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. " + new string('a', 512));
            var content = new GZipCompressedContent(new ByteArrayContent(original));

            byte[] first = await SerializeAsync(content, async: true);
            byte[] second = await SerializeAsync(content, async: false);

            Assert.Equal(original, DecompressGZip(first));
            Assert.Equal(original, DecompressGZip(second));
        }

        [Fact]
        public void GZip_SerializeToStream_NonRepeatableInnerContent_SecondCallThrows()
        {
            var inner = new StreamContent(new NonSeekableStream(Encoding.UTF8.GetBytes("data")));
            var content = new GZipCompressedContent(inner);

            using var ms = new MemoryStream();
            content.CopyTo(ms, null, default);

            Assert.Throws<InvalidOperationException>(() => content.CopyTo(ms, null, default));
        }

        private static async Task<byte[]> SerializeAsync(HttpContent content, bool async)
        {
            using var ms = new MemoryStream();

            if (async)
            {
                await content.CopyToAsync(ms);
            }
            else
            {
                content.CopyTo(ms, null, default);
            }

            return ms.ToArray();
        }

        private static byte[] DecompressGZip(byte[] compressed)
        {
            using var source = new MemoryStream(compressed);
            using var decompressor = new GZipStream(source, CompressionMode.Decompress);
            using var result = new MemoryStream();
            decompressor.CopyTo(result);
            return result.ToArray();
        }

        private sealed class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] data) : base(data) { }

            public override bool CanSeek => false;
        }

        private sealed class MockContent : HttpContent
        {
            public int DisposeCount { get; private set; }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.CompletedTask;

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }
    }
}
