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

        public static IEnumerable<object[]> CanterburyCorpus_WithCompressionLevel()
        {
            foreach (CompressionLevel compressionLevel in Enum.GetValues(typeof(CompressionLevel)))
            {
                foreach (object[] canterburyWithoutLevel in CanterburyCorpus())
                {
                    yield return new object[] { canterburyWithoutLevel[0], canterburyWithoutLevel[1], compressionLevel };
                }
            }
        }

        public static IEnumerable<object[]> CanterburyCorpus()
        {
            foreach (int innerIterations in new int[] { 1, 10 })
            {
                foreach (var fileName in UncompressedTestFiles())
                {
                    yield return new object[] { innerIterations, fileName[0] };
                }
            }
        }

        [Benchmark]
        [MemberData(nameof(CanterburyCorpus_WithCompressionLevel))]
        public void Compress_Canterbury_WithState(int innerIterations, string uncompressedFileName, CompressionLevel compressLevel)
        {
            byte[] bytes = File.ReadAllBytes(uncompressedFileName);
            ReadOnlySpan<byte> uncompressedData = new ReadOnlySpan<byte>(bytes);
            int maxCompressedSize = BrotliEncoder.GetMaxCompressedLength(bytes.Length);
            List<byte[]> compressedDataArrays = new List<byte[]>(innerIterations);
            foreach (var iteration in Benchmark.Iterations)
            {
                for (int i = 0; i < innerIterations; i++)
                {
                    compressedDataArrays.Add(new byte[maxCompressedSize]);
                }
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        using (BrotliEncoder encoder = new BrotliEncoder())
                        {
                            Span<byte> output = new Span<byte>(compressedDataArrays[i]);
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
            }
        }

        [Benchmark]
        [MemberData(nameof(CanterburyCorpus))]
        public void Decompress_Canterbury_WithState(int innerIterations, string uncompressedFileName)
        {
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

        [Benchmark]
        [MemberData(nameof(CanterburyCorpus_WithCompressionLevel))]
        public void Compress_Canterbury_WithoutState(int innerIterations, string uncompressedFileName, CompressionLevel compressLevel)
        {
            byte[] bytes = File.ReadAllBytes(uncompressedFileName);
            ReadOnlySpan<byte> uncompressedData = new ReadOnlySpan<byte>(bytes);
            int maxCompressedSize = BrotliEncoder.GetMaxCompressedLength(bytes.Length);
            List<byte[]> compressedDataArrays = new List<byte[]>(innerIterations);
            int compressLevelBrotli = compressLevel == CompressionLevel.Optimal ? 11 : compressLevel == CompressionLevel.Fastest ? 1 : 0;
            foreach (var iteration in Benchmark.Iterations)
            {
                for (int i = 0; i < innerIterations; i++)
                {
                    compressedDataArrays.Add(new byte[maxCompressedSize]);
                }
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        Assert.True(BrotliEncoder.TryCompress(uncompressedData, compressedDataArrays[i], out int bytesWritten, compressLevelBrotli, 22));
                    }
                }
            }
        }

        /// <summary>
        /// The perf tests for the instant decompression aren't exactly indicative of real-world scenarios since they require you to know 
        /// either the exact figure or the upper bound of the uncompressed size of your given compressed data.
        /// </summary>
        [Benchmark]
        [MemberData(nameof(CanterburyCorpus))]
        public void Decompress_Canterbury_WithoutState(int innerIterations, string uncompressedFileName)
        {
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
