// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.IO.Compression
{
    public class BrotliPerfTests : CompressionTestBase
    {
        protected override string CompressedTestFile(string uncompressedPath) => Path.Combine("BrotliTestData", Path.GetFileName(uncompressedPath) + ".br");

        public static IEnumerable<object[]> UncompressedTestFiles_WithCompressionLevel()
        {
            foreach (CompressionLevel compressionLevel in Enum.GetValues(typeof(CompressionLevel)))
            {
                foreach (object[] testFile in UncompressedTestFiles())
                {
                    yield return new object[] { testFile[0], compressionLevel };
                }
            }
        }

        [Benchmark(InnerIterationCount=10)] // limits the max iterations to 100
        [MemberData(nameof(UncompressedTestFiles_WithCompressionLevel))]
        public void Compress_Canterbury_WithState(string uncompressedFileName, CompressionLevel compressLevel)
        {
            byte[] bytes = File.ReadAllBytes(uncompressedFileName);
            ReadOnlySpan<byte> uncompressedData = new ReadOnlySpan<byte>(bytes);
            int maxCompressedSize = BrotliEncoder.GetMaxCompressedLength(bytes.Length);
            byte[] compressedDataArray = new byte[maxCompressedSize];
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                using (BrotliEncoder encoder = new BrotliEncoder())
                {
                    Span<byte> output = new Span<byte>(compressedDataArray);
                    ReadOnlySpan<byte> input = uncompressedData;
                    while (!input.IsEmpty && !output.IsEmpty)
                    {
                        encoder.Compress(input, output, out int bytesConsumed, out int written, isFinalBlock:false);
                        input = input.Slice(bytesConsumed);
                        output = output.Slice(written);
                    }
                    encoder.Compress(input, output, out int bytesConsumed2, out int written2, isFinalBlock: true);
                }
            }
        }

        [Benchmark(InnerIterationCount=100)]
        [MemberData(nameof(UncompressedTestFiles))]
        public void Decompress_Canterbury_WithState(string uncompressedFileName)
        {
            int innerIterations = (int)Benchmark.InnerIterationCount;
            byte[] compressedBytes = File.ReadAllBytes(CompressedTestFile(uncompressedFileName));
            ReadOnlySpan<byte> compressedData = new ReadOnlySpan<byte>(compressedBytes);
            List<byte[]> uncompressedDataArrays = new List<byte[]>(innerIterations);
            foreach (var iteration in Benchmark.Iterations)
            {
                for (int i = 0; i < innerIterations; i++)
                {
                    uncompressedDataArrays.Add(new byte[65520]);
                }
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        using (BrotliDecoder decoder = new BrotliDecoder())
                        {
                            Span<byte> output = new Span<byte>(uncompressedDataArrays[i]);
                            ReadOnlySpan<byte> input = compressedData;
                            while (!input.IsEmpty && !output.IsEmpty)
                            {
                                decoder.Decompress(input, output, out int bytesConsumed, out int written);
                                input = input.Slice(bytesConsumed);
                                output = output.Slice(written);
                            }
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount=10)] // limits the max iterations to 100
        [MemberData(nameof(UncompressedTestFiles_WithCompressionLevel))]
        public void Compress_Canterbury_WithoutState(string uncompressedFileName, CompressionLevel compressLevel)
        {
            byte[] bytes = File.ReadAllBytes(uncompressedFileName);
            ReadOnlySpan<byte> uncompressedData = new ReadOnlySpan<byte>(bytes);
            int maxCompressedSize = BrotliEncoder.GetMaxCompressedLength(bytes.Length);
            byte[] compressedDataArray = new byte[maxCompressedSize];
            int compressLevelBrotli = compressLevel == CompressionLevel.Optimal ? 11 : compressLevel == CompressionLevel.Fastest ? 1 : 0;
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    Assert.True(BrotliEncoder.TryCompress(uncompressedData, compressedDataArray, out int bytesWritten, compressLevelBrotli, 22));
                }
            }
        }

        /// <summary>
        /// The perf tests for the instant decompression aren't exactly indicative of real-world scenarios since they require you to know 
        /// either the exact figure or the upper bound of the uncompressed size of your given compressed data.
        /// </summary>
        [Benchmark(InnerIterationCount=100)]
        [MemberData(nameof(UncompressedTestFiles))]
        public void Decompress_Canterbury_WithoutState(string uncompressedFileName)
        {
            int innerIterations = (int)Benchmark.InnerIterationCount;
            byte[] compressedBytes = File.ReadAllBytes(CompressedTestFile(uncompressedFileName));
            ReadOnlySpan<byte> compressedData = new ReadOnlySpan<byte>(compressedBytes);
            int uncompressedSize = (int)new FileInfo(uncompressedFileName).Length;
            List<byte[]> uncompressedDataArrays = new List<byte[]>(innerIterations);
            foreach (var iteration in Benchmark.Iterations)
            {
                for (int i = 0; i < innerIterations; i++)
                {
                    uncompressedDataArrays.Add(new byte[uncompressedSize]);
                }
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Assert.True(BrotliDecoder.TryDecompress(compressedData, uncompressedDataArrays[i], out int bytesWritten));
                    }
                }
            }
        }           
    }
}
