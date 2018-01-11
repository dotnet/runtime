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
        public void ArrayBinarySearch_Int_FirstIndex(int size)
        {
            BenchmarkAndAssertArray(size, 0, 0);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_Int_MiddleIndex(int size)
        {
            BenchmarkAndAssertArray(size, (size - 1) / 2, (size - 1) / 2);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_Int_LastIndex(int size)
        {
            BenchmarkAndAssertArray(size, size - 1, size - 1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_Int_NotFoundBefore(int size)
        {
            BenchmarkAndAssertArray(size, -1, -1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_Int_NotFoundAfter(int size)
        {
            BenchmarkAndAssertArray(size, size, ~size);
        }


        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_String_FirstIndex(int size)
        {
            BenchmarkAndAssertArray(size, 0.ToString(NumberFormat), 0);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_String_MiddleIndex(int size)
        {
            BenchmarkAndAssertArray(size, ((size - 1) / 2).ToString(NumberFormat), (size - 1) / 2);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_String_LastIndex(int size)
        {
            BenchmarkAndAssertArray(size, (size - 1).ToString(NumberFormat), size - 1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_String_NotFoundBefore(int size)
        {
            // "/" is just before zero in character table
            BenchmarkAndAssertArray(size, "/", -1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void ArrayBinarySearch_String_NotFoundAfter(int size)
        {
            BenchmarkAndAssertArray(size, (size).ToString(NumberFormat), ~size);
        }


        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_FirstIndex(int size)
        {
            BenchmarkAndAssertSpan(size, 0, 0);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_MiddleIndex(int size)
        {
            BenchmarkAndAssertSpan(size, (size - 1) / 2, (size - 1) / 2);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_LastIndex(int size)
        {
            BenchmarkAndAssertSpan(size, size - 1, size - 1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_NotFoundBefore(int size)
        {
            BenchmarkAndAssertSpan(size, -1, -1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_Int_NotFoundAfter(int size)
        {
            BenchmarkAndAssertSpan(size, size, ~size);
        }


        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_FirstIndex(int size)
        {
            BenchmarkAndAssertSpan(size, 0.ToString(NumberFormat), 0);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_MiddleIndex(int size)
        {
            BenchmarkAndAssertSpan(size, ((size - 1) / 2).ToString(NumberFormat), (size - 1) / 2);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_LastIndex(int size)
        {
            BenchmarkAndAssertSpan(size, (size - 1).ToString(NumberFormat), size - 1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_NotFoundBefore(int size)
        {
            // "/" is just before zero in character table
            BenchmarkAndAssertSpan(size, "/", -1);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanBinarySearch_String_NotFoundAfter(int size)
        {
            BenchmarkAndAssertSpan(size, (size).ToString(NumberFormat), ~size);
        }

        private static void BenchmarkAndAssertArray(int size, int value, int expectedIndex)
        {
            BenchmarkAndAssertArray(size, i => i, value, expectedIndex);
        }

        private static void BenchmarkAndAssertArray(int size, string value, int expectedIndex)
        {
            BenchmarkAndAssertArray(size, i => i.ToString(NumberFormat), value, expectedIndex);
        }

        private static void BenchmarkAndAssertArray<T>(int size, Func<int, T> toValue, T value, int expectedIndex)
            where T : IComparable<T>
        {
            var array = new T[size];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = toValue(i);
            }

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= Array.BinarySearch(array, value);
                    }
                }
            }
            Assert.Equal(expectedIndex, index);
        }

        private static void BenchmarkAndAssertSpan(int size, int value, int expectedIndex)
        {
            BenchmarkAndAssertSpan(size, i => i, value, expectedIndex);
        }

        private static void BenchmarkAndAssertSpan(int size, string value, int expectedIndex)
        {
            BenchmarkAndAssertSpan(size, i => i.ToString(NumberFormat), value, expectedIndex);
        }

        private static void BenchmarkAndAssertSpan<T>(int size, Func<int, T> toValue, T value, int expectedIndex)
            where T : IComparable<T>
        {
            Span<T> span = new T[size];
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = toValue(i);
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
