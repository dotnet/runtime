// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public class GZipEncoderDecoderTests : EncoderDecoderTestBase
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

        public class GZipEncoderAdapter : EncoderAdapter
        {
            private readonly GZipEncoder _encoder;

            public GZipEncoderAdapter(GZipEncoder encoder)
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

        public class GZipDecoderAdapter : DecoderAdapter
        {
            private readonly GZipDecoder _decoder;

            public GZipDecoderAdapter(GZipDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        protected override EncoderAdapter CreateEncoder() =>
            new GZipEncoderAdapter(new GZipEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowLog) =>
            new GZipEncoderAdapter(new GZipEncoder(quality, windowLog));

        protected override EncoderAdapter CreateEncoder(DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override DecoderAdapter CreateDecoder() =>
            new GZipDecoderAdapter(new GZipDecoder());

        protected override DecoderAdapter CreateDecoder(DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override DictionaryAdapter CreateDictionary(ReadOnlySpan<byte> dictionaryData, int quality) =>
            throw new NotSupportedException();

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            GZipEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) =>
            GZipEncoder.TryCompress(source, destination, out bytesWritten, quality, windowLog);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            GZipDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override long GetMaxCompressedLength(long inputSize) =>
            GZipEncoder.GetMaxCompressedLength(inputSize);

        #region GZip-specific Tests

        [Fact]
        public void GZipEncoder_Ctor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GZipEncoder(null!));
        }

        [Theory]
        [InlineData(ZLibCompressionStrategy.Default)]
        [InlineData(ZLibCompressionStrategy.Filtered)]
        [InlineData(ZLibCompressionStrategy.HuffmanOnly)]
        [InlineData(ZLibCompressionStrategy.RunLengthEncoding)]
        [InlineData(ZLibCompressionStrategy.Fixed)]
        public void GZipEncoder_CompressionStrategies(ZLibCompressionStrategy strategy)
        {
            byte[] input = CreateTestData();
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 6,
                CompressionStrategy = strategy
            };
            using var encoder = new GZipEncoder(options);
            byte[] destination = new byte[GetMaxCompressedLength(input.Length)];

            OperationStatus status = encoder.Compress(input, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void GZipEncoder_WithOptions()
        {
            byte[] input = CreateTestData();
            var options = new ZLibCompressionOptions
            {
                CompressionLevel = 9,
                CompressionStrategy = ZLibCompressionStrategy.Filtered
            };

            using var encoder = new GZipEncoder(options);
            byte[] destination = new byte[GetMaxCompressedLength(input.Length)];

            OperationStatus status = encoder.Compress(input, destination, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void GZipEncoder_GZipStream_Interop()
        {
            byte[] input = CreateTestData();
            byte[] compressed = new byte[GetMaxCompressedLength(input.Length)];
            using var encoder = new GZipEncoder(6);
            encoder.Compress(input, compressed, out _, out int compressedSize, isFinalBlock: true);

            using var ms = new MemoryStream(compressed, 0, compressedSize);
            using var gzipStream = new GZipStream(ms, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            gzipStream.CopyTo(resultStream);

            Assert.Equal(input, resultStream.ToArray());
        }

        [Fact]
        public void GZipStream_GZipDecoder_Interop()
        {
            byte[] input = CreateTestData();
            using var ms = new MemoryStream();
            using (var gzipStream = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzipStream.Write(input);
            }

            byte[] compressed = ms.ToArray();
            byte[] decompressed = new byte[input.Length];

            using var decoder = new GZipDecoder();
            OperationStatus status = decoder.Decompress(compressed, decompressed, out _, out int written);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(input.Length, written);
            Assert.Equal(input, decompressed);
        }

        #endregion
    }
}
