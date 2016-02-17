// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace System.IO.Compression.Tests
{
    public class Perf_DeflateStream
    {
        private static string CreateCompressedFile(CompressionType type)
        {
            const int fileSize = 1000000;
            PerfUtils utils = new PerfUtils();
            string filePath = utils.GetTestFilePath() + ".gz";
            switch (type)
            {
                case CompressionType.CryptoRandom:
                    using (RandomNumberGenerator rand = RandomNumberGenerator.Create())
                    {
                        byte[] bytes = new byte[fileSize];
                        rand.GetBytes(bytes);
                        using (FileStream output = File.Create(filePath))
                        using (GZipStream zip = new GZipStream(output, CompressionMode.Compress))
                            zip.Write(bytes, 0, bytes.Length);
                    }
                    break;
                case CompressionType.RepeatedSegments:
                    {
                        byte[] bytes = new byte[fileSize / 1000];
                        new Random(128453).NextBytes(bytes);
                        using (FileStream output = File.Create(filePath))
                        using (GZipStream zip = new GZipStream(output, CompressionMode.Compress))
                            for (int i = 0; i < 1000; i++)
                                zip.Write(bytes, 0, bytes.Length);
                    }
                    break;
                case CompressionType.NormalData:
                    {
                        byte[] bytes = new byte[fileSize];
                        new Random(128453).NextBytes(bytes);
                        using (FileStream output = File.Create(filePath))
                        using (GZipStream zip = new GZipStream(output, CompressionMode.Compress))
                            zip.Write(bytes, 0, bytes.Length);
                    }
                    break;
            }
            return filePath;
        }

        private static byte[] CreateBytesToCompress(CompressionType type)
        {
            const int fileSize = 500000;
            byte[] bytes = new byte[fileSize];
            switch (type)
            {
                case CompressionType.CryptoRandom:
                    using (RandomNumberGenerator rand = RandomNumberGenerator.Create())
                        rand.GetBytes(bytes);
                    break;
                case CompressionType.RepeatedSegments:
                    {
                        byte[] small = new byte[1000];
                        new Random(123453).NextBytes(small);
                        for (int i = 0; i < fileSize / 1000; i++)
                        {
                            small.CopyTo(bytes, 1000 * i);
                        }
                    }
                    break;
                case CompressionType.VeryRepetitive:
                    {
                        byte[] small = new byte[100];
                        new Random(123453).NextBytes(small);
                        for (int i = 0; i < fileSize / 100; i++)
                        {
                            small.CopyTo(bytes, 100 * i);
                        }
                        break;
                    }
                case CompressionType.NormalData:
                    new Random(123453).NextBytes(bytes);
                    break;
            }
            return bytes;
        }

        public enum CompressionType
        {
            CryptoRandom = 1,
            RepeatedSegments = 2,
            VeryRepetitive = 3,
            NormalData = 4
        }

        [Benchmark]
        [InlineData(CompressionType.CryptoRandom)]
        [InlineData(CompressionType.RepeatedSegments)]
        [InlineData(CompressionType.NormalData)]
        public async void Decompress(CompressionType type)
        {
            string testFilePath = CreateCompressedFile(type);
            int _bufferSize = 1024;
            int retCount = -1;
            var bytes = new byte[_bufferSize];
            using (MemoryStream gzStream = await LocalMemoryStream.readAppFileAsync(testFilePath))
            using (MemoryStream strippedMs = StripHeaderAndFooter.Strip(gzStream))
                foreach (var iteration in Benchmark.Iterations)
                    using (iteration.StartMeasurement())
                        for (int i = 0; i < 10000; i++)
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
            File.Delete(testFilePath);
        }

        [Benchmark]
        [InlineData(CompressionType.CryptoRandom)]
        [InlineData(CompressionType.RepeatedSegments)]
        [InlineData(CompressionType.VeryRepetitive)]
        [InlineData(CompressionType.NormalData)]
        public void Compress(CompressionType type)
        {
            byte[] bytes = CreateBytesToCompress(type);
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
