// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Compression
{
    public class ZlibEncoderDecoderTests
    {
        private static readonly byte[] s_sampleData = Encoding.UTF8.GetBytes(
            "Hello, World! This is a test string for compression. " +
            "We need some repeated content to make compression effective. " +
            "Hello, World! This is a test string for compression. " +
            "The quick brown fox jumps over the lazy dog. " +
            "Sphinx of black quartz, judge my vow.");

        #region ZlibEncoder Tests

        [Fact]
        public void ZlibEncoder_Ctor_InvalidCompressionLevel_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZlibEncoder((CompressionLevel)(-1), ZlibCompressionFormat.Deflate));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZlibEncoder((CompressionLevel)99, ZlibCompressionFormat.Deflate));
        }

        [Fact]
        public void ZlibEncoder_Ctor_InvalidFormat_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZlibEncoder(CompressionLevel.Optimal, (ZlibCompressionFormat)99));
        }

        [Fact]
        public void ZlibEncoder_Ctor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ZlibEncoder(null!));
        }

        [Theory]
        [InlineData(ZlibCompressionFormat.Deflate)]
        [InlineData(ZlibCompressionFormat.ZLib)]
        [InlineData(ZlibCompressionFormat.GZip)]
        public void ZlibEncoder_Compress_AllFormats(ZlibCompressionFormat format)
        {
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, format);
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);
            Assert.True(bytesWritten < s_sampleData.Length); // Compression should reduce size
        }

        [Fact]
        public void ZlibEncoder_Dispose_MultipleCallsSafe()
        {
            var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            encoder.Dispose();
            encoder.Dispose(); // Should not throw
        }

        [Fact]
        public void ZlibEncoder_Compress_AfterDispose_Throws()
        {
            var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            encoder.Dispose();

            byte[] buffer = new byte[100];
            Assert.Throws<ObjectDisposedException>(() =>
                encoder.Compress(s_sampleData, buffer, out _, out _, isFinalBlock: true));
        }

        [Fact]
        public void ZlibEncoder_Compress_AfterFinished_ReturnsDone()
        {
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            // First compression with final block
            encoder.Compress(s_sampleData, destination, out _, out _, isFinalBlock: true);

            // Second call after finished should return Done immediately
            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, consumed);
            Assert.Equal(0, written);
        }

        [Fact]
        public void ZlibEncoder_Reset_AllowsReuse()
        {
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            // First compression
            encoder.Compress(s_sampleData, destination, out _, out int firstBytesWritten, isFinalBlock: true);

            // Reset
            encoder.Reset();

            // Second compression should work
            Array.Clear(destination);
            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int secondBytesWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.Equal(firstBytesWritten, secondBytesWritten); // Should produce same output
        }

        [Fact]
        public void ZlibEncoder_GetMaxCompressedLength_ValidValues()
        {
            Assert.True(ZlibEncoder.GetMaxCompressedLength(0) >= 0);
            Assert.True(ZlibEncoder.GetMaxCompressedLength(100) >= 100);
            Assert.True(ZlibEncoder.GetMaxCompressedLength(1000) >= 1000);
        }

        [Fact]
        public void ZlibEncoder_GetMaxCompressedLength_NegativeInput_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZlibEncoder.GetMaxCompressedLength(-1));
        }

        [Fact]
        public void ZlibEncoder_TryCompress_Success()
        {
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            bool success = ZlibEncoder.TryCompress(s_sampleData, destination, out int bytesWritten);

            Assert.True(success);
            Assert.True(bytesWritten > 0);
        }

        [Fact]
        public void ZlibEncoder_TryCompress_DestinationTooSmall_ReturnsFalse()
        {
            byte[] destination = new byte[1]; // Too small

            bool success = ZlibEncoder.TryCompress(s_sampleData, destination, out int bytesWritten);

            Assert.False(success);
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]      // Default - maps to level 6
        [InlineData(CompressionLevel.NoCompression)] // No compression
        [InlineData(CompressionLevel.Fastest)]      // Best speed - maps to level 1
        [InlineData(CompressionLevel.SmallestSize)] // Best compression - maps to level 9
        public void ZlibEncoder_CompressionLevels(CompressionLevel level)
        {
            using var encoder = new ZlibEncoder(level, ZlibCompressionFormat.Deflate);
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        [Theory]
        [InlineData(ZLibCompressionStrategy.Default)]
        [InlineData(ZLibCompressionStrategy.Filtered)]
        [InlineData(ZLibCompressionStrategy.HuffmanOnly)]
        [InlineData(ZLibCompressionStrategy.RunLengthEncoding)]
        [InlineData(ZLibCompressionStrategy.Fixed)]
        public void ZlibEncoder_CompressionStrategies(ZLibCompressionStrategy strategy)
        {
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate, strategy);
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void ZlibEncoder_Flush()
        {
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            // Write some data without finalizing
            encoder.Compress(s_sampleData.AsSpan(0, 50), destination, out _, out int written1, isFinalBlock: false);

            // Flush - may return Done, DestinationTooSmall, or NeedMoreData depending on internal state
            OperationStatus status = encoder.Flush(destination.AsSpan(written1), out int flushedBytes);

            // Just verify it returns a valid status and doesn't throw
            Assert.True(
                status == OperationStatus.Done ||
                status == OperationStatus.DestinationTooSmall ||
                status == OperationStatus.NeedMoreData,
                $"Unexpected status: {status}");
        }

        [Fact]
        public void ZlibEncoder_DestinationTooSmall()
        {
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            byte[] destination = new byte[5]; // Very small

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.DestinationTooSmall, status);
            Assert.True(consumed >= 0);
            Assert.True(written >= 0);
        }

        [Fact]
        public void ZlibEncoder_EmptySource()
        {
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            byte[] destination = new byte[100];

            OperationStatus status = encoder.Compress(ReadOnlySpan<byte>.Empty, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, consumed);
            Assert.True(written > 0); // Should still write end-of-stream marker
        }

        [Fact]
        public void ZlibEncoder_WithOptions()
        {
            var options = new ZlibEncoderOptions
            {
                CompressionLevel = 9,
                Format = ZlibCompressionFormat.GZip,
                CompressionStrategy = ZLibCompressionStrategy.Filtered
            };

            using var encoder = new ZlibEncoder(options);
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        #endregion

        #region ZlibDecoder Tests

        [Fact]
        public void ZlibDecoder_Ctor_InvalidFormat_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZlibDecoder((ZlibCompressionFormat)99));
        }

        [Theory]
        [InlineData(ZlibCompressionFormat.Deflate)]
        [InlineData(ZlibCompressionFormat.ZLib)]
        [InlineData(ZlibCompressionFormat.GZip)]
        public void ZlibDecoder_Decompress_AllFormats(ZlibCompressionFormat format)
        {
            // First, compress the data
            byte[] compressed = CompressData(s_sampleData, format);

            // Then decompress
            using var decoder = new ZlibDecoder(format);
            byte[] decompressed = new byte[s_sampleData.Length * 2]; // Extra room

            OperationStatus status = decoder.Decompress(compressed, decompressed, out int bytesConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(compressed.Length, bytesConsumed);
            Assert.Equal(s_sampleData.Length, bytesWritten);
            Assert.Equal(s_sampleData, decompressed.AsSpan(0, bytesWritten).ToArray());
        }

        [Fact]
        public void ZlibDecoder_Dispose_MultipleCallsSafe()
        {
            var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            decoder.Dispose();
            decoder.Dispose(); // Should not throw
        }

        [Fact]
        public void ZlibDecoder_Decompress_AfterDispose_Throws()
        {
            var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            decoder.Dispose();

            byte[] buffer = new byte[100];
            Assert.Throws<ObjectDisposedException>(() =>
                decoder.Decompress(s_sampleData, buffer, out _, out _));
        }

        [Fact]
        public void ZlibDecoder_Decompress_AfterFinished_ReturnsDone()
        {
            byte[] compressed = CompressData(s_sampleData, ZlibCompressionFormat.Deflate);
            using var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            byte[] decompressed = new byte[s_sampleData.Length * 2];

            // First decompression
            decoder.Decompress(compressed, decompressed, out _, out _);

            // Second call after finished should return Done immediately
            OperationStatus status = decoder.Decompress(compressed, decompressed, out int consumed, out int written);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(0, consumed);
            Assert.Equal(0, written);
        }

        [Fact]
        public void ZlibDecoder_Reset_AllowsReuse()
        {
            byte[] compressed = CompressData(s_sampleData, ZlibCompressionFormat.Deflate);
            using var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            byte[] decompressed = new byte[s_sampleData.Length * 2];

            // First decompression
            decoder.Decompress(compressed, decompressed, out _, out int firstBytesWritten);

            // Reset
            decoder.Reset();

            // Second decompression should work
            Array.Clear(decompressed);
            OperationStatus status = decoder.Decompress(compressed, decompressed, out int consumed, out int secondBytesWritten);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(compressed.Length, consumed);
            Assert.Equal(firstBytesWritten, secondBytesWritten);
            Assert.Equal(s_sampleData, decompressed.AsSpan(0, secondBytesWritten).ToArray());
        }

        [Fact]
        public void ZlibDecoder_TryDecompress_Success()
        {
            byte[] compressed = CompressData(s_sampleData, ZlibCompressionFormat.Deflate);
            byte[] decompressed = new byte[s_sampleData.Length * 2];

            bool success = ZlibDecoder.TryDecompress(compressed, decompressed, out int bytesWritten, ZlibCompressionFormat.Deflate);

            Assert.True(success);
            Assert.Equal(s_sampleData.Length, bytesWritten);
            Assert.Equal(s_sampleData, decompressed.AsSpan(0, bytesWritten).ToArray());
        }

        [Fact]
        public void ZlibDecoder_TryDecompress_DestinationTooSmall_ReturnsFalse()
        {
            byte[] compressed = CompressData(s_sampleData, ZlibCompressionFormat.Deflate);
            byte[] decompressed = new byte[1]; // Too small

            bool success = ZlibDecoder.TryDecompress(compressed, decompressed, out int bytesWritten, ZlibCompressionFormat.Deflate);

            Assert.False(success);
        }

        [Fact]
        public void ZlibDecoder_DestinationTooSmall()
        {
            byte[] compressed = CompressData(s_sampleData, ZlibCompressionFormat.Deflate);
            using var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            byte[] decompressed = new byte[5]; // Too small

            OperationStatus status = decoder.Decompress(compressed, decompressed, out int consumed, out int written);

            Assert.Equal(OperationStatus.DestinationTooSmall, status);
        }

        [Fact]
        public void ZlibDecoder_EmptySource()
        {
            using var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            byte[] decompressed = new byte[100];

            OperationStatus status = decoder.Decompress(ReadOnlySpan<byte>.Empty, decompressed, out int consumed, out int written);

            Assert.Equal(OperationStatus.NeedMoreData, status);
            Assert.Equal(0, consumed);
            Assert.Equal(0, written);
        }

        [Fact]
        public void ZlibDecoder_InvalidData()
        {
            using var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            byte[] garbage = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC, 0xFB };
            byte[] decompressed = new byte[100];

            OperationStatus status = decoder.Decompress(garbage, decompressed, out _, out _);

            Assert.Equal(OperationStatus.InvalidData, status);
        }

        #endregion

        #region Round-Trip Tests

        [Theory]
        [InlineData(ZlibCompressionFormat.Deflate)]
        [InlineData(ZlibCompressionFormat.ZLib)]
        [InlineData(ZlibCompressionFormat.GZip)]
        public void RoundTrip_WithState(ZlibCompressionFormat format)
        {
            byte[] compressed = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            byte[] decompressed = new byte[s_sampleData.Length];

            // Compress
            using (var encoder = new ZlibEncoder(CompressionLevel.Optimal, format))
            {
                OperationStatus compressStatus = encoder.Compress(s_sampleData, compressed, out _, out int written, isFinalBlock: true);
                Assert.Equal(OperationStatus.Done, compressStatus);
                compressed = compressed.AsSpan(0, written).ToArray();
            }

            // Decompress
            using (var decoder = new ZlibDecoder(format))
            {
                OperationStatus decompressStatus = decoder.Decompress(compressed, decompressed, out int consumed, out int written);
                Assert.Equal(OperationStatus.Done, decompressStatus);
                Assert.Equal(compressed.Length, consumed);
                Assert.Equal(s_sampleData.Length, written);
            }

            Assert.Equal(s_sampleData, decompressed);
        }

        [Theory]
        [InlineData(ZlibCompressionFormat.Deflate)]
        [InlineData(ZlibCompressionFormat.ZLib)]
        [InlineData(ZlibCompressionFormat.GZip)]
        public void RoundTrip_Static(ZlibCompressionFormat format)
        {
            byte[] compressed = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            byte[] decompressed = new byte[s_sampleData.Length];

            bool compressSuccess = ZlibEncoder.TryCompress(s_sampleData, compressed, out int compressedSize, CompressionLevel.Optimal, format);
            Assert.True(compressSuccess);

            compressed = compressed.AsSpan(0, compressedSize).ToArray();

            bool decompressSuccess = ZlibDecoder.TryDecompress(compressed, decompressed, out int decompressedSize, format);
            Assert.True(decompressSuccess);
            Assert.Equal(s_sampleData.Length, decompressedSize);

            Assert.Equal(s_sampleData, decompressed);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(100000)]
        public void RoundTrip_VariousSizes(int size)
        {
            byte[] original = new byte[size];
            Random.Shared.NextBytes(original);

            byte[] compressed = new byte[ZlibEncoder.GetMaxCompressedLength(size)];
            byte[] decompressed = new byte[size];

            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            encoder.Compress(original, compressed, out _, out int compressedSize, isFinalBlock: true);

            using var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out _, out int decompressedSize);

            Assert.Equal(size, decompressedSize);
            Assert.Equal(original, decompressed);
        }

        [Fact]
        public void RoundTrip_Chunks()
        {
            int chunkSize = 100;
            int totalSize = 2000;
            byte[] original = new byte[totalSize];
            Random.Shared.NextBytes(original);

            byte[] allCompressed = new byte[ZlibEncoder.GetMaxCompressedLength(totalSize)];
            int totalCompressed = 0;

            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);

            // Compress in chunks
            for (int i = 0; i < totalSize; i += chunkSize)
            {
                int remaining = Math.Min(chunkSize, totalSize - i);
                bool isFinal = (i + remaining) >= totalSize;

                OperationStatus status = encoder.Compress(
                    original.AsSpan(i, remaining),
                    allCompressed.AsSpan(totalCompressed),
                    out int consumed,
                    out int written,
                    isFinalBlock: isFinal);

                totalCompressed += written;

                if (!isFinal)
                {
                    // Flush intermediate data
                    encoder.Flush(allCompressed.AsSpan(totalCompressed), out int flushed);
                    totalCompressed += flushed;
                }
            }

            // Decompress
            using var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
            byte[] decompressed = new byte[totalSize];

            OperationStatus decompressStatus = decoder.Decompress(
                allCompressed.AsSpan(0, totalCompressed),
                decompressed,
                out int bytesConsumed,
                out int bytesWritten);

            Assert.Equal(OperationStatus.Done, decompressStatus);
            Assert.Equal(totalSize, bytesWritten);
            Assert.Equal(original, decompressed);
        }

        #endregion

        #region Comparison with Stream-based APIs

        [Theory]
        [InlineData(ZlibCompressionFormat.Deflate)]
        [InlineData(ZlibCompressionFormat.ZLib)]
        [InlineData(ZlibCompressionFormat.GZip)]
        public void Compare_EncoderOutput_MatchesStreamOutput(ZlibCompressionFormat format)
        {
            // Compress with span-based API
            byte[] spanCompressed = CompressData(s_sampleData, format);

            // Compress with stream-based API
            byte[] streamCompressed = CompressWithStream(s_sampleData, format);

            // Both should decompress to the same data
            byte[] fromSpan = DecompressWithStream(spanCompressed, format);
            byte[] fromStream = DecompressWithStream(streamCompressed, format);

            Assert.Equal(s_sampleData, fromSpan);
            Assert.Equal(s_sampleData, fromStream);
        }

        [Theory]
        [InlineData(ZlibCompressionFormat.Deflate)]
        [InlineData(ZlibCompressionFormat.ZLib)]
        [InlineData(ZlibCompressionFormat.GZip)]
        public void Compare_StreamCompressed_CanDecompressWithDecoder(ZlibCompressionFormat format)
        {
            // Compress with stream
            byte[] streamCompressed = CompressWithStream(s_sampleData, format);

            // Decompress with span-based decoder
            using var decoder = new ZlibDecoder(format);
            byte[] decompressed = new byte[s_sampleData.Length * 2];

            OperationStatus status = decoder.Decompress(streamCompressed, decompressed, out int consumed, out int written);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, written);
            Assert.Equal(s_sampleData, decompressed.AsSpan(0, written).ToArray());
        }

        [Theory]
        [InlineData(ZlibCompressionFormat.Deflate)]
        [InlineData(ZlibCompressionFormat.ZLib)]
        [InlineData(ZlibCompressionFormat.GZip)]
        public void Compare_EncoderCompressed_CanDecompressWithStream(ZlibCompressionFormat format)
        {
            // Compress with span-based encoder
            byte[] spanCompressed = CompressData(s_sampleData, format);

            // Decompress with stream
            byte[] decompressed = DecompressWithStream(spanCompressed, format);

            Assert.Equal(s_sampleData, decompressed);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Compress_HighlyCompressibleData()
        {
            // All zeros - should compress very well
            byte[] zeros = new byte[10000];
            byte[] compressed = new byte[ZlibEncoder.GetMaxCompressedLength(zeros.Length)];

            using var encoder = new ZlibEncoder(CompressionLevel.SmallestSize, ZlibCompressionFormat.Deflate);
            encoder.Compress(zeros, compressed, out _, out int written, isFinalBlock: true);

            // Should compress to much smaller size
            Assert.True(written < zeros.Length / 10, $"Expected significant compression, got {written} bytes from {zeros.Length} bytes");
        }

        [Fact]
        public void Compress_IncompressibleData()
        {
            // Random data - won't compress well
            byte[] random = new byte[1000];
            new Random(42).NextBytes(random);
            byte[] compressed = new byte[ZlibEncoder.GetMaxCompressedLength(random.Length)];

            using var encoder = new ZlibEncoder(CompressionLevel.SmallestSize, ZlibCompressionFormat.Deflate);
            encoder.Compress(random, compressed, out _, out int written, isFinalBlock: true);

            // Random data might even expand slightly
            Assert.True(written > 0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void RoundTrip_SmallData(int size)
        {
            byte[] original = new byte[size];
            if (size > 0)
            {
                Random.Shared.NextBytes(original);
            }

            byte[] compressed = new byte[ZlibEncoder.GetMaxCompressedLength(size) + 50];
            byte[] decompressed = new byte[Math.Max(size, 1)];

            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            var compressStatus = encoder.Compress(original, compressed, out _, out int compressedSize, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, compressStatus);

            if (size > 0)
            {
                using var decoder = new ZlibDecoder(ZlibCompressionFormat.Deflate);
                var decompressStatus = decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out _, out int decompressedSize);
                Assert.Equal(OperationStatus.Done, decompressStatus);
                Assert.Equal(size, decompressedSize);
                Assert.Equal(original, decompressed.AsSpan(0, size).ToArray());
            }
        }

        [Fact]
        public void MultipleResets_Work()
        {
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, ZlibCompressionFormat.Deflate);
            byte[] destination = new byte[ZlibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            for (int i = 0; i < 5; i++)
            {
                encoder.Compress(s_sampleData, destination, out _, out int written, isFinalBlock: true);
                Assert.True(written > 0);
                encoder.Reset();
            }
        }

        #endregion

        #region Helper Methods

        private static byte[] CompressData(byte[] data, ZlibCompressionFormat format)
        {
            byte[] compressed = new byte[ZlibEncoder.GetMaxCompressedLength(data.Length)];
            using var encoder = new ZlibEncoder(CompressionLevel.Optimal, format);
            encoder.Compress(data, compressed, out _, out int written, isFinalBlock: true);
            return compressed.AsSpan(0, written).ToArray();
        }

        private static byte[] CompressWithStream(byte[] data, ZlibCompressionFormat format)
        {
            using var output = new MemoryStream();
            using (Stream compressor = CreateCompressionStream(output, format))
            {
                compressor.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] DecompressWithStream(byte[] data, ZlibCompressionFormat format)
        {
            using var input = new MemoryStream(data);
            using Stream decompressor = CreateDecompressionStream(input, format);
            using var output = new MemoryStream();
            decompressor.CopyTo(output);
            return output.ToArray();
        }

        private static Stream CreateCompressionStream(Stream stream, ZlibCompressionFormat format)
        {
            return format switch
            {
                ZlibCompressionFormat.Deflate => new DeflateStream(stream, CompressionLevel.Optimal, leaveOpen: true),
                ZlibCompressionFormat.ZLib => new ZLibStream(stream, CompressionLevel.Optimal, leaveOpen: true),
                ZlibCompressionFormat.GZip => new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true),
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

        private static Stream CreateDecompressionStream(Stream stream, ZlibCompressionFormat format)
        {
            return format switch
            {
                ZlibCompressionFormat.Deflate => new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true),
                ZlibCompressionFormat.ZLib => new ZLibStream(stream, CompressionMode.Decompress, leaveOpen: true),
                ZlibCompressionFormat.GZip => new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true),
                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
        }

        #endregion
    }
}
