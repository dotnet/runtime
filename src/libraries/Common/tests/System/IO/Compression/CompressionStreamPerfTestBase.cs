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


        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark]
        [MemberData(nameof(CanterburyCorpus_WithCompressionLevel))]
        public void Compress_Canterbury(int innerIterations, string uncompressedFileName, CompressionLevel compressLevel)
        {
            byte[] bytes = File.ReadAllBytes(uncompressedFileName);
            MemoryStream[] compressedDataStreams = new MemoryStream[innerIterations];
            foreach (var iteration in Benchmark.Iterations)
            {
                // resizing a memory stream during compression will throw off our results, so pre-size it to the
                // size of our input
                for (int i = 0; i < innerIterations; i++)
                    compressedDataStreams[i] = new MemoryStream(bytes.Length);
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                        using (Stream compressor = CreateStream(compressedDataStreams[i], compressLevel))
                            compressor.Write(bytes, 0, bytes.Length);

                for (int i = 0; i < innerIterations; i++)
                    compressedDataStreams[i].Dispose();
            }
        }

        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark]
        [MemberData(nameof(CanterburyCorpus))]
        public void Decompress_Canterbury(int innerIterations, string uncompressedFilePath)
        {
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
