// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Compression
{
    public class ZstandardCompressionOptionsTests
    {
        [Fact]
        public void Parameters_SetToZero_Succeeds()
        {
            ZstandardCompressionOptions options = new();
            options.Quality = 0;
            options.WindowLog = 0;
            options.TargetBlockSize = 0;
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(1)]
        [InlineData(22)]
        public void Quality_SetToValidRange_Succeeds(int quality)
        {
            ZstandardCompressionOptions options = new();
            options.Quality = quality; // Should not throw
            Assert.Equal(quality, options.Quality);
        }

        [Theory]
        [InlineData(-10000000)]
        [InlineData(1000)]
        public void Quality_SetOutOfRange_ThrowsArgumentOutOfRangeException(int quality)
        {
            ZstandardCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.Quality = quality);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(23)]
        [InlineData(30)]
        public void WindowLog_SetToValidRange_Succeeds(int windowLog)
        {
            ZstandardCompressionOptions options = new();
            options.WindowLog = windowLog; // Should not throw
            Assert.Equal(windowLog, options.WindowLog);
        }

        [Theory]
        [InlineData(9)]
        [InlineData(32)]
        public void WindowLog_SetOutOfRange_ThrowsArgumentOutOfRangeException(int windowLog)
        {
            ZstandardCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.WindowLog = windowLog);
        }

        [Theory]
        [InlineData(1340)]
        [InlineData(65536)]
        [InlineData(131072)]
        public void TargetBlockSize_SetToValidRange_Succeeds(int targetBlockSize)
        {
            ZstandardCompressionOptions options = new();
            options.TargetBlockSize = targetBlockSize; // Should not throw
            Assert.Equal(targetBlockSize, options.TargetBlockSize);
        }

        [Theory]
        [InlineData(1339)]
        [InlineData(131073)]
        public void TargetBlockSize_SetOutOfRange_ThrowsArgumentOutOfRangeException(int targetBlockSize)
        {
            ZstandardCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.TargetBlockSize = targetBlockSize);
        }
    }

    public class ZstandardDecompressionOptionsTests
    {
        [Fact]
        public void MaxWindowLog_SetToZero_Succeeds()
        {
            ZstandardDecompressionOptions options = new();
            options.MaxWindowLog = 0;
            Assert.Equal(0, options.MaxWindowLog);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(23)]
        [InlineData(30)]
        public void MaxWindowLog_SetToValidRange_Succeeds(int maxWindowLog)
        {
            ZstandardDecompressionOptions options = new();
            options.MaxWindowLog = maxWindowLog;
            Assert.Equal(maxWindowLog, options.MaxWindowLog);
        }

        [Theory]
        [InlineData(9)]
        [InlineData(32)]
        public void MaxWindowLog_SetOutOfRange_ThrowsArgumentOutOfRangeException(int maxWindowLog)
        {
            ZstandardDecompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxWindowLog = maxWindowLog);
        }

        [Fact]
        public void Dictionary_SetAndGet_RoundTrips()
        {
            using ZstandardDictionary dictionary = ZstandardDictionary.Create(ZstandardTestUtils.CreateSampleDictionary());
            ZstandardDecompressionOptions options = new();
            options.Dictionary = dictionary;
            Assert.Same(dictionary, options.Dictionary);
        }

        [Fact]
        public void Dictionary_DefaultValue_IsNull()
        {
            ZstandardDecompressionOptions options = new();
            Assert.Null(options.Dictionary);
        }
    }
}
