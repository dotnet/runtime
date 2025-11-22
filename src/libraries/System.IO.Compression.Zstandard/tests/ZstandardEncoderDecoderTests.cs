// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardEncoderDecoderTests : EncoderDecoderTestBase
    {
        protected override bool SupportsDictionaries => true;
        protected override bool SupportsReset => true;

        protected override int ValidQuality => 3;
        protected override int ValidWindow => 10;

        protected override int InvalidQualityTooLow => -(1 << 17) - 1;
        protected override int InvalidQualityTooHigh => 23;
        protected override int InvalidWindowTooLow => 9;
        protected override int InvalidWindowTooHigh => 32;

        public class ZstandardEncoderAdapter : EncoderAdapter
        {
            private readonly ZstandardEncoder _encoder;

            public ZstandardEncoderAdapter(ZstandardEncoder encoder)
            {
                _encoder = encoder;
            }

            public override OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) =>
                _encoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

            public override OperationStatus Flush(Span<byte> destination, out int bytesWritten) =>
                _encoder.Flush(destination, out bytesWritten);

            public override void Dispose() => _encoder.Dispose();
            public override void Reset() => _encoder.Reset();
        }

        public class ZstandardDecoderAdapter : DecoderAdapter
        {
            private readonly ZstandardDecoder _decoder;

            public ZstandardDecoderAdapter(ZstandardDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => _decoder.Reset();
        }

        public class ZstandardDictionaryAdapter : DictionaryAdapter
        {
            public readonly ZstandardDictionary Dictionary;

            public ZstandardDictionaryAdapter(ZstandardDictionary dictionary)
            {
                Dictionary = dictionary;
            }

            public override void Dispose() => Dictionary.Dispose();
        }

        private static ZstandardDictionary? FromAdapter(DictionaryAdapter? adapter) =>
            (adapter as ZstandardDictionaryAdapter)?.Dictionary;

        protected override EncoderAdapter CreateEncoder() =>
            new ZstandardEncoderAdapter(new ZstandardEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowBits) =>
            new ZstandardEncoderAdapter(new ZstandardEncoder(quality, windowBits));

        protected override EncoderAdapter CreateEncoder(DictionaryAdapter dictionary, int windowBits) =>
            new ZstandardEncoderAdapter(new ZstandardEncoder(FromAdapter(dictionary), windowBits));

        protected override DecoderAdapter CreateDecoder() =>
            new ZstandardDecoderAdapter(new ZstandardDecoder());

        protected override DecoderAdapter CreateDecoder(DictionaryAdapter dictionary) =>
            new ZstandardDecoderAdapter(new ZstandardDecoder(FromAdapter(dictionary)));

        protected override DictionaryAdapter CreateDictionary(ReadOnlySpan<byte> dictionaryData, int quality) =>
            new ZstandardDictionaryAdapter(ZstandardDictionary.Create(dictionaryData, quality));

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            ZstandardEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary, int windowBits) =>
            ZstandardEncoder.TryCompress(source, destination, out bytesWritten, FromAdapter(dictionary), windowBits);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowBits) =>
            ZstandardEncoder.TryCompress(source, destination, out bytesWritten, quality, windowBits);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, DictionaryAdapter dictionary, out int bytesWritten) =>
            ZstandardDecoder.TryDecompress(source, FromAdapter(dictionary), destination, out bytesWritten);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            ZstandardDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override long GetMaxCompressedLength(long inputSize) =>
            ZstandardEncoder.GetMaxCompressedLength(inputSize);


        [Fact]
        public void GetMaxDecompressedLength_WithEmptyData_ReturnsZero()
        {
            ReadOnlySpan<byte> emptyData = ReadOnlySpan<byte>.Empty;

            bool result = ZstandardDecoder.TryGetMaxDecompressedLength(emptyData, out long maxLength);
            Assert.True(result);
            Assert.Equal(0, maxLength);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(long.MinValue)]
        public void SetSourceSize_WithNegativeSize_ThrowsArgumentOutOfRangeException(long size)
        {
            using ZstandardEncoder encoder = new();

            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.SetSourceSize(size));
        }

        [Fact]
        public void SetSourceSize_AfterDispose_ThrowsObjectDisposedException()
        {
            ZstandardEncoder encoder = new();
            encoder.Dispose();

            Assert.Throws<ObjectDisposedException>(() => encoder.SetSourceSize(100));
        }

        [Fact]
        public void SetSourceSize_BeforeCompression_AllowsCorrectSizeCompression()
        {
            using ZstandardEncoder encoder = new();
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // Set the correct source size
            encoder.SetSourceSize(input.Length);

            OperationStatus result = encoder.Compress(input, output, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void SetSourceSize_SizeDiffers_ReturnsInvalidData(long delta)
        {
            using ZstandardEncoder encoder = new();
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // Set incorrect source size
            encoder.SetSourceSize(input.Length + delta);

            // Don't specify isFinalBlock, as otherwise automatic size detection would kick in
            // and overwrite our pledged size with actual size
            OperationStatus result = encoder.Compress(input.AsSpan(0, 10), output, out int consumed, out int written, isFinalBlock: false);
            Assert.Equal(OperationStatus.Done, result);

            // push the rest of the data, which does not match the pledged size
            result = encoder.Compress(input.AsSpan(10), output, out _, out written, isFinalBlock: true);

            // The behavior depends on implementation - it may succeed or fail
            // This test just verifies that SetSourceSize doesn't crash and produces some result
            Assert.Equal(OperationStatus.InvalidData, result);
        }

        [Fact]
        public void SetSourceSize_AfterReset_ClearsSize()
        {
            using ZstandardEncoder encoder = new();
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // Set source size
            encoder.SetSourceSize(input.Length / 2);

            // Reset should clear the size
            encoder.Reset();

            // Now compression should work without size validation
            OperationStatus result = encoder.Compress(input, output, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);
        }

        [Fact]
        public void SetSourceSize_InvalidState_Throws()
        {
            using ZstandardEncoder encoder = new();
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // First, do a compression
            OperationStatus status = encoder.Compress(input.AsSpan(0, input.Length / 2), Span<byte>.Empty, out _, out _, isFinalBlock: true);

            Assert.NotEqual(OperationStatus.Done, status);
            Assert.Throws<InvalidOperationException>(() => encoder.SetSourceSize(input.Length));

            status = encoder.Flush(output, out _);
            Assert.Equal(OperationStatus.Done, status);

            // should throw also after everything is compressed
            Assert.Throws<InvalidOperationException>(() => encoder.SetSourceSize(input.Length));
        }

        [Fact]
        public void SetPrefix_ValidPrefix_SetsSuccessfully()
        {
            using ZstandardEncoder encoder = new();
            byte[] prefix = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            // Should not throw
            encoder.SetPrefix(prefix);

            // Verify encoder can still be used
            var input = ZstandardTestUtils.CreateTestData(100);
            byte[] output = new byte[1000];
            var result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            Assert.True(result == OperationStatus.Done || result == OperationStatus.DestinationTooSmall);
        }

        [Fact]
        public void SetPrefix_EmptyPrefix_SetsSuccessfully()
        {
            using ZstandardEncoder encoder = new();

            // Should not throw with empty prefix
            encoder.SetPrefix(ReadOnlyMemory<byte>.Empty);

            // Verify encoder can still be used
            var input = ZstandardTestUtils.CreateTestData(50);
            byte[] output = new byte[500];
            var result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            Assert.True(result == OperationStatus.Done || result == OperationStatus.DestinationTooSmall);
        }

        [Fact]
        public void SetPrefix_AfterDispose_ThrowsObjectDisposedException()
        {
            ZstandardEncoder encoder = new();
            encoder.Dispose();

            byte[] prefix = { 0x01, 0x02, 0x03 };
            Assert.Throws<ObjectDisposedException>(() => encoder.SetPrefix(prefix));
        }

        [Fact]
        public void SetPrefix_AfterFinished_ThrowsInvalidOperationException()
        {
            using ZstandardEncoder encoder = new();
            var input = ZstandardTestUtils.CreateTestData(100);
            byte[] output = new byte[1000];

            // Compress and finish
            encoder.Compress(input, output, out _, out _, isFinalBlock: true);
            encoder.Flush(output, out _);

            byte[] prefix = { 0x01, 0x02, 0x03 };
            Assert.Throws<InvalidOperationException>(() => encoder.SetPrefix(prefix));
        }

        [Fact]
        public void SetPrefix_AfterStartedCompression_ThrowsInvalidOperationException()
        {
            using ZstandardEncoder encoder = new();
            var input = ZstandardTestUtils.CreateTestData(100);
            byte[] output = new byte[1000];

            // Start compression
            encoder.Compress(input, output, out _, out _, isFinalBlock: false);

            byte[] prefix = { 0x01, 0x02, 0x03 };
            Assert.Throws<InvalidOperationException>(() => encoder.SetPrefix(prefix));
        }

        [Fact]
        public void SetPrefix_AfterReset_SetsSuccessfully()
        {
            using ZstandardEncoder encoder = new();
            var input = ZstandardTestUtils.CreateTestData(100);
            byte[] output = new byte[1000];

            // First compression cycle
            byte[] firstPrefix = { 0x01, 0x02, 0x03 };
            encoder.SetPrefix(firstPrefix);
            encoder.Compress(input, output, out _, out _, isFinalBlock: true);
            encoder.Flush(output, out _);

            // Reset and set new prefix
            encoder.Reset();
            byte[] secondPrefix = { 0x04, 0x05, 0x06 };
            encoder.SetPrefix(secondPrefix); // Should not throw

            // Verify encoder can still be used
            var result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            Assert.True(result == OperationStatus.Done || result == OperationStatus.DestinationTooSmall);
        }

        [Fact]
        public void SetPrefix_MultipleTimes_BeforeCompression_LastOneWins()
        {
            using ZstandardEncoder encoder = new();

            byte[] firstPrefix = { 0x01, 0x02, 0x03 };
            encoder.SetPrefix(firstPrefix);

            byte[] secondPrefix = { 0x04, 0x05, 0x06 };
            encoder.SetPrefix(secondPrefix); // Should not throw - last one wins

            // Verify encoder can still be used
            var input = ZstandardTestUtils.CreateTestData(100);
            byte[] output = new byte[1000];
            var result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            Assert.True(result == OperationStatus.Done || result == OperationStatus.DestinationTooSmall);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Compress_AppendChecksum_RoundTrip(bool corrupt)
        {
            using ZstandardEncoder encoder = new(new ZstandardCompressionOptions
            {
                AppendChecksum = true
            });
            byte[] input = ZstandardTestUtils.CreateTestData(100);
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            OperationStatus result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, result);

            if (corrupt)
            {
                // Corrupt the data
                output[10] ^= 0xFF;
            }

            // Try to decompress
            using ZstandardDecoder decoder = new();
            byte[] decompressed = new byte[input.Length];
            var decompressResult = decoder.Decompress(output.AsSpan(0, bytesWritten), decompressed, out int bytesConsumedDec, out int bytesWrittenDec);

            if (corrupt)
            {
                Assert.Equal(OperationStatus.InvalidData, decompressResult);
            }
            else
            {
                Assert.Equal(OperationStatus.Done, decompressResult);
                Assert.Equal(input.Length, bytesWrittenDec);
                Assert.Equal(input, decompressed);
            }
        }
    }
}