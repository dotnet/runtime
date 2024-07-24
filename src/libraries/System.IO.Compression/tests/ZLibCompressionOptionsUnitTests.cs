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
            ZLibCompressionOptions options = new() { CompressionLevel = -1, CompressionStrategy = ZLibCompressionStrategy.HuffmanOnly };

            Assert.Equal( -1, options.CompressionLevel);
            Assert.Equal(ZLibCompressionStrategy.HuffmanOnly, options.CompressionStrategy);
        }
    }
}
