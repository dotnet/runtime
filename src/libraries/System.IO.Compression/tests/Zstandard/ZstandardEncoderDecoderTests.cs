// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardEncoderDecoderTests : EncoderDecoderTestBase
    {
        protected override bool SupportsDictionaries => true;
        protected override bool SupportsReset => true;

        protected override int ValidQuality => 3;
        protected override int ValidWindowLog => 10;

        protected override int InvalidQualityTooLow => -(1 << 17) - 1;
        protected override int InvalidQualityTooHigh => 23;
        protected override int InvalidWindowLogTooLow => 9;
        protected override int InvalidWindowLogTooHigh => 32;

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

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary) =>
            ZstandardDecoder.TryDecompress(source, destination, out bytesWritten, FromAdapter(dictionary));

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            ZstandardDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override long GetMaxCompressedLength(long inputLength) =>
            ZstandardEncoder.GetMaxCompressedLength(inputLength);

        [Fact]
        public void Decoder_Ctor_MaxWindowLog_InvalidValues()
        {
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(new byte[] { 0x01, 0x02, 0x03, 0x04 }, quality: 3);

            Assert.Throws<ArgumentOutOfRangeException>("maxWindowLog", () => new ZstandardDecoder(maxWindowLog: 8));
            Assert.Throws<ArgumentOutOfRangeException>("maxWindowLog", () => new ZstandardDecoder(maxWindowLog: 33));
            Assert.Throws<ArgumentOutOfRangeException>("maxWindowLog", () => new ZstandardDecoder(dictionary, maxWindowLog: 8));
            Assert.Throws<ArgumentOutOfRangeException>("maxWindowLog", () => new ZstandardDecoder(dictionary, maxWindowLog: 33));
        }

        [Fact]
        public void GetMaxCompressedLength_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // unfortunately, the max argument is platform dependent due to internal use of size_t
            // on the native library side. So we test up to the smaller of the two limits.

            long maxValue = (long)Math.Min((ulong)long.MaxValue, (ulong)nuint.MaxValue);
            // since the returned value is slightly larger than the input, we
            // expect the value to overflow the signed long range
            Assert.Throws<ArgumentOutOfRangeException>("inputLength", () => GetMaxCompressedLength(maxValue));

            // on 64-bit platforms, this overflows and tests negative inputs
            // on 32-bit platforms, this is in range for long, but out of range for size_t used natively
            Assert.Throws<ArgumentOutOfRangeException>("inputLength", () => GetMaxCompressedLength(maxValue + 1L));

            // test also the negative explicitly
            Assert.Throws<ArgumentOutOfRangeException>("inputLength", () => GetMaxCompressedLength(-1));
        }

        [Fact]
        public void TryGetMaxDecompressedLength_WithEmptyData_ReturnsZero()
        {
            ReadOnlySpan<byte> emptyData = ReadOnlySpan<byte>.Empty;

            bool result = ZstandardDecoder.TryGetMaxDecompressedLength(emptyData, out long maxLength);
            Assert.True(result);
            Assert.Equal(0, maxLength);
        }

        private Memory<byte> CompressHelper(ReadOnlySpan<byte> input, bool knownSizeInHeader = true)
        {
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];
            int compressedSize;
            if (!knownSizeInHeader)
            {
                // perform in two steps so that the encoder does not know the size upfront
                using ZstandardEncoder encoder = new(); // 2 GB window
                OperationStatus result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
                Assert.Equal(OperationStatus.Done, result);

                compressedSize = bytesWritten;
                result = encoder.Compress(Span<byte>.Empty, output.AsSpan(bytesWritten), out _, out bytesWritten, isFinalBlock: true);
                Assert.Equal(OperationStatus.Done, result);
                compressedSize += bytesWritten;
            }
            else
            {
                bool compressResult = ZstandardEncoder.TryCompress(input, output, out compressedSize);
                Assert.True(compressResult);
            }

            return output.AsMemory(0, compressedSize);
        }

        [Fact]
        public void TryGetMaxDecompressedLength_ExactSizeUnknown_ReturnsEstimate()
        {
            Memory<byte> compressed = CompressHelper(ZstandardTestUtils.CreateTestData(1000), knownSizeInHeader: false);

            Assert.True(ZstandardDecoder.TryGetMaxDecompressedLength(compressed.Span, out long maxLength));
            Assert.True(maxLength >= 1000);
        }

        private void CheckKnownDecompressedSize(ReadOnlySpan<byte> compressedData, long expectedDecompressedLength)
        {
            bool result = ZstandardDecoder.TryGetMaxDecompressedLength(compressedData, out long maxLength);
            Assert.True(result);
            Assert.Equal(expectedDecompressedLength, maxLength);
        }

        [Fact]
        public void TryGetMaxDecompressedLength_SizeKnown_GivesExactLength()
        {
            Memory<byte> compressed = CompressHelper(ZstandardTestUtils.CreateTestData(5000), knownSizeInHeader: true);

            CheckKnownDecompressedSize(compressed.Span, 5000);
        }

        [Fact]
        public void TryGetMaxDecompressedLength_IncompleteFrame_ReturnsFalse()
        {
            Memory<byte> compressed = CompressHelper(ZstandardTestUtils.CreateTestData(5000), knownSizeInHeader: true);

            Assert.False(ZstandardDecoder.TryGetMaxDecompressedLength(compressed.Span.Slice(0, 10), out long maxLength));
            Assert.Equal(0, maxLength);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(long.MinValue)]
        public void SetSourceLength_WithNegativeLength_ThrowsArgumentOutOfRangeException(long length)
        {
            using ZstandardEncoder encoder = new();

            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.SetSourceLength(length));
        }

        [Fact]
        public void SetSourceLength_AfterDispose_ThrowsObjectDisposedException()
        {
            ZstandardEncoder encoder = new();
            encoder.Dispose();

            Assert.Throws<ObjectDisposedException>(() => encoder.SetSourceLength(100));
        }

        [Fact]
        public void SetSourceLength_BeforeCompression_AllowsCorrectLengthCompression()
        {
            using ZstandardEncoder encoder = new();
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // Set the correct source length
            encoder.SetSourceLength(input.Length);

            OperationStatus result = encoder.Compress(input, output, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);

            CheckKnownDecompressedSize(output.AsSpan(0, written), input.Length);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void SetSourceLength_LengthDiffers_ReturnsInvalidData(long delta)
        {
            using ZstandardEncoder encoder = new();
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // Set incorrect source length
            encoder.SetSourceLength(input.Length + delta);

            // Don't specify isFinalBlock, as otherwise automatic length detection would kick in
            // and overwrite our pledged length with actual length
            OperationStatus result = encoder.Compress(input.AsSpan(0, 10), output, out int consumed, out int written, isFinalBlock: false);
            Assert.Equal(OperationStatus.Done, result);

            // push the rest of the data, which does not match the pledged length
            result = encoder.Compress(input.AsSpan(10), output, out _, out written, isFinalBlock: true);

            // The behavior depends on implementation - it may succeed or fail
            // This test just verifies that SetSourceLength doesn't crash and produces some result
            Assert.Equal(OperationStatus.InvalidData, result);
        }

        [Fact]
        public void SetSourceLength_AfterReset_ClearsLength()
        {
            using ZstandardEncoder encoder = new();
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // Set source length
            encoder.SetSourceLength(input.Length / 2);

            // Reset should clear the length
            encoder.Reset();

            // Now compression should work without length validation
            OperationStatus result = encoder.Compress(input, output, out int consumed, out int written, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(input.Length, consumed);
            Assert.True(written > 0);

            CheckKnownDecompressedSize(output.AsSpan(0, written), input.Length);
        }

        [Fact]
        public void SetSourceLength_InvalidState_Throws()
        {
            using ZstandardEncoder encoder = new();
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // First, do a compression
            OperationStatus status = encoder.Compress(input.AsSpan(0, input.Length / 2), Span<byte>.Empty, out _, out _, isFinalBlock: false);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Throws<InvalidOperationException>(() => encoder.SetSourceLength(input.Length));

            status = encoder.Compress(Span<byte>.Empty, output, out _, out int compressedBytes, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);

            // should throw also after everything is compressed
            Assert.Throws<InvalidOperationException>(() => encoder.SetSourceLength(input.Length));
        }

        private void TestRoundTrip(ReadOnlyMemory<byte> data, ZstandardEncoder encoder, ZstandardDecoder decoder, bool shouldFail = false)
        {
            byte[] compressed = new byte[ZstandardEncoder.GetMaxCompressedLength(data.Length)];
            OperationStatus compressStatus = encoder.Compress(data.Span, compressed, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, compressStatus);
            Assert.Equal(data.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);

            byte[] decompressed = new byte[data.Length];
            OperationStatus decompressStatus = decoder.Decompress(compressed.AsSpan(0, bytesWritten), decompressed, out bytesConsumed, out bytesWritten);

            if (shouldFail)
            {
                Assert.Equal(OperationStatus.InvalidData, decompressStatus);
            }
            else
            {
                Assert.Equal(OperationStatus.Done, decompressStatus);
                Assert.Equal(data.Length, bytesWritten);
                Assert.Equal(data.Span, decompressed);
            }
        }

        [Fact]
        public void SetPrefix_ValidPrefix_Roundtrips()
        {
            byte[] originalData = "Hello, World! This is a test string for compression and decompression."u8.ToArray();
            byte[] prefixData = "Hello, World! "u8.ToArray();

            using ZstandardEncoder encoder = new();
            encoder.SetPrefix(prefixData);

            using ZstandardDecoder decoder = new();
            decoder.SetPrefix(prefixData);

            TestRoundTrip(originalData, encoder, decoder);
        }

        [Fact]
        public void SetPrefix_EmptyPrefix_SetsSuccessfully()
        {
            // Should not throw with empty prefix
            using ZstandardEncoder encoder = new();
            encoder.SetPrefix(ReadOnlyMemory<byte>.Empty);

            using ZstandardDecoder decoder = new();

            // empty prefix is the same as no prefix, data should roundtrip
            TestRoundTrip(ZstandardTestUtils.CreateTestData(100), encoder, decoder);
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

            using ZstandardDecoder decoder = new();
            decoder.SetPrefix(secondPrefix);

            TestRoundTrip(input, encoder, decoder);

            // Reset again
            decoder.Reset();
            encoder.Reset();

            decoder.SetPrefix(Memory<byte>.Empty);

            TestRoundTrip(input, encoder, decoder);
        }

        [Fact]
        public void SetPrefix_MultipleTimes_BeforeCompression_LastOneWins()
        {
            using ZstandardEncoder encoder = new();

            byte[] firstPrefix = { 0x01, 0x02, 0x03 };
            encoder.SetPrefix(firstPrefix);

            byte[] secondPrefix = { 0x04, 0x05, 0x06 };
            encoder.SetPrefix(secondPrefix); // Should not throw - last one wins

            using ZstandardDecoder decoder = new();
            decoder.SetPrefix(secondPrefix);

            TestRoundTrip(ZstandardTestUtils.CreateTestData(100), encoder, decoder);
        }

        [Fact]
        public void SetPrefix_SameAsDictionary()
        {
            byte[] dictionaryData = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A };
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData, quality: 3);

            using ZstandardEncoder encoder = new(dictionary);

            using ZstandardDecoder decoder = new();
            decoder.SetPrefix(dictionaryData);

            TestRoundTrip(ZstandardTestUtils.CreateTestData(100), encoder, decoder);
        }

        [Fact]
        public void SetPrefix_InvalidState_Throws()
        {
            using ZstandardEncoder encoder = new();
            byte[] prefixData = { 0x01, 0x02, 0x03 };
            byte[] input = ZstandardTestUtils.CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // First, do a compression
            OperationStatus status = encoder.Compress(input.AsSpan(0, input.Length / 2), Span<byte>.Empty, out _, out _, isFinalBlock: false);

            Assert.Equal(OperationStatus.Done, status);
            Assert.Throws<InvalidOperationException>(() => encoder.SetPrefix(prefixData));
            status = encoder.Compress(Span<byte>.Empty, output, out _, out int compressedBytes, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, status);

            // should throw also after everything is compressed
            Assert.Throws<InvalidOperationException>(() => encoder.SetPrefix(prefixData));

            Span<byte> compressedSpan = output.AsSpan(0, compressedBytes);
            using ZstandardDecoder decoder = new();

            // First, do a decompression
            status = decoder.Decompress(compressedSpan.Slice(0, 10), output, out int consumed, out _);
            Assert.Equal(OperationStatus.NeedMoreData, status);

            // should throw, since we started
            Assert.Throws<InvalidOperationException>(() => decoder.SetPrefix(prefixData));

            status = decoder.Decompress(compressedSpan.Slice(consumed), input, out _, out _);
            Assert.Equal(OperationStatus.Done, status);

            // should throw also after everything is decompressed
            Assert.Throws<InvalidOperationException>(() => decoder.SetPrefix(prefixData));
        }

        [Fact]
        public void Decompress_WindowTooLarge_ThrowsIOException()
        {
            byte[] input = ZstandardTestUtils.CreateTestData(1024 * 1024); // 1 MB
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // by default, decoder will not allow windows larger than 27 to protect against memory DOS attacks
            int largeWindowLog = 28; // 256 MB window

            using ZstandardEncoder encoder = new(quality: 3, windowLog: largeWindowLog);

            // perform in two steps so that the encoder does not know the size upfront
            OperationStatus result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);
            Assert.Equal(OperationStatus.Done, result);

            int compressedSize = bytesWritten;
            result = encoder.Compress(Span<byte>.Empty, output.AsSpan(bytesWritten), out _, out bytesWritten, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, result);
            compressedSize += bytesWritten;

            byte[] decompressed = new byte[input.Length + 1];

            using ZstandardDecoder decoder = new();

            var ex = Assert.Throws<IOException>(() => decoder.Decompress(output.AsSpan(0, compressedSize), decompressed, out _, out _));
            Assert.Contains("maxWindowLog", ex.Message);

            // now try with increased maxWindowLog
            using ZstandardDecoder adjustedDecoder = new(maxWindowLog: largeWindowLog);
            var decompressResult = adjustedDecoder.Decompress(output.AsSpan(0, compressedSize), decompressed, out bytesConsumed, out bytesWritten);
            Assert.Equal(OperationStatus.Done, decompressResult);
            Assert.Equal(input.Length, bytesWritten);
            Assert.Equal(input, decompressed.AsSpan(0, bytesWritten));
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