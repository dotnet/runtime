// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public class DeflateEncoderDecoderTests : EncoderDecoderTestBase
    {
        protected override bool SupportsDictionaries => false;
        protected override bool SupportsReset => false;

        protected override string WindowLogParamName => "windowLog";
        protected override string InputLengthParamName => "inputLength";

        // Quality maps to zlib compression level (0-9)
        protected override int ValidQuality => 6;
        protected override int ValidWindowLog => 15;

        protected override int InvalidQualityTooLow => -2;
        protected override int InvalidQualityTooHigh => 10;
        protected override int InvalidWindowLogTooLow => 7;
        protected override int InvalidWindowLogTooHigh => 16;

        public class DeflateEncoderAdapter : EncoderAdapter
        {
            private readonly DeflateEncoder _encoder;

            public DeflateEncoderAdapter(DeflateEncoder encoder)
            {
                _encoder = encoder;
            }

            public override OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) =>
                _encoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

            public override OperationStatus Flush(Span<byte> destination, out int bytesWritten) =>
                _encoder.Flush(destination, out bytesWritten);

            public override void Dispose() => _encoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        public class DeflateDecoderAdapter : DecoderAdapter
        {
            private readonly DeflateDecoder _decoder;

            public DeflateDecoderAdapter(DeflateDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        protected override EncoderAdapter CreateEncoder() =>
            new DeflateEncoderAdapter(new DeflateEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowLog) =>
            new DeflateEncoderAdapter(new DeflateEncoder(quality, windowLog));

        protected override EncoderAdapter CreateEncoder(DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override DecoderAdapter CreateDecoder() =>
            new DeflateDecoderAdapter(new DeflateDecoder());

        protected override DecoderAdapter CreateDecoder(DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override DictionaryAdapter CreateDictionary(ReadOnlySpan<byte> dictionaryData, int quality) =>
            throw new NotSupportedException();

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            DeflateEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) =>
            DeflateEncoder.TryCompress(source, destination, out bytesWritten, quality, windowLog);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            DeflateDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override long GetMaxCompressedLength(long inputSize) =>
            DeflateEncoder.GetMaxCompressedLength(inputSize);

        #region Deflate-specific Tests

        [Fact]
        public void DeflateEncoder_Ctor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DeflateEncoder(null!));
        }

        [Theory]
        [InlineData(ZLibCompressionStrategy.Default)]
        [InlineData(ZLibCompressionStrategy.Filtered)]
        [InlineData(ZLibCompressionStrategy.HuffmanOnly)]
        [InlineData(ZLibCompressionStrategy.RunLengthEncoding)]
        [InlineData(ZLibCompressionStrategy.Fixed)]
        public void DeflateEncoder_CompressionStrategies(ZLibCompressionStrategy strategy)
        {
            byte[] input = CreateTestData();
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 6,
                CompressionStrategy = strategy
            };
            using var encoder = new DeflateEncoder(options);
            byte[] destination = new byte[GetMaxCompressedLength(input.Length)];

            OperationStatus status = encoder.Compress(input, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void DeflateEncoder_WithOptions()
        {
            byte[] input = CreateTestData();
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 9,
                CompressionStrategy = ZLibCompressionStrategy.Filtered
            };

            using var encoder = new DeflateEncoder(options);
            byte[] destination = new byte[GetMaxCompressedLength(input.Length)];

            OperationStatus status = encoder.Compress(input, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
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
    }
}
