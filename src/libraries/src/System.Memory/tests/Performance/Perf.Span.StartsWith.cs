// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    public class Perf_Span_StartsWith
    {
        [Benchmark(InnerIterationCount = 10000)]
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(10000, 1)]
        [InlineData(10, 10)]
        [InlineData(100, 10)]
        [InlineData(1000, 10)]
        [InlineData(10000, 10)]
        [InlineData(100, 100)]
        [InlineData(1000, 100)]
        [InlineData(10000, 100)]
        [InlineData(1000, 1000)]
        [InlineData(10000, 1000)]
        [InlineData(10000, 10000)]
        public void Int(int size, int valSize)
        {
            var a = new int[size];
            for (int i = 0; i < size; i++)
            {
                int num = 65 + i % 26;
                a[i] = num;
            }

            var b = new int[valSize];
            for (int i = 0; i < valSize; i++)
            {
                int num = 65 + i % 26;
                b[i] = num;
            }
            var span = new Span<int>(a);
            var value = new Span<int>(b);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        bool result = span.StartsWith(value);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 1000000)]
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(10000, 1)]
        [InlineData(10, 10)]
        [InlineData(100, 10)]
        [InlineData(1000, 10)]
        [InlineData(10000, 10)]
        [InlineData(100, 100)]
        [InlineData(1000, 100)]
        [InlineData(10000, 100)]
        [InlineData(1000, 1000)]
        [InlineData(10000, 1000)]
        [InlineData(10000, 10000)]
        public void Byte(int size, int valSize)
        {
            var a = new byte[size];
            for (int i = 0; i < size; i++)
            {
                int num = 65 + i % 26;
                a[i] = (byte)num;
            }

            var b = new byte[valSize];
            for (int i = 0; i < valSize; i++)
            {
                int num = 65 + i % 26;
                b[i] = (byte)num;
            }
            var span = new Span<byte>(a);
            var value = new Span<byte>(b);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        bool result = span.StartsWith(value);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 10000)]
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(10000, 1)]
        [InlineData(10, 10)]
        [InlineData(100, 10)]
        [InlineData(1000, 10)]
        [InlineData(10000, 10)]
        [InlineData(100, 100)]
        [InlineData(1000, 100)]
        [InlineData(10000, 100)]
        [InlineData(1000, 1000)]
        [InlineData(10000, 1000)]
        [InlineData(10000, 10000)]
        public void String(int size, int valSize)
        {
            var a = new string[size];
            for (int i = 0; i < size; i++)
            {
                int num = 65 + i % 26;
                a[i] = ((char)num).ToString();
            }

            var b = new string[valSize];
            for (int i = 0; i < valSize; i++)
            {
                int num = 65 + i % 26;
                b[i] = ((char)num).ToString();
            }
            var span = new Span<string>(a);
            var value = new Span<string>(b);
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        bool result = span.StartsWith(value);
                    }
                }
            }
        }
    }
}
