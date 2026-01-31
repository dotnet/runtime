// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Xunit;

namespace System.IO.Compression
{
    public class DeflateEncoderDecoderTests
    {
        private static readonly byte[] s_sampleData = Encoding.UTF8.GetBytes(
            "Hello, World! This is a test string for compression. " +
            "We need some repeated content to make compression effective. " +
            "Hello, World! This is a test string for compression. " +
            "The quick brown fox jumps over the lazy dog. " +
            "Sphinx of black quartz, judge my vow.");

        #region DeflateEncoder Tests

        [Fact]
        public void DeflateEncoder_Ctor_InvalidCompressionLevel_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeflateEncoder((CompressionLevel)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeflateEncoder((CompressionLevel)99));
        }

        [Fact]
        public void DeflateEncoder_Ctor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DeflateEncoder(null!));
        }

        [Fact]
        public void DeflateEncoder_Compress_Success()
        {
            using var encoder = new DeflateEncoder(CompressionLevel.Optimal);
            byte[] destination = new byte[DeflateEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);
            Assert.True(bytesWritten < s_sampleData.Length);
        }

        [Fact]
        public void DeflateEncoder_Dispose_MultipleCallsSafe()
        {
            var encoder = new DeflateEncoder(CompressionLevel.Optimal);
            encoder.Dispose();
            encoder.Dispose();
        }

        [Fact]
        public void DeflateEncoder_Compress_AfterDispose_Throws()
        {
            var encoder = new DeflateEncoder(CompressionLevel.Optimal);
            encoder.Dispose();

            byte[] buffer = new byte[100];
            Assert.Throws<ObjectDisposedException>(() =>
                encoder.Compress(s_sampleData, buffer, out _, out _, isFinalBlock: true));
        }

        [Fact]
        public void DeflateEncoder_GetMaxCompressedLength_ValidValues()
        {
            Assert.True(DeflateEncoder.GetMaxCompressedLength(0) >= 0);
            Assert.True(DeflateEncoder.GetMaxCompressedLength(100) >= 100);
            Assert.True(DeflateEncoder.GetMaxCompressedLength(1000) >= 1000);
        }

        [Fact]
        public void DeflateEncoder_GetMaxCompressedLength_NegativeInput_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DeflateEncoder.GetMaxCompressedLength(-1));
        }

        [Fact]
        public void DeflateEncoder_TryCompress_Success()
        {
            byte[] destination = new byte[DeflateEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            bool success = DeflateEncoder.TryCompress(s_sampleData, destination, out int bytesWritten);

            Assert.True(success);
            Assert.True(bytesWritten > 0);
        }

        [Fact]
        public void DeflateEncoder_TryCompress_DestinationTooSmall_ReturnsFalse()
        {
            byte[] destination = new byte[1];

            bool success = DeflateEncoder.TryCompress(s_sampleData, destination, out int bytesWritten);

            Assert.False(success);
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.NoCompression)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.SmallestSize)]
        public void DeflateEncoder_CompressionLevels(CompressionLevel level)
        {
            using var encoder = new DeflateEncoder(level);
            byte[] destination = new byte[DeflateEncoder.GetMaxCompressedLength(s_sampleData.Length)];

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
        public void DeflateEncoder_CompressionStrategies(ZLibCompressionStrategy strategy)
        {
            using var encoder = new DeflateEncoder(CompressionLevel.Optimal, strategy);
            byte[] destination = new byte[DeflateEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void DeflateEncoder_WithOptions()
        {
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 9,
                CompressionStrategy = ZLibCompressionStrategy.Filtered
            };

            using var encoder = new DeflateEncoder(options);
            byte[] destination = new byte[DeflateEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        #endregion

        #region DeflateDecoder Tests

        [Fact]
        public void DeflateDecoder_Decompress_Success()
        {
            byte[] compressed = new byte[DeflateEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            using var encoder = new DeflateEncoder(CompressionLevel.Optimal);
            encoder.Compress(s_sampleData, compressed, out _, out int compressedSize, isFinalBlock: true);

            using var decoder = new DeflateDecoder();
            byte[] decompressed = new byte[s_sampleData.Length];

            OperationStatus status = decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out int consumed, out int written);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(compressedSize, consumed);
            Assert.Equal(s_sampleData.Length, written);
            Assert.Equal(s_sampleData, decompressed);
        }

        [Fact]
        public void DeflateDecoder_Dispose_MultipleCallsSafe()
        {
            var decoder = new DeflateDecoder();
            decoder.Dispose();
            decoder.Dispose();
        }

        [Fact]
        public void DeflateDecoder_Decompress_AfterDispose_Throws()
        {
            var decoder = new DeflateDecoder();
            decoder.Dispose();

            byte[] buffer = new byte[100];
            Assert.Throws<ObjectDisposedException>(() =>
                decoder.Decompress(buffer, buffer, out _, out _));
        }

        [Fact]
        public void DeflateDecoder_TryDecompress_Success()
        {
            byte[] compressed = new byte[DeflateEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            using var encoder = new DeflateEncoder(CompressionLevel.Optimal);
            encoder.Compress(s_sampleData, compressed, out _, out int compressedSize, isFinalBlock: true);

            byte[] decompressed = new byte[s_sampleData.Length];

            bool success = DeflateDecoder.TryDecompress(compressed.AsSpan(0, compressedSize), decompressed, out int bytesWritten);

            Assert.True(success);
            Assert.Equal(s_sampleData.Length, bytesWritten);
            Assert.Equal(s_sampleData, decompressed);
        }

        [Fact]
        public void DeflateDecoder_InvalidData_ReturnsInvalidData()
        {
            byte[] invalidData = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

            using var decoder = new DeflateDecoder();
            byte[] decompressed = new byte[100];

            OperationStatus status = decoder.Decompress(invalidData, decompressed, out _, out _);

            Assert.Equal(OperationStatus.InvalidData, status);
        }

        #endregion

        #region RoundTrip Tests

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void DeflateEncoder_Decoder_RoundTrip(int dataSize)
        {
            byte[] original = new byte[dataSize];
            new Random(42).NextBytes(original);

            byte[] compressed = new byte[DeflateEncoder.GetMaxCompressedLength(dataSize)];
            using var encoder = new DeflateEncoder(CompressionLevel.Optimal);
            OperationStatus compressStatus = encoder.Compress(original, compressed, out _, out int compressedSize, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, compressStatus);

            byte[] decompressed = new byte[dataSize];
            using var decoder = new DeflateDecoder();
            OperationStatus decompressStatus = decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out _, out int decompressedSize);

            Assert.Equal(OperationStatus.Done, decompressStatus);
            Assert.Equal(dataSize, decompressedSize);
            Assert.Equal(original, decompressed);
        }

        [Fact]
        public void DeflateEncoder_Decoder_RoundTrip_AllCompressionLevels()
        {
            foreach (CompressionLevel level in Enum.GetValues<CompressionLevel>())
            {
                byte[] compressed = new byte[DeflateEncoder.GetMaxCompressedLength(s_sampleData.Length)];
                using var encoder = new DeflateEncoder(level);
                encoder.Compress(s_sampleData, compressed, out _, out int compressedSize, isFinalBlock: true);

                byte[] decompressed = new byte[s_sampleData.Length];
                using var decoder = new DeflateDecoder();
                decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out _, out _);

                Assert.Equal(s_sampleData, decompressed);
            }
        }

        #endregion
    }

    public class ZLibEncoderDecoderTests
    {
        private static readonly byte[] s_sampleData = Encoding.UTF8.GetBytes(
            "Hello, World! This is a test string for compression. " +
            "We need some repeated content to make compression effective. " +
            "Hello, World! This is a test string for compression. " +
            "The quick brown fox jumps over the lazy dog. " +
            "Sphinx of black quartz, judge my vow.");

        #region ZLibEncoder Tests

        [Fact]
        public void ZLibEncoder_Ctor_InvalidCompressionLevel_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZLibEncoder((CompressionLevel)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZLibEncoder((CompressionLevel)99));
        }

        [Fact]
        public void ZLibEncoder_Ctor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ZLibEncoder(null!));
        }

        [Fact]
        public void ZLibEncoder_Compress_Success()
        {
            using var encoder = new ZLibEncoder(CompressionLevel.Optimal);
            byte[] destination = new byte[ZLibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);
            Assert.True(bytesWritten < s_sampleData.Length);
        }

        [Fact]
        public void ZLibEncoder_TryCompress_Success()
        {
            byte[] destination = new byte[ZLibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            bool success = ZLibEncoder.TryCompress(s_sampleData, destination, out int bytesWritten);

            Assert.True(success);
            Assert.True(bytesWritten > 0);
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.NoCompression)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.SmallestSize)]
        public void ZLibEncoder_CompressionLevels(CompressionLevel level)
        {
            using var encoder = new ZLibEncoder(level);
            byte[] destination = new byte[ZLibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void ZLibEncoder_WithOptions()
        {
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 9,
                CompressionStrategy = ZLibCompressionStrategy.Filtered
            };

            using var encoder = new ZLibEncoder(options);
            byte[] destination = new byte[ZLibEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        #endregion

        #region ZLibDecoder Tests

        [Fact]
        public void ZLibDecoder_Decompress_Success()
        {
            byte[] compressed = new byte[ZLibEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            using var encoder = new ZLibEncoder(CompressionLevel.Optimal);
            encoder.Compress(s_sampleData, compressed, out _, out int compressedSize, isFinalBlock: true);

            using var decoder = new ZLibDecoder();
            byte[] decompressed = new byte[s_sampleData.Length];

            OperationStatus status = decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out int consumed, out int written);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(compressedSize, consumed);
            Assert.Equal(s_sampleData.Length, written);
            Assert.Equal(s_sampleData, decompressed);
        }

        [Fact]
        public void ZLibDecoder_TryDecompress_Success()
        {
            byte[] compressed = new byte[ZLibEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            using var encoder = new ZLibEncoder(CompressionLevel.Optimal);
            encoder.Compress(s_sampleData, compressed, out _, out int compressedSize, isFinalBlock: true);

            byte[] decompressed = new byte[s_sampleData.Length];

            bool success = ZLibDecoder.TryDecompress(compressed.AsSpan(0, compressedSize), decompressed, out int bytesWritten);

            Assert.True(success);
            Assert.Equal(s_sampleData.Length, bytesWritten);
            Assert.Equal(s_sampleData, decompressed);
        }

        #endregion

        #region RoundTrip Tests

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void ZLibEncoder_Decoder_RoundTrip(int dataSize)
        {
            byte[] original = new byte[dataSize];
            new Random(42).NextBytes(original);

            byte[] compressed = new byte[ZLibEncoder.GetMaxCompressedLength(dataSize)];
            using var encoder = new ZLibEncoder(CompressionLevel.Optimal);
            OperationStatus compressStatus = encoder.Compress(original, compressed, out _, out int compressedSize, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, compressStatus);

            byte[] decompressed = new byte[dataSize];
            using var decoder = new ZLibDecoder();
            OperationStatus decompressStatus = decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out _, out int decompressedSize);

            Assert.Equal(OperationStatus.Done, decompressStatus);
            Assert.Equal(dataSize, decompressedSize);
            Assert.Equal(original, decompressed);
        }

        #endregion
    }

    public class GZipEncoderDecoderTests
    {
        private static readonly byte[] s_sampleData = Encoding.UTF8.GetBytes(
            "Hello, World! This is a test string for compression. " +
            "We need some repeated content to make compression effective. " +
            "Hello, World! This is a test string for compression. " +
            "The quick brown fox jumps over the lazy dog. " +
            "Sphinx of black quartz, judge my vow.");

        #region GZipEncoder Tests

        [Fact]
        public void GZipEncoder_Ctor_InvalidCompressionLevel_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GZipEncoder((CompressionLevel)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GZipEncoder((CompressionLevel)99));
        }

        [Fact]
        public void GZipEncoder_Ctor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GZipEncoder(null!));
        }

        [Fact]
        public void GZipEncoder_Compress_Success()
        {
            using var encoder = new GZipEncoder(CompressionLevel.Optimal);
            byte[] destination = new byte[GZipEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);
            Assert.True(bytesWritten < s_sampleData.Length);
        }

        [Fact]
        public void GZipEncoder_TryCompress_Success()
        {
            byte[] destination = new byte[GZipEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            bool success = GZipEncoder.TryCompress(s_sampleData, destination, out int bytesWritten);

            Assert.True(success);
            Assert.True(bytesWritten > 0);
        }

        [Theory]
        [InlineData(CompressionLevel.Optimal)]
        [InlineData(CompressionLevel.NoCompression)]
        [InlineData(CompressionLevel.Fastest)]
        [InlineData(CompressionLevel.SmallestSize)]
        public void GZipEncoder_CompressionLevels(CompressionLevel level)
        {
            using var encoder = new GZipEncoder(level);
            byte[] destination = new byte[GZipEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void GZipEncoder_WithOptions()
        {
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 9,
                CompressionStrategy = ZLibCompressionStrategy.Filtered
            };

            using var encoder = new GZipEncoder(options);
            byte[] destination = new byte[GZipEncoder.GetMaxCompressedLength(s_sampleData.Length)];

            OperationStatus status = encoder.Compress(s_sampleData, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, consumed);
            Assert.True(written > 0);
        }

        #endregion

        #region GZipDecoder Tests

        [Fact]
        public void GZipDecoder_Decompress_Success()
        {
            byte[] compressed = new byte[GZipEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            using var encoder = new GZipEncoder(CompressionLevel.Optimal);
            encoder.Compress(s_sampleData, compressed, out _, out int compressedSize, isFinalBlock: true);

            using var decoder = new GZipDecoder();
            byte[] decompressed = new byte[s_sampleData.Length];

            OperationStatus status = decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out int consumed, out int written);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(compressedSize, consumed);
            Assert.Equal(s_sampleData.Length, written);
            Assert.Equal(s_sampleData, decompressed);
        }

        [Fact]
        public void GZipDecoder_TryDecompress_Success()
        {
            byte[] compressed = new byte[GZipEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            using var encoder = new GZipEncoder(CompressionLevel.Optimal);
            encoder.Compress(s_sampleData, compressed, out _, out int compressedSize, isFinalBlock: true);

            byte[] decompressed = new byte[s_sampleData.Length];

            bool success = GZipDecoder.TryDecompress(compressed.AsSpan(0, compressedSize), decompressed, out int bytesWritten);

            Assert.True(success);
            Assert.Equal(s_sampleData.Length, bytesWritten);
            Assert.Equal(s_sampleData, decompressed);
        }

        #endregion

        #region RoundTrip Tests

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void GZipEncoder_Decoder_RoundTrip(int dataSize)
        {
            byte[] original = new byte[dataSize];
            new Random(42).NextBytes(original);

            byte[] compressed = new byte[GZipEncoder.GetMaxCompressedLength(dataSize)];
            using var encoder = new GZipEncoder(CompressionLevel.Optimal);
            OperationStatus compressStatus = encoder.Compress(original, compressed, out _, out int compressedSize, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, compressStatus);

            byte[] decompressed = new byte[dataSize];
            using var decoder = new GZipDecoder();
            OperationStatus decompressStatus = decoder.Decompress(compressed.AsSpan(0, compressedSize), decompressed, out _, out int decompressedSize);

            Assert.Equal(OperationStatus.Done, decompressStatus);
            Assert.Equal(dataSize, decompressedSize);
            Assert.Equal(original, decompressed);
        }

        #endregion

        #region Cross-Format Compatibility Tests

        [Fact]
        public void GZipEncoder_GZipStream_Interop()
        {
            byte[] compressed = new byte[GZipEncoder.GetMaxCompressedLength(s_sampleData.Length)];
            using var encoder = new GZipEncoder(CompressionLevel.Optimal);
            encoder.Compress(s_sampleData, compressed, out _, out int compressedSize, isFinalBlock: true);

            using var ms = new MemoryStream(compressed, 0, compressedSize);
            using var gzipStream = new GZipStream(ms, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            gzipStream.CopyTo(resultStream);

            Assert.Equal(s_sampleData, resultStream.ToArray());
        }

        [Fact]
        public void GZipStream_GZipDecoder_Interop()
        {
            using var ms = new MemoryStream();
            using (var gzipStream = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzipStream.Write(s_sampleData);
            }

            byte[] compressed = ms.ToArray();
            byte[] decompressed = new byte[s_sampleData.Length];

            using var decoder = new GZipDecoder();
            OperationStatus status = decoder.Decompress(compressed, decompressed, out _, out int written);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(s_sampleData.Length, written);
            Assert.Equal(s_sampleData, decompressed);
        }

        #endregion
    }
}
