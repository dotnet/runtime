// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace System.IO.Compression.Tests
{
    public class Perf_DeflateStream
    {
        private static List<object[]> _compressedFiles;
        private static List<object[]> _byteArraysToCompress;

        /// <summary>
        /// Yields an Enumerable list of paths to GZTestData files
        /// </summary>
        public static IEnumerable<object[]> CompressedFiles()
        {
            if (_compressedFiles == null)
            {
                PerfUtils utils = new PerfUtils();
                _compressedFiles = new List<object[]>();
                // Crypto random data
                byte[] bytes = new byte[100000000];
                var rand = RandomNumberGenerator.Create();
                rand.GetBytes(bytes);
                string filePath = utils.GetTestFilePath() + ".gz";
                using (FileStream output = File.Create(filePath))
                using (GZipStream zip = new GZipStream(output, CompressionMode.Compress))
                    zip.Write(bytes, 0, bytes.Length);
                _compressedFiles.Add(new object[] { filePath });

                // Create a compressed file with repeated segments
                bytes = Text.Encoding.UTF8.GetBytes(utils.CreateString(100000));
                filePath = utils.GetTestFilePath() + ".gz";
                using (FileStream output = File.Create(filePath))
                using (GZipStream zip = new GZipStream(output, CompressionMode.Compress))
                    for (int i = 0; i < 1000; i++)
                        zip.Write(bytes, 0, bytes.Length);
                _compressedFiles.Add(new object[] { filePath });
            }
            return _compressedFiles;
        }

        // Creates byte arrays that contain random data to be compressed
        public static IEnumerable<object[]> ByteArraysToCompress()
        {
            if (_byteArraysToCompress == null)
            {
                PerfUtils utils = new PerfUtils();
                _byteArraysToCompress = new List<object[]>();

                // Regular, semi well formed data
                _byteArraysToCompress.Add(new object[] { Text.Encoding.UTF8.GetBytes(utils.CreateString(100000000)) });

                // Crypto random data
                {
                    byte[] bytes = new byte[100000000];
                    var rand = RandomNumberGenerator.Create();
                    rand.GetBytes(bytes);
                    _byteArraysToCompress.Add(new object[] { bytes });
                }

                // Highly repeated data
                {
                    byte[] bytes = new byte[101000000];
                    byte[] small = Text.Encoding.UTF8.GetBytes(utils.CreateString(100000));
                    for (int i = 0; i < 1000; i++)
                        small.CopyTo(bytes, 100000 * i);
                    _byteArraysToCompress.Add(new object[] { bytes });
                }
            }
            return _byteArraysToCompress;
        }

        [Benchmark]
        [MemberData("CompressedFiles")]
        public async void Decompress(string testFilePath)
        {
            int _bufferSize = 1024;
            int retCount = -1;
            var bytes = new byte[_bufferSize];
            using (MemoryStream gzStream = await LocalMemoryStream.readAppFileAsync(testFilePath))
            using (MemoryStream strippedMs = StripHeaderAndFooter.Strip(gzStream))
                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < 20000; i++)
                        {
                            using (DeflateStream zip = new DeflateStream(strippedMs, CompressionMode.Decompress, leaveOpen: true))
                            {
                                while (retCount != 0)
                                {
                                    retCount = zip.Read(bytes, 0, _bufferSize);
                                }
                            }
                            strippedMs.Seek(0, SeekOrigin.Begin);
                        }
        }

        [Benchmark]
        [MemberData("ByteArraysToCompress")]
        public void Compress(byte[] bytes)
        {
            PerfUtils utils = new PerfUtils();
            foreach (var iteration in Benchmark.Iterations)
            {
                string filePath = utils.GetTestFilePath();
                using (FileStream output = File.Create(filePath))
                using (DeflateStream zip = new DeflateStream(output, CompressionMode.Compress))
                using (iteration.StartMeasurement())
                {
                    zip.Write(bytes, 0, bytes.Length);
                }
                File.Delete(filePath);
            }
        }
    }
}
