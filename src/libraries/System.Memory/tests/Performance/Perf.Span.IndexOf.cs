// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    public class Perf_Span_IndexOf
    {
        private const int InnerCount = 100000;

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanIndexOfChar(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= charSpan.IndexOf('5');
                    }
                }
            }
            Assert.Equal(size / 2, index);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanIndexOfCharAsBytes(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';
            Span<byte> byteSpan = charSpan.AsBytes();

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= byteSpan.IndexOf<byte>(53);        // '5' = 53
                    }
                }
            }
            Assert.Equal(size > 1 ? size : 0, index);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void StringIndexOfChar(int size)
        {
            string str = new string('0', size / 2) + "5";
            if (size > 1)
            {
                str += new string('0', size / 2 - 1);
            }

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= str.IndexOf('5');
                    }
                }
            }
            Assert.Equal(size / 2, index);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanLastIndexOfChar(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= charSpan.LastIndexOf('5');
                    }
                }
            }
            Assert.Equal(size / 2, index);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanLastIndexOfCharAsBytes(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';
            Span<byte> byteSpan = charSpan.AsBytes();

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= byteSpan.LastIndexOf<byte>(53);        // '5' = 53
                    }
                }
            }
            Assert.Equal(size > 1 ? size : 0, index);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void StringLastIndexOfChar(int size)
        {
            string str = new string('0', size / 2) + "5";
            if (size > 1)
            {
                str += new string('0', size / 2 - 1);
            }

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= str.LastIndexOf('5');
                    }
                }
            }
            Assert.Equal(size / 2, index);
        }
    }
}
