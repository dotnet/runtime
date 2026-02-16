// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public class ZLibEncoderDecoderTests : EncoderDecoderTestBase
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

        public class ZLibEncoderAdapter : EncoderAdapter
        {
            private readonly ZLibEncoder _encoder;

            public ZLibEncoderAdapter(ZLibEncoder encoder)
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

        public class ZLibDecoderAdapter : DecoderAdapter
        {
            private readonly ZLibDecoder _decoder;

            public ZLibDecoderAdapter(ZLibDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        protected override EncoderAdapter CreateEncoder() =>
            new ZLibEncoderAdapter(new ZLibEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowLog) =>
            new ZLibEncoderAdapter(new ZLibEncoder(quality, windowLog));

        protected override EncoderAdapter CreateEncoder(DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override DecoderAdapter CreateDecoder() =>
            new ZLibDecoderAdapter(new ZLibDecoder());

        protected override DecoderAdapter CreateDecoder(DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override DictionaryAdapter CreateDictionary(ReadOnlySpan<byte> dictionaryData, int quality) =>
            throw new NotSupportedException();

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            ZLibEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) =>
            ZLibEncoder.TryCompress(source, destination, out bytesWritten, quality, windowLog);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            ZLibDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override long GetMaxCompressedLength(long inputSize) =>
            ZLibEncoder.GetMaxCompressedLength(inputSize);

        #region ZLib-specific Tests

        [Fact]
        public void ZLibEncoder_Ctor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ZLibEncoder(null!));
        }

        [Theory]
        [InlineData(ZLibCompressionStrategy.Default)]
        [InlineData(ZLibCompressionStrategy.Filtered)]
        [InlineData(ZLibCompressionStrategy.HuffmanOnly)]
        [InlineData(ZLibCompressionStrategy.RunLengthEncoding)]
        [InlineData(ZLibCompressionStrategy.Fixed)]
        public void ZLibEncoder_CompressionStrategies(ZLibCompressionStrategy strategy)
        {
            byte[] input = CreateTestData();
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 6,
                CompressionStrategy = strategy
            };
            using var encoder = new ZLibEncoder(options);
            byte[] destination = new byte[GetMaxCompressedLength(input.Length)];

            OperationStatus status = encoder.Compress(input, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void ZLibEncoder_WithOptions()
        {
            byte[] input = CreateTestData();
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 9,
                CompressionStrategy = ZLibCompressionStrategy.Filtered
            };

            using var encoder = new ZLibEncoder(options);
            byte[] destination = new byte[GetMaxCompressedLength(input.Length)];

            OperationStatus status = encoder.Compress(input, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
        }

        #endregion
    }
}
