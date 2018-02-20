// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.IO.Compression
{
    public abstract class CompressionStreamPerfTestBase : CompressionStreamTestBase
    {
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

        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark(InnerIterationCount=10)] // limits the max iterations to 100
        [MemberData(nameof(UncompressedTestFiles_WithCompressionLevel))]
        public void Compress_Canterbury(string uncompressedFileName, CompressionLevel compressLevel)
        {
            byte[] bytes = File.ReadAllBytes(uncompressedFileName);
            foreach (var iteration in Benchmark.Iterations)
            {
                // resizing a memory stream during compression will throw off our results, so pre-size it to the
                // size of our input
                using (MemoryStream compressedDataStream = new MemoryStream(bytes.Length))
                using (iteration.StartMeasurement())
                using (Stream compressor = CreateStream(compressedDataStream, compressLevel))
                    compressor.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark(InnerIterationCount=100)]
        [MemberData(nameof(UncompressedTestFiles))]
        public void Decompress_Canterbury(string uncompressedFilePath)
        {
            int innerIterations = (int)Benchmark.InnerIterationCount;
            string compressedFilePath = CompressedTestFile(uncompressedFilePath);
            byte[] outputRead = new byte[new FileInfo(uncompressedFilePath).Length];
            MemoryStream[] memories = new MemoryStream[innerIterations];
            foreach (var iteration in Benchmark.Iterations)
            {
                for (int i = 0; i < innerIterations; i++)
                    memories[i] = new MemoryStream(File.ReadAllBytes(compressedFilePath));

                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                        using (Stream decompressor = CreateStream(memories[i], CompressionMode.Decompress))
                            decompressor.Read(outputRead, 0, outputRead.Length);

                for (int i = 0; i < innerIterations; i++)
                    memories[i].Dispose();
            }
        }
    }
}
