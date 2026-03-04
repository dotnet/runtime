// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Compression
{
    public class ZLibCompressionOptionsUnitTests
    {
        [Fact]
        public void ZLibCompressionOptionsInvalidCompressionLevel()
        {
            ZLibCompressionOptions options = new();

            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.CompressionLevel = -2);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.CompressionLevel = 10);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new ZLibCompressionOptions() { CompressionLevel = 11 });
        }

        [Fact]
        public void ZLibCompressionOptionsInvalidCompressionStrategy()
        {
            ZLibCompressionOptions options = new();

            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.CompressionStrategy = (ZLibCompressionStrategy)(-1));
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.CompressionStrategy = (ZLibCompressionStrategy)5);
            Assert.Throws<ArgumentOutOfRangeException>("value", () => new ZLibCompressionOptions() { CompressionStrategy = (ZLibCompressionStrategy)15 });
        }

        [Fact]
        public void ZLibCompressionOptionsValidOptions()
        {
            ZLibCompressionOptions options = new();

            Assert.Equal(-1, options.CompressionLevel);
            Assert.Equal(ZLibCompressionStrategy.Default, options.CompressionStrategy);

            options.CompressionLevel = 5;
            options.CompressionStrategy = ZLibCompressionStrategy.HuffmanOnly;

            Assert.Equal(5, options.CompressionLevel);
            Assert.Equal(ZLibCompressionStrategy.HuffmanOnly, options.CompressionStrategy);
        }

        [Fact]
        public void WindowLog_DefaultValue()
        {
            ZLibCompressionOptions options = new();

            Assert.Equal(-1, options.WindowLog);
        }

        [Theory]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(15)]
        [InlineData(-1)]
        public void WindowLog_SetToValidRange_Succeeds(int windowLog)
        {
            ZLibCompressionOptions options = new();
            options.WindowLog = windowLog;
            Assert.Equal(windowLog, options.WindowLog);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(0)]
        [InlineData(7)]
        [InlineData(16)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void WindowLog_SetOutOfRange_ThrowsArgumentOutOfRangeException(int windowLog)
        {
            ZLibCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.WindowLog = windowLog);
        }

        [Fact]
        public void StaticWindowLogProperties_ReturnExpectedValues()
        {
            Assert.Equal(15, ZLibCompressionOptions.DefaultWindowLog);
            Assert.Equal(8, ZLibCompressionOptions.MinWindowLog);
            Assert.Equal(15, ZLibCompressionOptions.MaxWindowLog);
        }
    }
}
