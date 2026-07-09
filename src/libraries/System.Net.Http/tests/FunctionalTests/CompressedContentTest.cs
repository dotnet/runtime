// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class CompressedContentTest
    {
        [Fact]
        public void Ctor_NullContent_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("content", () => new GZipCompressedContent(null));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new GZipCompressedContent(null, new ZLibCompressionOptions()));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new GZipCompressedContent(null, CompressionLevel.SmallestSize));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new BrotliCompressedContent(null));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new BrotliCompressedContent(null, new BrotliCompressionOptions()));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new BrotliCompressedContent(null, CompressionLevel.SmallestSize));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new ZstandardCompressedContent(null));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new ZstandardCompressedContent(null, new ZstandardCompressionOptions()));
            AssertExtensions.Throws<ArgumentNullException>("content", () => new ZstandardCompressedContent(null, CompressionLevel.SmallestSize));
        }

        [Fact]
        public void Ctor_NullOptions_ThrowsArgumentNullException()
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            AssertExtensions.Throws<ArgumentNullException>("compressionOptions", () => new GZipCompressedContent(inner, null));
            AssertExtensions.Throws<ArgumentNullException>("compressionOptions", () => new BrotliCompressedContent(inner, null));
            AssertExtensions.Throws<ArgumentNullException>("compressionOptions", () => new ZstandardCompressedContent(inner, null));
        }

        [Theory]
        [InlineData("gzip")]
        [InlineData("br")]
        [InlineData("zstd")]
        public void Ctor_SetsContentEncodingHeader(string encoding)
        {
            HttpContent content = CreateContent(encoding, new ByteArrayContent(Array.Empty<byte>()));

            Assert.Equal(new[] { encoding }, content.Headers.ContentEncoding);
        }

        [Fact]
        public void Ctor_RemovesContentLength()
        {
            var inner = new ByteArrayContent(new byte[10]);
            Assert.Equal(10, inner.Headers.ContentLength);

            var content = new GZipCompressedContent(inner);

            Assert.Null(content.Headers.ContentLength);
        }

        [Fact]
        public void Ctor_CopiesInnerContentHeaders()
        {
            var inner = new ByteArrayContent(Array.Empty<byte>());
            inner.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var content = new GZipCompressedContent(inner);

            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
        }

        [Fact]
        public void Ctor_NestedContent_StacksContentEncoding()
        {
            var inner = new GZipCompressedContent(new ByteArrayContent(Array.Empty<byte>()));
            var outer = new BrotliCompressedContent(inner);

            Assert.Equal(new[] { "gzip", "br" }, outer.Headers.ContentEncoding);
        }

        [Fact]
        public void Dispose_DisposesInnerContent()
        {
            var inner = new MockContent();
            var content = new GZipCompressedContent(inner);

            content.Dispose();

            Assert.Equal(1, inner.DisposeCount);
        }

        [Theory]
        [MemberData(nameof(RoundTripData))]
        public async Task SerializeToStream_RoundTrips_MatchesOriginal(string encoding, bool async)
        {
            byte[] original = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. " + new string('a', 1024));
            HttpContent content = CreateContent(encoding, new ByteArrayContent(original));

            byte[] compressed = await SerializeAsync(content, async);
            byte[] decompressed = Decompress(compressed, encoding);

            Assert.Equal(original, decompressed);
        }

        [Fact]
        public async Task SerializeToStream_WithOptions_RoundTrips()
        {
            byte[] original = Encoding.UTF8.GetBytes(new string('a', 4096));

            var gzip = new GZipCompressedContent(new ByteArrayContent(original), new ZLibCompressionOptions { CompressionLevel = 5 });
            Assert.Equal(original, Decompress(await SerializeAsync(gzip, async: true), "gzip"));

            if (!OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi())
            {
                var brotli = new BrotliCompressedContent(new ByteArrayContent(original), new BrotliCompressionOptions { Quality = 5 });
                Assert.Equal(original, Decompress(await SerializeAsync(brotli, async: true), "br"));

                var zstd = new ZstandardCompressedContent(new ByteArrayContent(original), new ZstandardCompressionOptions { Quality = 5 });
                Assert.Equal(original, Decompress(await SerializeAsync(zstd, async: true), "zstd"));
            }
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task SerializeToStream_WithCompressionLevel_RoundTrips(string encoding)
        {
            byte[] original = Encoding.UTF8.GetBytes(new string('a', 4096));
            HttpContent content = encoding switch
            {
                "gzip" => new GZipCompressedContent(new ByteArrayContent(original), CompressionLevel.SmallestSize),
                "br" => new BrotliCompressedContent(new ByteArrayContent(original), CompressionLevel.SmallestSize),
                "zstd" => new ZstandardCompressedContent(new ByteArrayContent(original), CompressionLevel.SmallestSize),
                _ => throw new ArgumentOutOfRangeException(nameof(encoding))
            };

            Assert.Equal(original, Decompress(await SerializeAsync(content, async: true), encoding));
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task SerializeToStream_RepeatableInnerContent_CanSerializeMultipleTimes(string encoding)
        {
            byte[] original = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. " + new string('a', 512));
            HttpContent content = CreateContent(encoding, new ByteArrayContent(original));

            byte[] first = await SerializeAsync(content, async: true);
            byte[] second = await SerializeAsync(content, async: false);

            Assert.Equal(original, Decompress(first, encoding));
            Assert.Equal(original, Decompress(second, encoding));
        }

        [Fact]
        public void SerializeToStream_NonRepeatableInnerContent_SecondCallThrows()
        {
            var inner = new StreamContent(new NonSeekableStream(Encoding.UTF8.GetBytes("data")));
            var content = new GZipCompressedContent(inner);

            using var ms = new MemoryStream();
            content.CopyTo(ms, null, default);

            Assert.Throws<InvalidOperationException>(() => content.CopyTo(ms, null, default));
        }

        public static IEnumerable<object[]> RoundTripData()
        {
            foreach (bool async in new[] { true, false })
            {
                yield return new object[] { "gzip", async };

                // BrotliStream and ZstandardStream are not supported on browser/wasi.
                if (!OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi())
                {
                    yield return new object[] { "br", async };
                    yield return new object[] { "zstd", async };
                }
            }
        }

        public static IEnumerable<object[]> Encodings()
        {
            yield return new object[] { "gzip" };

            // BrotliStream and ZstandardStream are not supported on browser/wasi.
            if (!OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi())
            {
                yield return new object[] { "br" };
                yield return new object[] { "zstd" };
            }
        }

        private static HttpContent CreateContent(string encoding, HttpContent inner) => encoding switch
        {
            "gzip" => new GZipCompressedContent(inner),
            "br" => new BrotliCompressedContent(inner),
            "zstd" => new ZstandardCompressedContent(inner),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };

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

        private static byte[] Decompress(byte[] compressed, string encoding)
        {
            using var source = new MemoryStream(compressed);
            using Stream decompressor = encoding switch
            {
                "gzip" => new GZipStream(source, CompressionMode.Decompress),
                "br" => new BrotliStream(source, CompressionMode.Decompress),
                "zstd" => new ZstandardStream(source, CompressionMode.Decompress),
                _ => throw new ArgumentOutOfRangeException(nameof(encoding))
            };

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

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) => Task.CompletedTask;

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }
    }
}
