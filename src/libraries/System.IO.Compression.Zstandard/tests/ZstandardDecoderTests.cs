// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardDecoderTests
    {
        [Fact]
        public void Constructor_WithNullDictionary_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZstandardDecoder(null!));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            byte[] dictionaryData = CreateSampleDictionary();
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData);
            ZstandardDecoder decoder = new ZstandardDecoder(dictionary);

            decoder.Dispose();
            decoder.Dispose();
        }

        [Fact]
        public void Reset_AfterDispose_ThrowsObjectDisposedException()
        {
            var decoder = new ZstandardDecoder();
            decoder.Dispose();

            Assert.Throws<ObjectDisposedException>(() => decoder.Reset());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Reset_AllowsReuseForMultipleDecompressions(bool useDictionary)
        {
            byte[] dictionaryData = CreateSampleDictionary();
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(dictionaryData, ZstandardCompressionOptions.DefaultQuality);

            // First compress some data to have something to decompress
            byte[] input = CreateTestData();
            byte[] compressed = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];
            bool compressResult = useDictionary
                ? ZstandardEncoder.TryCompress(input, compressed, out int compressedLength, dictionary, ZstandardCompressionOptions.DefaultWindow)
                : ZstandardEncoder.TryCompress(input, compressed, out compressedLength);
            Assert.True(compressResult);

            // Resize compressed to actual length
            Array.Resize(ref compressed, compressedLength);

            using var decoder = useDictionary
                ? new ZstandardDecoder(dictionary)
                : new ZstandardDecoder();
            byte[] output1 = new byte[input.Length];
            byte[] output2 = new byte[input.Length];

            // First decompression
            OperationStatus result1 = decoder.Decompress(compressed, output1, out int consumed1, out int written1);
            Assert.Equal(OperationStatus.Done, result1);
            Assert.Equal(compressed.Length, consumed1);
            Assert.Equal(input.Length, written1);
            Assert.Equal(input, output1);

            // Reset and decompress again
            decoder.Reset();
            OperationStatus result2 = decoder.Decompress(compressed, output2, out int consumed2, out int written2);
            Assert.Equal(OperationStatus.Done, result2);
            Assert.Equal(compressed.Length, consumed2);
            Assert.Equal(input.Length, written2);
            Assert.Equal(input, output2);
        }

        [Fact]
        public void GetMaxDecompressedLength_WithEmptyData_ReturnsZero()
        {
            ReadOnlySpan<byte> emptyData = ReadOnlySpan<byte>.Empty;

            bool result = ZstandardDecoder.TryGetMaxDecompressedLength(emptyData, out long maxLength);
            Assert.True(result);
            Assert.Equal(0, maxLength);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TryDecompress_WithEmptySource_ReturnsFalse(bool useDictionary)
        {
            ReadOnlySpan<byte> emptySource = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            using ZstandardDictionary dictionary = ZstandardDictionary.Create(CreateSampleDictionary());

            bool result = useDictionary
                ? ZstandardDecoder.TryDecompress(emptySource, dictionary, destination, out int bytesWritten)
                : ZstandardDecoder.TryDecompress(emptySource, destination, out bytesWritten);

            Assert.False(result);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void TryDecompress_WithDictionary_NullDictionary_ThrowsArgumentNullException()
        {
            byte[] source = new byte[] { 1, 2, 3, 4 };
            byte[] destination = new byte[100];

            Assert.Throws<ArgumentNullException>(() =>
                ZstandardDecoder.TryDecompress(source, null!, destination, out _));
        }

        [Fact]
        public void Decompress_AfterDispose_ThrowsObjectDisposedException()
        {
            byte[] dictionaryData = CreateSampleDictionary();
            ZstandardDecoder decoder = new ZstandardDecoder();
            decoder.Dispose();

            byte[] source = new byte[] { 1, 2, 3, 4 };
            byte[] destination = new byte[100];

            Assert.Throws<ObjectDisposedException>(() =>
                decoder.Decompress(source, destination, out _, out _));
        }

        [Fact]
        public void Decompress_WithEmptySource_ReturnsNeedMoreData()
        {
            using ZstandardDecoder decoder = new ZstandardDecoder();

            ReadOnlySpan<byte> emptySource = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            OperationStatus result = decoder.Decompress(emptySource, destination, out int bytesConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.NeedMoreData, result);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);

            Assert.False(ZstandardDecoder.TryDecompress(emptySource, destination, out bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void Decompress_WithEmptyDestination_ReturnsDestinationTooSmall()
        {
            Span<byte> destination = Span<byte>.Empty;
            Span<byte> source = new byte[100];

            Assert.True(ZstandardEncoder.TryCompress("This is a test content"u8, source, out int bytesWritten));
            source = source.Slice(0, bytesWritten);

            using ZstandardDecoder decoder = new ZstandardDecoder();
            OperationStatus result = decoder.Decompress(source, destination, out int bytesConsumed, out bytesWritten);

            Assert.Equal(OperationStatus.DestinationTooSmall, result);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
        }

        public static IEnumerable<object[]> GetRoundTripTestData()
        {
            foreach (int quality in new[] { 1, 2, 3 })
            {
                foreach (bool useDictionary in new[] { true, false })
                {
                    foreach (bool staticEncode in new[] { true, false })
                    {
                        foreach (bool staticDecode in new[] { true, false })
                        {
                            yield return new object[] { quality, useDictionary, staticEncode, staticDecode };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetRoundTripTestData))]
        public void RoundTrip_SuccessfullyCompressesAndDecompresses(int quality, bool useDictionary, bool staticEncode, bool staticDecode)
        {
            byte[] originalData = "Hello, World! This is a test string for Zstandard compression and decompression."u8.ToArray();
            byte[] compressedBuffer = new byte[ZstandardEncoder.GetMaxCompressedLength(originalData.Length)];
            byte[] decompressedBuffer = new byte[originalData.Length * 2];

            using ZstandardDictionary dictionary = ZstandardDictionary.Create(CreateSampleDictionary(), quality);

            int window = 10;

            int bytesWritten;
            int bytesConsumed;

            // Compress
            if (staticEncode)
            {
                bool result =
                    useDictionary
                    ? ZstandardEncoder.TryCompress(originalData, compressedBuffer, out bytesWritten, dictionary, window)
                    : ZstandardEncoder.TryCompress(originalData, compressedBuffer, out bytesWritten, quality, window);
                bytesConsumed = originalData.Length;

                Assert.True(result);
            }
            else
            {
                using var encoder = useDictionary ? new ZstandardEncoder(dictionary, window) : new ZstandardEncoder(quality, window);
                OperationStatus compressResult = encoder.Compress(originalData, compressedBuffer, out bytesConsumed, out bytesWritten, true);
                Assert.Equal(OperationStatus.Done, compressResult);
            }

            Assert.Equal(originalData.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);
            int compressedLength = bytesWritten;

            // Decompress
            if (staticDecode)
            {
                bool result =
                    useDictionary
                    ? ZstandardDecoder.TryDecompress(compressedBuffer.AsSpan(0, compressedLength), dictionary, decompressedBuffer, out bytesWritten)
                    : ZstandardDecoder.TryDecompress(compressedBuffer.AsSpan(0, compressedLength), decompressedBuffer, out bytesWritten);
                bytesConsumed = compressedLength;

                Assert.True(result);
            }
            else
            {
                using var decoder = useDictionary ? new ZstandardDecoder(dictionary) : new ZstandardDecoder();
                OperationStatus decompressResult = decoder.Decompress(compressedBuffer.AsSpan(0, compressedLength), decompressedBuffer, out bytesConsumed, out bytesWritten);

                Assert.Equal(OperationStatus.Done, decompressResult);
            }

            Assert.Equal(compressedLength, bytesConsumed);
            Assert.Equal(originalData.Length, bytesWritten);
            Assert.Equal(originalData, decompressedBuffer.AsSpan(0, bytesWritten));
        }

        private static byte[] CreateSampleDictionary() => ZstandardTestUtils.CreateSampleDictionary();

        private static byte[] CreateTestData()
        {
            // Create some test data that compresses well
            byte[] data = new byte[1000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 10); // Repeating pattern
            }
            return data;
        }
    }
}
