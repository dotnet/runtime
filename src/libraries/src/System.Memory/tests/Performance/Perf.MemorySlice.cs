// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    public class MemorySlice
    {
        private const int InnerCount = 1000;
        volatile static int volatileInt = 0;

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void MemorySliceThenGetSpan(int numberOfBytes)
        {
            Memory<byte> memory = new byte[numberOfBytes];
            int numberOfSlices = numberOfBytes / 10 - 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < numberOfSlices; j++)
                        {
                            var span = memory.Slice(10, 1).Span;
                            localInt ^= span[0];
                        }
                    }
                }
                volatileInt = localInt;
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1000)]
        [InlineData(1000 * 1000)]
        private static void MemoryGetSpanThenSlice(int numberOfBytes)
        {
            Memory<byte> memory = new byte[numberOfBytes];
            int numberOfSlices = numberOfBytes / 10 - 1;

            foreach (var iteration in Benchmark.Iterations)
            {
                int localInt = 0;
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (int j = 0; j < numberOfSlices; j++)
                        {
                            var span = memory.Span.Slice(10, 1);
                            localInt ^= span[0];
                        }
                    }
                }
                volatileInt = localInt;
            }
        }

    }
}
