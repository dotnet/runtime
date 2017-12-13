// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    public class Perf_Span_BinarySearch
    {
        private const int InnerCount = 100000;
        private const string NumberFormat = "D9";

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_FirstIndex(int size)
        {
            BenchmarkAndAssert(size, 0, 0);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_MiddleIndex(int size)
        {
            BenchmarkAndAssert(size, size / 2, size / 2);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_LastIndex(int size)
        {
            BenchmarkAndAssert(size, size - 1, size - 1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_NotFoundBefore(int size)
        {
            BenchmarkAndAssert(size, -1, -1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_NotFoundAfter(int size)
        {
            BenchmarkAndAssert(size, size, ~size);
        }


        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_FirstIndex(int size)
        {
            BenchmarkAndAssert(size, 0.ToString(NumberFormat), 0);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_MiddleIndex(int size)
        {
            BenchmarkAndAssert(size, (size / 2).ToString(NumberFormat), size / 2);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_LastIndex(int size)
        {
            BenchmarkAndAssert(size, (size - 1).ToString(NumberFormat), size - 1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_NotFoundBefore(int size)
        {
            // "/" is just before zero in character table
            BenchmarkAndAssert(size, "/", -1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_NotFoundAfter(int size)
        {
            BenchmarkAndAssert(size, (size).ToString(NumberFormat), ~size);
        }

        private static void BenchmarkAndAssert(int size, int value, int expectedIndex)
        {
            Span<int> span = new int[size];
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i;
            }

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= span.BinarySearch(value);
                    }
                }
            }
            Assert.Equal(expectedIndex, index);
        }

        private static void BenchmarkAndAssert(int size, string value, int expectedIndex)
        {
            Span<string> span = new string[size];
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = i.ToString(NumberFormat);
            }

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= span.BinarySearch(value);
                    }
                }
            }
            Assert.Equal(expectedIndex, index);
        }
    }
}
