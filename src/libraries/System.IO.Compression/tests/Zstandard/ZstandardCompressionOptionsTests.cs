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
            options.WindowLog2 = 0;
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
        public void WindowLog2_SetToValidRange_Succeeds(int windowLog2)
        {
            ZstandardCompressionOptions options = new();
            options.WindowLog2 = windowLog2; // Should not throw
            Assert.Equal(windowLog2, options.WindowLog2);
        }

        [Theory]
        [InlineData(9)]
        [InlineData(32)]
        public void WindowLog2_SetOutOfRange_ThrowsArgumentOutOfRangeException(int windowLog2)
        {
            ZstandardCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.WindowLog2 = windowLog2);
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
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(23)]
        [InlineData(30)]
        public void MaxWindowLog2_SetToValidRange_Succeeds(int maxWindowLog2)
        {
            ZstandardDecompressionOptions options = new();
            options.MaxWindowLog2 = maxWindowLog2;
            Assert.Equal(maxWindowLog2, options.MaxWindowLog2);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(9)]
        [InlineData(32)]
        public void MaxWindowLog2_SetOutOfRange_ThrowsArgumentOutOfRangeException(int maxWindowLog2)
        {
            ZstandardDecompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxWindowLog2 = maxWindowLog2);
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
