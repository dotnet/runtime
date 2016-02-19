// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace System.IO.Tests
{
    public class Perf_FileStream : FileSystemTest
    {

        private static byte[] CreateBytesToWrite(int size)
        {
            byte[] bytes = new byte[size];
            new Random(531033).NextBytes(bytes);
            return bytes;
        }

        private string CreateFile(int size)
        {
            string filePath = GetTestFilePath();
            byte[] bytes = new byte[size];
            new Random(531033).NextBytes(bytes);
            File.WriteAllBytes(filePath, bytes);
            return filePath;
        }

        /// <summary>
        /// Yields test data for Read and Write operations. Specifies three parameters:
        /// 
        /// useAsync: Whether the IO should be done asynchronously
        /// bufferSize:The size of an individual call. The total number of calls will be equivalent to totalSize / readSize
        /// totalSize: The total number of bytes to read from the file
        /// </summary>
        public static IEnumerable<object[]> ReadWriteTestParameters()
        {
            // A bufferSize of -1 indicates that the bufferSize should be equal to the totalSize, whatever it may be.
            const int DefaultBuffer = 4096;
            int[] bufferSizes = new int[] { DefaultBuffer / 8, -1, 1};
            int[] totalSizes = new int[] { 200000, 10000000 };

            foreach (int bufferSize in bufferSizes)
                foreach (int totalSize in totalSizes)
                    foreach (bool useAsync in new[] { true, false })
                        if (!(useAsync && bufferSize == 1)) //no Async versions of ReadByte/WriteByte
                            yield return new object[] { useAsync, bufferSize == -1 ? totalSize : bufferSize, totalSize };
        }

        /// <summary>
        /// Test for reading from a file using a FileStream. Makes use of differently sized reads to test the performance
        /// impact of using the internal buffer.
        /// 
        /// If the bufferSize == 1, calls ReadByte instead of the traditional Read.
        /// </summary>
        [Benchmark]
        [MemberData(nameof(ReadWriteTestParameters))]
        public async Task Read(bool useAsync, int bufferSize, int totalSize)
        {
            byte[] bytes = new byte[bufferSize];
            // Actual file size may be slightly over the desired size due to rounding if totalSize % readSize != 0
            int innerIterations = totalSize / bufferSize;
            string filePath = CreateFile(innerIterations * bufferSize);
            foreach (var iteration in Benchmark.Iterations)
            {
                if (useAsync)
                {
                    using (FileStream reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                    using (iteration.StartMeasurement())
                        await ReadAsyncCore(reader, innerIterations, bytes);
                }
                else
                {
                    using (FileStream reader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.None))
                    using (iteration.StartMeasurement())
                    {
                        if (bufferSize == 1)
                            for (int i = 0; i < totalSize; i++)
                                reader.ReadByte();
                        else
                            for (int i = 0; i < innerIterations; i++)
                                reader.Read(bytes, 0, bytes.Length);
                    }
                }
            }
            File.Delete(filePath);
        }

        /// <summary>
        /// Test for writing to a file using a FileStream. Makes use of differently sized writes to test the performance
        /// impact of using the internal buffer.
        /// 
        /// If the bufferSize == 1, calls WriteByte instead of the traditional Write.
        /// </summary>
        [Benchmark]
        [MemberData(nameof(ReadWriteTestParameters))]
        public async Task Write(bool useAsync, int bufferSize, int totalSize)
        {
            byte[] bytes = CreateBytesToWrite(bufferSize);
            // Actual file size may be slightly over the desired size due to rounding if totalSize % bufferSize != 0
            int innerIterations = totalSize / bufferSize;
            string filePath = GetTestFilePath();
            foreach (var iteration in Benchmark.Iterations)
            {
                if (useAsync)
                {
                    using (FileStream writer = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous))
                    using (iteration.StartMeasurement())
                        await WriteAsyncCore(writer, innerIterations, bytes);
                }
                else
                {
                    using (FileStream writer = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.None))
                    using (iteration.StartMeasurement())
                    {
                        if (bufferSize == 1)
                            for (int i = 0; i < totalSize; i++)
                                writer.WriteByte(bytes[0]);
                        else
                            for (int i = 0; i < innerIterations; i++)
                                writer.Write(bytes, 0, bytes.Length);
                    }
                }
                File.Delete(filePath);
            }
        }

        private static async Task ReadAsyncCore(FileStream reader, int innerIterations, byte[] bytes)
        {
            for (int i = 0; i < innerIterations; i++)
                await reader.ReadAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        private static async Task WriteAsyncCore(FileStream writer, int innerIterations, byte[] bytes)
        {
            for (int i = 0; i < innerIterations; i++)
                await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }
    }
}
