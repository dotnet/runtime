// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    public class MemorySlice
    {
        private const int InnerCount = 1000;
        volatile static int s_volatileInt = 0;

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Memory_Byte_SliceThenGetSpan(int numberOfBytes)
        {
            Memory<byte> memory = new byte[numberOfBytes];
            int numberOfSlices = numberOfBytes / 10 - 1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < numberOfSlices; j++)
                        {
                            Span<byte> span = memory.Slice(10, 1).Span;
                            localInt ^= span[0];
                        }
                    }
                }
                s_volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void Memory_Byte_GetSpanThenSlice(int numberOfBytes)
        {
            Memory<byte> memory = new byte[numberOfBytes];
            int numberOfSlices = numberOfBytes / 10 - 1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < numberOfSlices; j++)
                        {
                            Span<byte> span = memory.Span.Slice(10, 1);
                            localInt ^= span[0];
                        }
                    }
                }
                s_volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void ReadOnlyMemory_Byte_GetSpanThenSlice(int numberOfBytes)
        {
            ReadOnlyMemory<byte> memory = new byte[numberOfBytes];
            int numberOfSlices = numberOfBytes / 10 - 1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        for (int j = 0; j < numberOfSlices; j++)
                        {
                            ReadOnlySpan<byte> span = memory.Span.Slice(10, 1);
                            localInt ^= span[0];
                        }
                    }
                }
                s_volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void ReadOnlyMemory_Char_GetSpanThenSlice(int numberOfChars)
        {
            ReadOnlyMemory<char> memory = new char[numberOfChars];
            int numberOfSlices = numberOfChars / 10 - 1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        for (int j = 0; j < numberOfSlices; j++)
                        {
                            ReadOnlySpan<char> span = memory.Span.Slice(10, 1);
                            localInt ^= span[0];
                        }
                    }
                }
                s_volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void ReadOnlyMemory_Byte_TryGetArray()
        {
            ReadOnlyMemory<byte> memory = new byte[1];
            ArraySegment<byte> result;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        MemoryMarshal.TryGetArray(memory, out result);
                    }
                }
            }

            s_volatileInt = result.Count;
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        public static void ReadOnlyMemory_Char_TryGetArray()
        {
            ReadOnlyMemory<char> memory = new char[1];
            ArraySegment<char> result;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                long iters = Benchmark.InnerIterationCount;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < iters; i++)
                    {
                        MemoryMarshal.TryGetArray(memory, out result);
                    }
                }
            }

            s_volatileInt = result.Count;
        }
    }
}
