// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Compression
{
    public class ZstandardEncoderTests
    {
        private static int ValidWindow = 10;
        private static int ValidQuality = 3;

        public static int[] InvalidWindows = [int.MinValue, 9, 32, int.MaxValue];
        public static int[] InvalidQualities = [int.MinValue, -(1 << 17) - 1, 23, int.MaxValue];

        public static IEnumerable<object[]> InvalidWindowsTestData =>

            InvalidWindows.Select(window => new object[] { window });

        public static IEnumerable<object[]> InvalidQualitiesTestData =>
            InvalidQualities.Select(quality => new object[] { quality });

        [Theory]
        [MemberData(nameof(InvalidQualitiesTestData))]
        public void Constructor_WithInvalidQuality_ThrowsArgumentOutOfRangeException(int quality)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZstandardEncoder(quality, ValidWindow));
        }

        [Theory]
        [MemberData(nameof(InvalidWindowsTestData))]
        public void Constructor_WithInvalidWindow_ThrowsArgumentOutOfRangeException(int window)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZstandardEncoder(5, window));
        }

        [Fact]
        public void Constructor_WithDictionary_WithQuality_Succeeds()
        {
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(ZstandardTestUtils.CreateSampleDictionary(), ValidQuality);

            using var encoder = new ZstandardEncoder(dictionary, ValidWindow);
        }

        [Fact]
        public void Constructor_WithDictionary_WithoutQuality_Throws()
        {
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(ZstandardTestUtils.CreateSampleDictionary());

            Assert.Throws<ArgumentException>(() => new ZstandardEncoder(dictionary, ValidWindow));
        }

        [Fact]
        public void Constructor_WithNullDictionary_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZstandardEncoder(null!, ValidWindow));
        }

        [Fact]
        public void GetMaxCompressedLength_WithValidInput_ReturnsPositiveValue()
        {
            int inputSize = 1000;

            int maxLength = ZstandardEncoder.GetMaxCompressedLength(inputSize);

            Assert.True(maxLength > 0);
            Assert.True(maxLength >= inputSize); // Compressed size should be at least input size for worst case
        }

        [Fact]
        public void GetMaxCompressedLength_WithNegativeInput_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZstandardEncoder.GetMaxCompressedLength(-1));
        }

        [Fact]
        public void TryCompress_WithEmptyInput_ReturnsTrue()
        {
            ReadOnlySpan<byte> source = ReadOnlySpan<byte>.Empty;
            Span<byte> destination = new byte[100];

            bool result = ZstandardEncoder.TryCompress(source, destination, out int bytesWritten);

            Assert.True(result);
            Assert.Equal(0, bytesWritten);
        }

        [Fact]
        public void TryCompress_WithValidInput_CompressesData()
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            bool result = ZstandardEncoder.TryCompress(input, output, out int bytesWritten);

            Assert.True(result);
            Assert.True(bytesWritten > 0);
            Assert.True(bytesWritten < input.Length); // Should compress to smaller size
        }

        [Fact]
        public void TryCompress_WithQualityAndWindow_CompressesData()
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            bool result = ZstandardEncoder.TryCompress(input, output, out int bytesWritten, quality: ValidQuality, window: ValidWindow);

            Assert.True(result);
            Assert.True(bytesWritten > 0);
        }

        [Theory]
        [MemberData(nameof(InvalidQualitiesTestData))]
        public void TryCompress_WithInvalidQuality_ThrowsArgumentOutOfRangeException(int quality)
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            Assert.Throws<ArgumentOutOfRangeException>(() => ZstandardEncoder.TryCompress(input, output, out _, quality: quality, window: ValidWindow));
        }

        [Theory]
        [MemberData(nameof(InvalidWindowsTestData))]
        public void TryCompress_WithInvalidWindow_ThrowsArgumentOutOfRangeException(int window)
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            Assert.Throws<ArgumentOutOfRangeException>(() => ZstandardEncoder.TryCompress(input, output, out _, quality: ValidQuality, window: window));
        }

        [Fact]
        public void TryCompress_WithDictionary_WithQuality_Succeeds()
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(ZstandardTestUtils.CreateSampleDictionary(), ValidQuality);

            bool result = ZstandardEncoder.TryCompress(input, output, out int bytesWritten, dictionary: dictionary, window: ValidWindow);

            Assert.True(result);
            Assert.True(bytesWritten > 0);
        }

        [Fact]
        public void TryCompress_WithDictionary_WithoutQuality_Throws()
        {
            byte[] input = CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(ZstandardTestUtils.CreateSampleDictionary());

            Assert.Throws<ArgumentException>(() => ZstandardEncoder.TryCompress(input, output, out _, dictionary: dictionary, window: ValidWindow));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Compress_WithValidInput_CompressesData(bool explicitInit)
        {
            using var encoder = explicitInit ? new ZstandardEncoder(ValidQuality, ValidWindow) : new ZstandardEncoder();
            byte[] input = CreateTestData();
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            OperationStatus result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);

            Assert.Equal(OperationStatus.Done, result);
            Assert.Equal(input.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);
        }

        [Fact]
        public void Compress_AfterDispose_ThrowsObjectDisposedException()
        {
            var encoder = new ZstandardEncoder(ValidQuality, ValidWindow);
            encoder.Dispose();
            byte[] input = CreateTestData();
            byte[] output = new byte[100];

            Assert.Throws<ObjectDisposedException>(() => encoder.Compress(input, output, out _, out _, isFinalBlock: true));
        }

        [Fact]
        public void Flush_WithValidEncoder_Succeeds()
        {
            using var encoder = new ZstandardEncoder(ValidQuality, ValidWindow);
            byte[] output = new byte[1000];

            OperationStatus result = encoder.Flush(output, out int bytesWritten);

            Assert.True(result == OperationStatus.Done || result == OperationStatus.DestinationTooSmall);
            Assert.True(bytesWritten >= 0);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var encoder = new ZstandardEncoder(ValidQuality, ValidWindow);

            encoder.Dispose();
            encoder.Dispose();
        }

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
