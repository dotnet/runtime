// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit.Sdk;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardStreamUnitTests : CompressionStreamUnitTestBase
    {
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new ZstandardStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new ZstandardStream(stream, mode, leaveOpen);
        public override Stream CreateStream(Stream stream, CompressionLevel level)
        {
            if (PlatformDetection.Is32BitProcess && level == CompressionLevel.SmallestSize)
            {
                // Zstandard smallest size requires too much working memory
                // (800+ MB) and causes intermittent allocation errors on 32-bit
                // processes in CI.
                level = CompressionLevel.Optimal;
            }

            return new ZstandardStream(stream, level);
        }
        public override Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen)
        {
            if (PlatformDetection.Is32BitProcess && level == CompressionLevel.SmallestSize)
            {
                // Zstandard smallest size requires too much working memory
                // (800+ MB) and causes intermittent allocation errors on 32-bit
                // processes in CI.
                level = CompressionLevel.Optimal;
            }

            return new ZstandardStream(stream, level, leaveOpen);
        }
        public override Stream CreateStream(Stream stream, ZLibCompressionOptions options, bool leaveOpen) =>
            new ZstandardStream(stream, options == null ? null : new ZstandardCompressionOptions { Quality = options.CompressionLevel }, leaveOpen);

        public override Stream BaseStream(Stream stream) => ((ZstandardStream)stream).BaseStream;

        // The tests are relying on an implementation detail of ZstandardStream, using knowledge of its internal buffer size
        // in various test calculations.  Currently the implementation is using the ArrayPool, which will round up to a
        // power-of-2. If the buffer size employed changes (which could also mean that ArrayPool<byte>.Shared starts giving
        // out different array sizes), the tests will need to be tweaked.
        public override int BufferSize => 1 << 16;

        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("ZstandardTestData", Path.GetFileName(uncompressedPath) + ".zst");

        [Fact]
        public void ZstandardStream_WithEncoder_CompressesData()
        {
            ZstandardEncoder encoder = new(5, 10);
            byte[] testData = ZstandardTestUtils.CreateTestData();
            using MemoryStream input = new(testData);
            using MemoryStream output = new();

            using (ZstandardStream compressionStream = new(output, encoder, leaveOpen: true))
            {
                input.CopyTo(compressionStream);
            }

            // Verify data was compressed
            Assert.True(output.Length > 0);
            Assert.True(output.Length < testData.Length);

            // Verify the encoder was reset, not disposed (should be reusable)
            using MemoryStream output2 = new();
            using (ZstandardStream compressionStream2 = new(output2, encoder, leaveOpen: true))
            {
                input.Position = 0;
                input.CopyTo(compressionStream2);
            }

            Assert.True(output2.Length > 0);
            encoder.Dispose(); // Clean up
        }

        [Fact]
        public void ZstandardStream_WithDecoder_DecompressesData()
        {
            // First, create some compressed data
            byte[] testData = ZstandardTestUtils.CreateTestData();
            byte[] compressedData = new byte[ZstandardEncoder.GetMaxCompressedLength(testData.Length)];
            bool compressResult = ZstandardEncoder.TryCompress(testData, compressedData, out int compressedLength);
            Assert.True(compressResult);

            Array.Resize(ref compressedData, compressedLength);

            ZstandardDecoder decoder = new();
            using MemoryStream input = new(compressedData);
            using MemoryStream output = new();

            using (ZstandardStream decompressionStream = new(input, decoder, leaveOpen: true))
            {
                decompressionStream.CopyTo(output);
            }

            // Verify data was decompressed correctly
            Assert.Equal(testData, output.ToArray());

            // Verify the decoder was reset, not disposed (should be reusable)
            using MemoryStream output2 = new();
            using (ZstandardStream decompressionStream2 = new(input, decoder, leaveOpen: true))
            {
                input.Position = 0;
                decompressionStream2.CopyTo(output2);
            }

            Assert.Equal(testData, output2.ToArray());
            decoder.Dispose(); // Clean up
        }

        [Theory]
        [InlineData(true, -1)]
        [InlineData(false, -1)]
        [InlineData(true, 2)]
        [InlineData(false, 2)]
        public async Task ZstandardStream_SetSourceLength_SizeDiffers_InvalidDataException(bool async, long delta)
        {
            byte[] testData = ZstandardTestUtils.CreateTestData();
            using MemoryStream output = new();
            ZstandardStream compressionStream = new(output, CompressionLevel.Optimal);

            compressionStream.SetSourceLength(testData.Length + delta);
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                // for shorter source length, the error occurs during Write/WriteAsync
                // for longer source length, the error occurs as part of Dispose/DisposeAsync
                if (async)
                {
                    await compressionStream.WriteAsync(testData, 0, testData.Length);
                    await compressionStream.DisposeAsync();
                }
                else
                {
                    compressionStream.Write(testData, 0, testData.Length);
                    compressionStream.Dispose();
                }
            });
        }

        [Fact]
        public void ZstandardStream_DecompressInvalidData_InvalidDataException()
        {
            byte[] invalidCompressedData = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            using MemoryStream input = new(invalidCompressedData);
            using ZstandardStream decompressionStream = new(input, CompressionMode.Decompress);
            byte[] buffer = new byte[16];

            Assert.Throws<InvalidDataException>(() => decompressionStream.Read(buffer, 0, buffer.Length));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZstandardStream_Roundtrip_WithDictionary(bool async)
        {
            byte[] dictionaryData = ZstandardTestUtils.CreateSampleDictionary();
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData);

            byte[] testData = ZstandardTestUtils.CreateTestData(5000);

            using MemoryStream compressedStream = new();
            using (ZstandardStream compressionStream = new(compressedStream, CompressionMode.Compress, dictionary, leaveOpen: true))
            {
                if (async)
                {
                    await compressionStream.WriteAsync(testData, 0, testData.Length);
                }
                else
                {
                    compressionStream.Write(testData, 0, testData.Length);
                }
            }

            compressedStream.Position = 0;

            using MemoryStream decompressedStream = new();
            using (ZstandardStream decompressionStream = new(compressedStream, CompressionMode.Decompress, dictionary))
            {
                if (async)
                {
                    await decompressionStream.CopyToAsync(decompressedStream);
                }
                else
                {
                    decompressionStream.CopyTo(decompressedStream);
                }
            }

            Assert.Equal(testData, decompressedStream.ToArray());
        }

        [InlineData(TestScenario.ReadAsync)]
        [InlineData(TestScenario.Read)]
        [InlineData(TestScenario.Copy)]
        [InlineData(TestScenario.CopyAsync)]
        [InlineData(TestScenario.ReadByte)]
        [InlineData(TestScenario.ReadByteAsync)]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StreamTruncation_IsDetected(TestScenario testScenario)
        {
            RemoteExecutor.Invoke(async (testScenario) =>
            {
                TestScenario scenario = Enum.Parse<TestScenario>(testScenario);

                AppContext.SetSwitch("System.IO.Compression.UseStrictValidation", true);

                var buffer = new byte[16];
                byte[] source = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
                byte[] compressedData;
                using (var compressed = new MemoryStream())
                using (Stream compressor = CreateStream(compressed, CompressionMode.Compress))
                {
                    foreach (byte b in source)
                    {
                        compressor.WriteByte(b);
                    }

                    compressor.Dispose();
                    compressedData = compressed.ToArray();
                }

                for (var i = 1; i <= compressedData.Length; i += 1)
                {
                    bool expectException = i < compressedData.Length;
                    using (var compressedStream = new MemoryStream(compressedData.Take(i).ToArray()))
                    {
                        using (Stream decompressor = CreateStream(compressedStream, CompressionMode.Decompress))
                        {
                            var decompressedStream = new MemoryStream();

                            try
                            {
                                switch (scenario)
                                {
                                    case TestScenario.Copy:
                                        decompressor.CopyTo(decompressedStream);
                                        break;

                                    case TestScenario.CopyAsync:
                                        await decompressor.CopyToAsync(decompressedStream);
                                        break;

                                    case TestScenario.Read:
                                        while (decompressor.Read(buffer, 0, buffer.Length) != 0) { }
                                        break;

                                    case TestScenario.ReadAsync:
                                        while (await decompressor.ReadAsync(buffer, 0, buffer.Length) != 0) { }
                                        break;

                                    case TestScenario.ReadByte:
                                        while (decompressor.ReadByte() != -1) { }
                                        break;

                                    case TestScenario.ReadByteAsync:
                                        while (await decompressor.ReadByteAsync() != -1) { }
                                        break;
                                }
                            }
                            catch (InvalidDataException e)
                            {
                                if (expectException)
                                    continue;

                                throw new XunitException($"An unexpected error occurred while decompressing data:{e}");
                            }

                            if (expectException)
                            {
                                throw new XunitException($"Truncated stream was decompressed successfully but exception was expected: length={i}/{compressedData.Length}");
                            }
                        }
                    }
                }
            }, testScenario.ToString()).Dispose();
        }

        [Fact]
        public void ZstandardStream_WithDecompressionOptions_DecompressesData()
        {
            byte[] testData = ZstandardTestUtils.CreateTestData();
            byte[] compressedData = new byte[ZstandardEncoder.GetMaxCompressedLength(testData.Length)];
            Assert.True(ZstandardEncoder.TryCompress(testData, compressedData, out int compressedLength));
            Array.Resize(ref compressedData, compressedLength);

            ZstandardDecompressionOptions options = new();
            using MemoryStream input = new(compressedData);
            using MemoryStream output = new();

            using (ZstandardStream decompressionStream = new(input, options, leaveOpen: true))
            {
                decompressionStream.CopyTo(output);
            }

            Assert.Equal(testData, output.ToArray());
        }

        [Fact]
        public void ZstandardStream_WithDecompressionOptions_NullOptions_ThrowsArgumentNullException()
        {
            using MemoryStream input = new();
            Assert.Throws<ArgumentNullException>("decompressionOptions", () => new ZstandardStream(input, (ZstandardDecompressionOptions)null!));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZstandardStream_WithDecompressionOptions_WithDictionary_RoundTrips(bool async)
        {
            byte[] dictionaryData = ZstandardTestUtils.CreateSampleDictionary();
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData);

            byte[] testData = ZstandardTestUtils.CreateTestData(5000);

            using MemoryStream compressedStream = new();
            using (ZstandardStream compressionStream = new(compressedStream, CompressionMode.Compress, dictionary, leaveOpen: true))
            {
                if (async)
                    await compressionStream.WriteAsync(testData, 0, testData.Length);
                else
                    compressionStream.Write(testData, 0, testData.Length);
            }

            compressedStream.Position = 0;

            ZstandardDecompressionOptions options = new() { Dictionary = dictionary };
            using MemoryStream decompressedStream = new();
            using (ZstandardStream decompressionStream = new(compressedStream, options))
            {
                if (async)
                    await decompressionStream.CopyToAsync(decompressedStream);
                else
                    decompressionStream.CopyTo(decompressedStream);
            }

            Assert.Equal(testData, decompressedStream.ToArray());
        }

        [Fact]
        public void ZstandardStream_WithDecompressionOptions_DisposedStream_ThrowsObjectDisposedException()
        {
            byte[] testData = ZstandardTestUtils.CreateTestData();
            byte[] compressedData = new byte[ZstandardEncoder.GetMaxCompressedLength(testData.Length)];
            Assert.True(ZstandardEncoder.TryCompress(testData, compressedData, out int compressedLength));
            Array.Resize(ref compressedData, compressedLength);

            ZstandardDecompressionOptions options = new();
            using MemoryStream input = new(compressedData);
            ZstandardStream decompressionStream = new(input, options);
            decompressionStream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => decompressionStream.Read(new byte[1], 0, 1));
        }

        // Compresses data into a single Zstandard frame.
        private static byte[] CompressToSingleFrame(byte[] data)
        {
            byte[] buffer = new byte[ZstandardEncoder.GetMaxCompressedLength(data.Length)];
            Assert.True(ZstandardEncoder.TryCompress(data, buffer, out int compressedLength));
            Array.Resize(ref buffer, compressedLength);
            return buffer;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZstandardStream_ConcatenatedFrames_DecompressesAllFrames(bool async)
        {
            // A Zstandard stream may be a sequence of frames concatenated back-to-back (RFC 8878 section 3),
            // as produced by Content-Encoding: zstd for large HTTP bodies; decoding must cover every frame,
            // not just the first. Two differently sized frames so the summed output length alone proves both
            // were decoded.
            byte[] first = ZstandardTestUtils.CreateTestData(300);
            byte[] second = ZstandardTestUtils.CreateTestData(200);
            byte[] expected = [.. first, .. second];

            byte[] body = [.. CompressToSingleFrame(first), .. CompressToSingleFrame(second)];

            using MemoryStream input = new(body);
            using MemoryStream output = new();
            using (ZstandardStream decompressor = new(input, CompressionMode.Decompress, leaveOpen: true))
            {
                if (async)
                {
                    await decompressor.CopyToAsync(output);
                }
                else
                {
                    decompressor.CopyTo(output);
                }
            }

            Assert.Equal(expected.Length, output.Length);
            Assert.Equal(expected, output.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZstandardStream_ConcatenatedFrames_AcrossReads_DecompressesAllFrames(bool async)
        {
            // The next frame's magic number can be split across underlying reads. A stream that yields a
            // single byte per read forces every frame boundary to be discovered across multiple reads, and
            // also exercises the non-seekable path (no rewind), as used by HttpClient automatic decompression.
            byte[] first = ZstandardTestUtils.CreateTestData(8_000);
            byte[] second = ZstandardTestUtils.CreateTestData(5_000);
            byte[] expected = [.. first, .. second];

            byte[] body = [.. CompressToSingleFrame(first), .. CompressToSingleFrame(second)];

            using Stream input = new SingleByteReadStream(body);
            using MemoryStream output = new();
            using (ZstandardStream decompressor = new(input, CompressionMode.Decompress, leaveOpen: true))
            {
                if (async)
                {
                    await decompressor.CopyToAsync(output);
                }
                else
                {
                    decompressor.CopyTo(output);
                }
            }

            Assert.Equal(expected, output.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZstandardStream_EmptyFramesAmongFrames_DecompressesAllFrames(bool async)
        {
            // A Zstandard frame can decode to zero bytes (for example, a frame whose content is empty). The
            // decoder reports end-of-frame for it the same as any other frame, so a zero-output frame must be
            // skipped rather than mistaken for the end of the stream, whether it is the leading, an
            // intermediate, or the trailing frame.
            byte[] first = ZstandardTestUtils.CreateTestData(5_000);
            byte[] second = ZstandardTestUtils.CreateTestData(7_000);
            byte[] expected = [.. first, .. second];

            byte[] body =
            [
                .. CompressToSingleFrame([]),
                .. CompressToSingleFrame(first),
                .. CompressToSingleFrame([]),
                .. CompressToSingleFrame(second),
                .. CompressToSingleFrame([]),
            ];

            using MemoryStream input = new(body);
            using MemoryStream output = new();
            using (ZstandardStream decompressor = new(input, CompressionMode.Decompress, leaveOpen: true))
            {
                if (async)
                {
                    await decompressor.CopyToAsync(output);
                }
                else
                {
                    decompressor.CopyTo(output);
                }
            }

            Assert.Equal(expected, output.ToArray());
        }

        [Theory]
        [InlineData(false, 1)]
        [InlineData(true, 1)]
        [InlineData(false, 2)]
        [InlineData(true, 2)]
        [InlineData(false, 3)]
        [InlineData(true, 3)]
        public async Task ZstandardStream_FrameFollowedByShortTrailingData_StopsAtEndOfFrame(bool async, int trailingLength)
        {
            // Trailing data shorter than a frame magic number (1-3 bytes) after the final frame. This is the
            // boundary case where the decoder cannot immediately tell a split next-frame magic from trailing
            // data; the stream must still end cleanly and leave the trailing bytes on the (seekable) base
            // stream. The 4-or-more-byte trailing-data case is covered by the inherited
            // AutomaticStreamRewinds_WhenDecompressionFinishes test (CompressionStreamUnitTestBase).
            byte[] payload = ZstandardTestUtils.CreateTestData(10_000);
            byte[] frame = CompressToSingleFrame(payload);
            byte[] trailing = Enumerable.Range(1, trailingLength).Select(i => (byte)i).ToArray();

            using MemoryStream input = new([.. frame, .. trailing]);
            using MemoryStream output = new();
            using (ZstandardStream decompressor = new(input, CompressionMode.Decompress, leaveOpen: true))
            {
                if (async)
                {
                    await decompressor.CopyToAsync(output);
                }
                else
                {
                    decompressor.CopyTo(output);
                }
            }

            Assert.Equal(payload, output.ToArray());

            byte[] remainder = new byte[trailing.Length];
            int read = input.Read(remainder, 0, remainder.Length);
            Assert.Equal(trailing.Length, read);
            Assert.Equal(trailing, remainder);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZstandardStream_FrameFollowedByCorruptFrame_Throws(bool async)
        {
            // A continuation chunk that begins with a valid frame magic but a corrupt header is corrupt input,
            // the same as a corrupt first frame, so it must throw rather than be silently dropped as trailing data.
            byte[] first = ZstandardTestUtils.CreateTestData(4_000);
            byte[] corruptFrame = CompressToSingleFrame(ZstandardTestUtils.CreateTestData(4_000));

            // Keep the 4-byte magic intact (so it is recognized as a following frame) but set the reserved
            // bit in the frame header descriptor (the byte right after the magic), which a compliant decoder
            // must reject.
            corruptFrame[4] |= 0x08;

            byte[] body = [.. CompressToSingleFrame(first), .. corruptFrame];

            using MemoryStream input = new(body);
            using MemoryStream output = new();
            using ZstandardStream decompressor = new(input, CompressionMode.Decompress, leaveOpen: true);

            if (async)
            {
                await Assert.ThrowsAsync<InvalidDataException>(() => decompressor.CopyToAsync(output));
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => decompressor.CopyTo(output));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ZstandardStream_SkippableFrameBetweenFrames_DecompressesAllFrames(bool async)
        {
            // A skippable frame may appear between data frames and must not prevent subsequent
            // frames from being decoded (RFC 8878 section 3.1.2).
            byte[] first = ZstandardTestUtils.CreateTestData(4_000);
            byte[] second = ZstandardTestUtils.CreateTestData(6_000);
            byte[] expected = [.. first, .. second];

            // A minimal skippable frame: magic 0x184D2A50, a 4-byte little-endian content size of 4, then 4
            // bytes of (ignored) user data.
            byte[] skippable = [0x50, 0x2A, 0x4D, 0x18, 0x04, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04];

            byte[] body = [.. CompressToSingleFrame(first), .. skippable, .. CompressToSingleFrame(second)];

            using MemoryStream input = new(body);
            using MemoryStream output = new();
            using (ZstandardStream decompressor = new(input, CompressionMode.Decompress, leaveOpen: true))
            {
                if (async)
                {
                    await decompressor.CopyToAsync(output);
                }
                else
                {
                    decompressor.CopyTo(output);
                }
            }

            Assert.Equal(expected, output.ToArray());
        }

        // A non-seekable, read-only stream that returns at most one byte per read.
        private sealed class SingleByteReadStream : Stream
        {
            private readonly byte[] _data;
            private int _position;

            public SingleByteReadStream(byte[] data) => _data = data;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

            public override int Read(Span<byte> buffer)
            {
                if (buffer.IsEmpty || _position >= _data.Length)
                {
                    return 0;
                }

                buffer[0] = _data[_position++];
                return 1;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled<int>(cancellationToken);
                }

                return Task.FromResult(Read(buffer.AsSpan(offset, count)));
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return ValueTask.FromCanceled<int>(cancellationToken);
                }

                return new ValueTask<int>(Read(buffer.Span));
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
