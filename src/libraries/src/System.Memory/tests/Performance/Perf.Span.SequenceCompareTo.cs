// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    public class Perf_Span_SequenceCompareTo
    {
        private const int InnerCount = 100000;

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void SequenceCompareToSame_Byte(int size)
        {
            Span<byte> first = new byte[size];
            Span<byte> second = new byte[size];
            int result = -1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        result = first.SequenceCompareTo<byte>(second);
                    }
                }
            }

            Assert.Equal(0, result);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void SequenceCompareToDifferent_Byte(int size)
        {
            Span<byte> first = new byte[size];
            Span<byte> second = new byte[size];
            int result = -1;

            first[size/2] = 1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        result = first.SequenceCompareTo<byte>(second);
                    }
                }
            }

            Assert.Equal(1, result);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void SequenceCompareToSame_Int(int size)
        {
            Span<int> first = new int[size];
            Span<int> second = new int[size];
            int result = -1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        result = first.SequenceCompareTo<int>(second);
                    }
                }
            }

            Assert.Equal(0, result);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void SequenceCompareToDifferent_Int(int size)
        {
            Span<int> first = new int[size];
            Span<int> second = new int[size];
            int result = -1;

            first[size/2] = 1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        result = first.SequenceCompareTo<int>(second);
                    }
                }
            }

            Assert.Equal(1, result);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void SequenceCompareToSame_String(int size)
        {
            var firstStringArray = new string[size];
            var secondStringArray = new string[size];
            for (int i = 0; i < size; i++)
            {
                firstStringArray[i] = secondStringArray[i] = "0";
            }

            Span<string> first = firstStringArray;
            Span<string> second = secondStringArray;
            int result = -1;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        result = first.SequenceCompareTo(second);
                    }
                }
            }

            Assert.Equal(0, result);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void SequenceCompareToDifferent_String(int size)
        {
            var firstStringArray = new string[size];
            var secondStringArray = new string[size];
            for (int i = 0; i < size; i++)
            {
                firstStringArray[i] = secondStringArray[i] = "0";
            }

            Span<string> first = firstStringArray;
            Span<string> second = secondStringArray;
            int result = -1;

            first[size/2] = "1";

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        result = first.SequenceCompareTo(second);
                    }
                }
            }

            Assert.Equal(1, result);
        }
    }
}
