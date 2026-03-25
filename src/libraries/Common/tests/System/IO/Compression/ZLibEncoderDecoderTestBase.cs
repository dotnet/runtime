// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public abstract class ZLibEncoderDecoderTestBase : EncoderDecoderTestBase
    {
        protected override bool SupportsDictionaries => false;
        protected override bool SupportsReset => false;

        protected override string WindowLogParamName => "windowLog";
        protected override string InputLengthParamName => "inputLength";

        // Quality maps to zlib compression level (0-9)
        protected override int ValidQuality => 6;
        protected override int ValidWindowLog => 15;
        protected override int MinQuality => 0;
        protected override int MaxQuality => 9;
        protected override int MinWindowLog => 8;
        protected override int MaxWindowLog => 15;

        protected override int InvalidQualityTooLow => -2;
        protected override int InvalidQualityTooHigh => 10;
        protected override int InvalidWindowLogTooLow => 7;
        protected override int InvalidWindowLogTooHigh => 16;

        protected abstract EncoderAdapter CreateEncoder(ZLibCompressionOptions options);

        protected override EncoderAdapter CreateEncoder(DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override DecoderAdapter CreateDecoder(DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override DictionaryAdapter CreateDictionary(ReadOnlySpan<byte> dictionaryData, int quality) =>
            throw new NotSupportedException();

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        [Fact]
        public void Encoder_Ctor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateEncoder(null!));
        }

        [Theory]
        [InlineData(ZLibCompressionStrategy.Default)]
        [InlineData(ZLibCompressionStrategy.Filtered)]
        [InlineData(ZLibCompressionStrategy.HuffmanOnly)]
        [InlineData(ZLibCompressionStrategy.RunLengthEncoding)]
        [InlineData(ZLibCompressionStrategy.Fixed)]
        public void Encoder_CompressionStrategies(ZLibCompressionStrategy strategy)
        {
            byte[] input = CreateTestData();
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 6,
                CompressionStrategy = strategy
            };
            using var encoder = CreateEncoder(options);
            byte[] destination = new byte[GetMaxCompressedLength(input.Length)];

            OperationStatus status = encoder.Compress(input, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void Decoder_InvalidData_ReturnsInvalidData()
        {
            byte[] invalidData = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

            using var decoder = CreateDecoder();
            byte[] decompressed = new byte[100];

            OperationStatus status = decoder.Decompress(invalidData, decompressed, out _, out _);

            Assert.Equal(OperationStatus.InvalidData, status);
        }
    }
}
