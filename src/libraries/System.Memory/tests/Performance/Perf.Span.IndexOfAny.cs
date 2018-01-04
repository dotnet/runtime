// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    public class Perf_Span_IndexOfAny
    {
        private const int InnerCount = 100000;

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanIndexOfAnyChar_Two(int size)
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
                        index |= charSpan.IndexOfAny('5', 'a');
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
        public void SpanIndexOfAnyCharAsBytes_Two(int size)
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
                        index |= byteSpan.IndexOfAny<byte>(53, 54);        // '5' = 53
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
        public void SpanIndexOfAnyString_Two(int size)
        {
            string[] stringAray = new string[size];
            stringAray[size / 2] = "5";
            Span<string> stringSpan = new Span<string>(stringAray);

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= stringSpan.IndexOfAny("5", "a");
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
        public void SpanIndexOfAnyChar_Three(int size)
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
                        index |= charSpan.IndexOfAny('5', 'a', 'b');
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
        public void SpanIndexOfAnyCharAsBytes_Three(int size)
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
                        index |= byteSpan.IndexOfAny<byte>(53, 54, 55);        // '5' = 53
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
        public void SpanIndexOfAnyString_Three(int size)
        {
            string[] stringAray = new string[size];
            stringAray[size / 2] = "5";
            Span<string> stringSpan = new Span<string>(stringAray);

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= stringSpan.IndexOfAny("5", "a", "b");
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
        public void SpanIndexOfAnyChar_Many(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';
            ReadOnlySpan<char> values = new ReadOnlySpan<char>(new char[] { '5', 'a', 'b', 'c' });

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= charSpan.IndexOfAny(values);
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
        public void SpanIndexOfAnyCharAsBytes_Many(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';
            Span<byte> byteSpan = charSpan.AsBytes();
            ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { 53, 54, 55, 56 });        // '5' = 53

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= byteSpan.IndexOfAny(values);
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
        public void SpanIndexOfAnyCharAsBytes_NoSearchValue_Many(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';
            Span<byte> byteSpan = charSpan.AsBytes();
            ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { 54, 55, 56, 57 });        // '5' = 53

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= byteSpan.IndexOfAny(values);
                    }
                }
            }
            Assert.Equal(-1, index);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void SpanIndexOfAnyCharAsBytes_ContainsLastSearchValue_Many(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';
            Span<byte> byteSpan = charSpan.AsBytes();
            ReadOnlySpan<byte> values = new ReadOnlySpan<byte>(new byte[] { 54, 55, 56, 53 });        // '5' = 53

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= byteSpan.IndexOfAny(values);
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
        public void SpanIndexOfAnyString_Many(int size)
        {
            string[] stringAray = new string[size];
            stringAray[size / 2] = "5";
            Span<string> stringSpan = new Span<string>(stringAray);
            ReadOnlySpan<string> values = new ReadOnlySpan<string>(new string[] { "5", "a", "b", "c" });

            int index = 0;
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        index |= stringSpan.IndexOfAny(values);
                    }
                }
            }
            Assert.Equal(size / 2, index);
        }
    }
}
