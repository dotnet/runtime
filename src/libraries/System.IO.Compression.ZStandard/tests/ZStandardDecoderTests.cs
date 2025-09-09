// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace System.IO.Compression
{
    public class ZStandardDecoderTests
    {
        [Fact]
        public void Constructor_WithNullDictionary_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZStandardDecoder(null!));
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            byte[] dictionaryData = CreateSampleDictionary();
            using ZStandardDictionary dictionary = ZStandardDictionary.Create(dictionaryData);
            ZStandardDecoder decoder = new ZStandardDecoder(dictionary);

            decoder.Dispose();
            decoder.Dispose();
        }

        [Fact]
        public void GetMaxDecompressedLength_WithEmptyData_ReturnsZero()
        {
            ReadOnlySpan<byte> emptyData = ReadOnlySpan<byte>.Empty;

            int result = ZStandardDecoder.GetMaxDecompressedLength(emptyData);

            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TryDecompress_WithEmptySource_ReturnsFalse(bool useDictionary)
        {
            ReadOnlySpan<byte> emptySource = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            using ZStandardDictionary dictionary = ZStandardDictionary.Create(CreateSampleDictionary());

            bool result = useDictionary
                ? ZStandardDecoder.TryDecompress(emptySource, dictionary, destination, out int bytesWritten)
                : ZStandardDecoder.TryDecompress(emptySource, destination, out bytesWritten);

            Assert.False(result);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void TryDecompress_WithDictionary_NullDictionary_ThrowsArgumentNullException()
        {
            byte[] source = new byte[] { 1, 2, 3, 4 };
            byte[] destination = new byte[100];

            Assert.Throws<ArgumentNullException>(() =>
                ZStandardDecoder.TryDecompress(source, null!, destination, out _));
        }

        [Fact]
        public void Decompress_AfterDispose_ThrowsObjectDisposedException()
        {
            byte[] dictionaryData = CreateSampleDictionary();
            ZStandardDecoder decoder = new ZStandardDecoder();
            decoder.Dispose();

            byte[] source = new byte[] { 1, 2, 3, 4 };
            byte[] destination = new byte[100];

            Assert.Throws<ObjectDisposedException>(() =>
                decoder.Decompress(source, destination, out _, out _));
        }

        [Fact]
        public void Decompress_WithEmptySource_ReturnsNeedMoreData()
        {
            using ZStandardDecoder decoder = new ZStandardDecoder();

            ReadOnlySpan<byte> emptySource = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            OperationStatus result = decoder.Decompress(emptySource, destination, out int bytesConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.NeedMoreData, result);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);

            Assert.False(ZStandardDecoder.TryDecompress(emptySource, destination, out bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void Decompress_WithEmptyDestination_ReturnsDestinationTooSmall()
        {
            Span<byte> destination = Span<byte>.Empty;
            Span<byte> source = new byte[100];

            Assert.True(ZStandardEncoder.TryCompress("This is a test content"u8, source, out int bytesWritten));
            source = source.Slice(0, bytesWritten);

            using ZStandardDecoder decoder = new ZStandardDecoder();
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
            byte[] originalData = "Hello, World! This is a test string for ZStandard compression and decompression."u8.ToArray();
            byte[] compressedBuffer = new byte[ZStandardEncoder.GetMaxCompressedLength(originalData.Length)];
            byte[] decompressedBuffer = new byte[originalData.Length * 2];

            using ZStandardDictionary dictionary = ZStandardDictionary.Create(CreateSampleDictionary(), quality);

            int window = 10;

            int bytesWritten;
            int bytesConsumed;

            // Compress
            if (staticEncode)
            {
                bool result =
                    useDictionary
                    ? ZStandardEncoder.TryCompress(originalData, compressedBuffer, out bytesWritten, dictionary, window)
                    : ZStandardEncoder.TryCompress(originalData, compressedBuffer, out bytesWritten, quality, window);
                bytesConsumed = originalData.Length;

                Assert.True(result);
            }
            else
            {
                using var encoder = useDictionary ? new ZStandardEncoder(dictionary, window) : new ZStandardEncoder(quality, window);
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
                    ? ZStandardDecoder.TryDecompress(compressedBuffer.AsSpan(0, compressedLength), dictionary, decompressedBuffer, out bytesWritten)
                    : ZStandardDecoder.TryDecompress(compressedBuffer.AsSpan(0, compressedLength), decompressedBuffer, out bytesWritten);
                bytesConsumed = compressedLength;

                Assert.True(result);
            }
            else
            {
                using var decoder = useDictionary ? new ZStandardDecoder(dictionary) : new ZStandardDecoder();
                OperationStatus decompressResult = decoder.Decompress(compressedBuffer.AsSpan(0, compressedLength), decompressedBuffer, out bytesConsumed, out bytesWritten);

                Assert.Equal(OperationStatus.Done, decompressResult);
            }

            Assert.Equal(compressedLength, bytesConsumed);
            Assert.Equal(originalData.Length, bytesWritten);
            Assert.Equal(originalData, decompressedBuffer.AsSpan(0, bytesWritten));
        }

        private static byte[] CreateSampleDictionary() => ZStandardTestUtils.CreateSampleDictionary();
    }
}
