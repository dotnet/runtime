// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace System.IO.Compression.Tests
{
    public class Perf_GZipStream
    {
        public static IEnumerable<object[]> CanterburyCorpus()
        {
            foreach (CompressionLevel compressionLevel in Enum.GetValues(typeof(CompressionLevel)))
            {
                foreach (int innerIterations in new int[] { 1, 10 })
                {
                    yield return new object[] { innerIterations, "alice29.txt", compressionLevel };
                    yield return new object[] { innerIterations, "asyoulik.txt", compressionLevel };
                    yield return new object[] { innerIterations, "cp.html", compressionLevel };
                    yield return new object[] { innerIterations, "fields.c", compressionLevel };
                    yield return new object[] { innerIterations, "grammar.lsp", compressionLevel };
                    yield return new object[] { innerIterations, "kennedy.xls", compressionLevel };
                    yield return new object[] { innerIterations, "lcet10.txt", compressionLevel };
                    yield return new object[] { innerIterations, "plrabn12.txt", compressionLevel };
                    yield return new object[] { innerIterations, "ptt5", compressionLevel };
                    yield return new object[] { innerIterations, "sum", compressionLevel };
                    yield return new object[] { innerIterations, "xargs.1", compressionLevel };
                }
            }
        }

        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark]
        [MemberData("CanterburyCorpus")]
        public void Compress_Canterbury(int innerIterations, string fileName, CompressionLevel compressLevel)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine("GZTestData", "Canterbury", fileName));
            PerfUtils utils = new PerfUtils();
            FileStream[] filestreams = new FileStream[innerIterations];
            GZipStream[] gzips = new GZipStream[innerIterations];
            string[] paths = new string[innerIterations];
            foreach (var iteration in Benchmark.Iterations)
            {
                for (int i = 0; i < innerIterations; i++)
                {
                    paths[i] = utils.GetTestFilePath();
                    filestreams[i] = File.Create(paths[i]);
                }
                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                    {
                        gzips[i] = new GZipStream(filestreams[i], compressLevel);
                        gzips[i].Write(bytes, 0, bytes.Length);
                        gzips[i].Flush();
                        gzips[i].Dispose();
                        filestreams[i].Dispose();
                    }
                for (int i = 0; i < innerIterations; i++)
                    File.Delete(paths[i]);
            }
        }

        /// <summary>
        /// Benchmark tests to measure the performance of individually compressing each file in the
        /// Canterbury Corpus
        /// </summary>
        [Benchmark]
        [MemberData("CanterburyCorpus")]
        public void Decompress_Canterbury(int innerIterations, string fileName, CompressionLevel compressLevel)
        {
            string zipFilePath = Path.Combine("GZTestData", "Canterbury", "GZcompressed", fileName + ".gz");
            string sourceFilePath = Path.Combine("GZTestData", "Canterbury", fileName);
            byte[] outputRead = new byte[new FileInfo(sourceFilePath).Length];
            MemoryStream[] memories = new MemoryStream[innerIterations];
            GZipStream[] gzips = new GZipStream[innerIterations];
            foreach (var iteration in Benchmark.Iterations)
            {
                for (int i = 0; i < innerIterations; i++)
                    memories[i] = new MemoryStream(File.ReadAllBytes(zipFilePath));

                using (iteration.StartMeasurement())
                    for (int i = 0; i < innerIterations; i++)
                        using (GZipStream unzip = new GZipStream(memories[i], CompressionMode.Decompress))
                            unzip.Read(outputRead, 0, outputRead.Length);

                for (int i = 0; i < innerIterations; i++)
                    memories[i].Dispose();
            }
        }
    }

}
