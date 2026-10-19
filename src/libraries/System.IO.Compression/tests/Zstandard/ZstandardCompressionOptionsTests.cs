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
            options.HashLog2 = 0;
            options.ChainLog2 = 0;
            Assert.Equal(0, options.HashLog2);
            Assert.Equal(0, options.ChainLog2);
        }

        [Fact]
        public void StaticBounds_HaveExpectedValues()
        {
            Assert.Equal(6, ZstandardCompressionOptions.MinHashLog2);
            Assert.Equal(30, ZstandardCompressionOptions.MaxHashLog2);
            Assert.Equal(6, ZstandardCompressionOptions.MinChainLog2);
            Assert.Equal(Environment.Is64BitProcess ? 30 : 29, ZstandardCompressionOptions.MaxChainLog2);
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

        [Theory]
        [InlineData(6)]
        [InlineData(15)]
        [InlineData(20)]
        [InlineData(30)]
        public void HashLog2_SetToValidRange_Succeeds(int hashLog2)
        {
            ZstandardCompressionOptions options = new();
            options.HashLog2 = hashLog2; // Should not throw
            Assert.Equal(hashLog2, options.HashLog2);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(31)]
        public void HashLog2_SetOutOfRange_ThrowsArgumentOutOfRangeException(int hashLog2)
        {
            ZstandardCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.HashLog2 = hashLog2);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(15)]
        [InlineData(20)]
        [InlineData(29)]
        public void ChainLog2_SetToValidRange_Succeeds(int chainLog2)
        {
            ZstandardCompressionOptions options = new();
            options.ChainLog2 = chainLog2; // Should not throw
            Assert.Equal(chainLog2, options.ChainLog2);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(31)]
        public void ChainLog2_SetOutOfRange_ThrowsArgumentOutOfRangeException(int chainLog2)
        {
            ZstandardCompressionOptions options = new();
            Assert.Throws<ArgumentOutOfRangeException>(() => options.ChainLog2 = chainLog2);
        }

        [Fact]
        public void Encoder_WithHashLog2AndChainLog2_CompressesAndDecompressesSuccessfully()
        {
            ZstandardCompressionOptions options = new()
            {
                Quality = 19,
                HashLog2 = 12,
                ChainLog2 = 12
            };

            using ZstandardEncoder encoder = new(options);
            byte[] input = ZstandardTestUtils.CreateTestData(1024);
            byte[] output = new byte[ZstandardEncoder.GetMaxCompressedLength(input.Length)];

            System.Buffers.OperationStatus result = encoder.Compress(input, output, out int bytesConsumed, out int bytesWritten, isFinalBlock: true);
            Assert.Equal(System.Buffers.OperationStatus.Done, result);
            Assert.Equal(input.Length, bytesConsumed);
            Assert.True(bytesWritten > 0);

            using ZstandardDecoder decoder = new();
            byte[] decompressed = new byte[input.Length];
            System.Buffers.OperationStatus decompressResult = decoder.Decompress(output.AsSpan(0, bytesWritten), decompressed, out int decompConsumed, out int decompWritten);
            Assert.Equal(System.Buffers.OperationStatus.Done, decompressResult);
            Assert.Equal(bytesWritten, decompConsumed);
            Assert.Equal(input.Length, decompWritten);
            Assert.Equal(input, decompressed);
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
