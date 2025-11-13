// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        public void Constructor_WithNullDictionary_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ZstandardEncoder(null!, ValidWindow));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1000)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue / 2)]
        public void GetMaxCompressedLength_WithValidInput_ReturnsPositiveValue(long inputSize)
        {
            long maxLength = ZstandardEncoder.GetMaxCompressedLength(inputSize);

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
        public void Encoder_Finalize()
        {
            {
                var encoder = new ZstandardEncoder(ValidQuality, ValidWindow);
                byte[] input = CreateTestData();
                byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

                OperationStatus result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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

        [Fact]
        public void Reset_AfterDispose_ThrowsObjectDisposedException()
        {
            var encoder = new ZstandardEncoder(ValidQuality, ValidWindow);
            encoder.Dispose();

            Assert.Throws<ObjectDisposedException>(() => encoder.Reset());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Reset_AllowsReuseForMultipleCompressions(bool useDictionary)
        {
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(ZstandardTestUtils.CreateSampleDictionary(), ValidQuality);
            using var encoder = useDictionary
                ? new ZstandardEncoder(dictionary, ValidWindow)
                : new ZstandardEncoder(ValidQuality, ValidWindow);

            byte[] input = CreateTestData();
            byte[] output1 = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];
            byte[] output2 = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            // First compression
            OperationStatus result1 = encoder.Compress(input, output1, out int consumed1, out int written1, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, result1);
            Assert.Equal(input.Length, consumed1);
            Assert.True(written1 > 0);

            // Reset and compress again
            encoder.Reset();
            OperationStatus result2 = encoder.Compress(input, output2, out int consumed2, out int written2, isFinalBlock: true);
            Assert.Equal(OperationStatus.Done, result2);
            Assert.Equal(input.Length, consumed2);
            Assert.True(written2 > 0);

            Assert.Equal(output1, output2);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(long.MinValue)]
        public void SetSourceSize_WithNegativeSize_ThrowsArgumentOutOfRangeException(long size)
        {
            using var encoder = new ZstandardEncoder();

            Assert.Throws<ArgumentOutOfRangeException>(() => encoder.SetSourceSize(size));
        }

        [Fact]
        public void SetSourceSize_AfterDispose_ThrowsObjectDisposedException()
        {
            var encoder = new ZstandardEncoder();
            encoder.Dispose();

            Assert.Throws<ObjectDisposedException>(() => encoder.SetSourceSize(100));
        }

        [Fact]
        public void SetSourceSize_BeforeCompression_AllowsCorrectSizeCompression()
        {
            using var encoder = new ZstandardEncoder();
            byte[] input = CreateTestData();
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
        public void SetSourceSize_SmallerThanActualData_DoesNotReadFurther(long delta)
        {
            using var encoder = new ZstandardEncoder();
            byte[] input = CreateTestData();
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
            using var encoder = new ZstandardEncoder();
            byte[] input = CreateTestData();
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
            using var encoder = new ZstandardEncoder();
            byte[] input = CreateTestData();
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
