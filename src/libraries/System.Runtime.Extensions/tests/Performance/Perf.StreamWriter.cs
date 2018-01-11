// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.IO.Tests
{
    public class Perf_StreamWriter
    {
        private const int MemoryStreamSize = 32768;
        private const int TotalWriteCount = 16777216; // 2^24 - should yield around 300ms runs
        private const int DefaultStreamWriterBufferSize = 1024; // Same as StreamWriter internal default

        [Benchmark]
        [MemberData(nameof(WriteLengthMemberData))]
        public void WriteCharArray(int writeLength)
        {
            char[] buffer = new string('a', writeLength).ToCharArray();
            int innerIterations = MemoryStreamSize / writeLength;
            int outerIteration = TotalWriteCount / innerIterations;
            using (var stream = new MemoryStream(MemoryStreamSize))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false, true), DefaultStreamWriterBufferSize, true))
                {
                    foreach (var iteration in Benchmark.Iterations)
                    {
                        using (iteration.StartMeasurement())
                        {
                            for (int i = 0; i < outerIteration; i++)
                            {
                                for (int j = 0; j < innerIterations; j++)
                                {
                                    writer.Write(buffer);
                                }
                                writer.Flush();
                                stream.Position = 0;
                            }
                        }
                    }
                }
            }
        }

        [Benchmark]
        [MemberData(nameof(WriteLengthMemberData))]
        public void WritePartialCharArray(int writeLength)
        {
            char[] buffer = new string('a', writeLength + 10).ToCharArray();
            int innerIterations = MemoryStreamSize / writeLength;
            int outerIteration = TotalWriteCount / innerIterations;
            using (var stream = new MemoryStream(MemoryStreamSize))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false, true), DefaultStreamWriterBufferSize, true))
                {
                    foreach (var iteration in Benchmark.Iterations)
                    {
                        using (iteration.StartMeasurement())
                        {
                            for (int i = 0; i < outerIteration; i++)
                            {
                                for (int j = 0; j < innerIterations; j++)
                                {
                                    writer.Write(buffer, 10, writeLength);
                                }
                                writer.Flush();
                                stream.Position = 0;
                            }
                        }
                    }
                }
            }
        }

        [Benchmark]
        [MemberData(nameof(WriteLengthMemberData))]
        public void WriteString(int writeLength)
        {
            string value = new string('a', writeLength);
            int innerIterations = MemoryStreamSize / writeLength;
            int outerIteration = TotalWriteCount / innerIterations;
            using (var stream = new MemoryStream(MemoryStreamSize))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false, true), DefaultStreamWriterBufferSize, true))
                {
                    foreach (var iteration in Benchmark.Iterations)
                    {
                        using (iteration.StartMeasurement())
                        {
                            for (int i = 0; i < outerIteration; i++)
                            {
                                for (int j = 0; j < innerIterations; j++)
                                {
                                    writer.Write(value);
                                }
                                writer.Flush();
                                stream.Position = 0;
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<object[]> WriteLengthMemberData()
        {
            yield return new object[] { 2 };
            yield return new object[] { 100 };
        }
    }
}
