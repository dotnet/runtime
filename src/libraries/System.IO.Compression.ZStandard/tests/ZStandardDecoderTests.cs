// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        public void TryDecompress_WithEmptySource_ReturnsTrue(bool useDictionary)
        {
            ReadOnlySpan<byte> emptySource = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            using ZStandardDictionary dictionary = ZStandardDictionary.Create(CreateSampleDictionary());

            bool result = useDictionary
                ? ZStandardDecoder.TryDecompress(emptySource, dictionary, destination, out int bytesWritten)
                : ZStandardDecoder.TryDecompress(emptySource, destination, out bytesWritten);

            Assert.True(result);
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
        public void Decompress_WithEmptySource_ReturnsOperationStatusDone()
        {
            byte[] dictionaryData = CreateSampleDictionary();
            using ZStandardDecoder decoder = new ZStandardDecoder();

            ReadOnlySpan<byte> emptySource = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            OperationStatus result = decoder.Decompress(emptySource, destination, out int bytesConsumed, out int bytesWritten);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(0, bytesConsumed);
            Assert.Equal(0, bytesWritten);
        }

        private static byte[] CreateSampleDictionary() => ZStandardTestUtils.CreateSampleDictionary();
    }
}
