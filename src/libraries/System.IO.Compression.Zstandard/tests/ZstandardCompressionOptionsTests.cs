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
            options.Window = 0;
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
        [InlineData(31)]
        public void Window_SetToValidRange_Succeeds(int window)
        {
            ZstandardCompressionOptions options = new();
            options.Window = window; // Should not throw
            Assert.Equal(window, options.Window);
        }

        [Theory]
        [InlineData(9)]
        [InlineData(32)]
        public void Window_SetOutOfRange_ThrowsArgumentOutOfRangeException(int window)
        {
            ZstandardCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.Window = window);
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
}