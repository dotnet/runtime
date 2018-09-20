// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Memory.Tests
{
    // Adapted from Perf.Span.IndexOf.cs
    public class Perf_Span_Contains
    {
        private const int InnerCount = 50_000;

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(2000)]
        public void SpanContainsChar(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';

            bool found = true;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (!charSpan.Contains('5'))
                            found = false;
                    }
                }
            }

            Assert.True(found);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(2000)]
        public void SpanIndexOfChar(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';

            bool found = true;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (charSpan.IndexOf('5') < 0)
                            found = false;
                    }
                }
            }

            Assert.True(found);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(2000)]
        public void SpanContainsCharAsBytes(int size)
        {
            Span<char> charSpan = new char[size];
            charSpan[size / 2] = '5';
            Span<byte> byteSpan = MemoryMarshal.AsBytes(charSpan);

            bool found = true;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (!charSpan.Contains('5')) // '5' = 53
                            found = false;
                    }
                }
            }

            Assert.True(found);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(2000)]
        public void StringContainsChar(int size)
        {
            string str = new string('0', size / 2) + "5";
            if (size > 1)
            {
                str += new string('0', size / 2 - 1);
            }

            bool found = true;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (!str.Contains('5'))
                            found = false;
                    }
                }
            }

            Assert.True(found);
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(2000)]
        public void StringIndexOfChar(int size)
        {
            string str = new string('0', size / 2) + "5";
            if (size > 1)
            {
                str += new string('0', size / 2 - 1);
            }

            bool found = true;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (str.IndexOf('5') < 0)
                            found = false;
                    }
                }
            }

            Assert.True(found);
        }

        private static string GenerateInputString(char source, int count, char replaceChar, int replacePos)
        {
            char[] str = new char[count];
            for (int i = 0; i < count; i++)
            {
                str[i] = replaceChar;
            }
            str[replacePos] = replaceChar;

            return new string(str);
        }

        public static IEnumerable<object[]> s_indexTestData = new List<object[]>
        {
            new object[] { "string1", '1' },
            new object[] { "foobardzsdzs", 'z' },
            new object[] { "StrIng", 'I' },
            new object[] { "\u3060", '\u305F' },
            new object[] { "ABCDE", 'c' },
            new object[] { "More Test's", '\'' },
            new object[] { "Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz", '~' },
            new object[] { "Hello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello WorldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylongHello Worldbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbareallyreallylong!xyz", 'w' },
            new object[] { "Hello Worldbbbbbbbbbbbbbbcbbbbbbbbbbbbbbbbbbba!", 'y' },
            new object[] { GenerateInputString('A', 10, '5', 5), '5' },
            new object[] { GenerateInputString('A', 100, 'X', 70), 'x' },
            new object[] { GenerateInputString('A', 1000, 'G', 500), 'G' },
            new object[] { GenerateInputString('\u3060', 1000, 'x', 500), 'x' },
            new object[] { GenerateInputString('\u3060', 100, '\u3059', 50), '\u3059' }
        };

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(s_indexTestData))]
        public ulong ContainsChar_StringAsSpan(string input, char value)
        {
            var count = 0UL;

            ReadOnlySpan<char> inputSpan = input.AsSpan();

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (inputSpan.Contains(value))
                            count++;
                    }
                }
            }

            return count;
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(s_indexTestData))]
        public ulong ContainsChar_String(string input, char value)
        {
            var count = 0UL;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (input.Contains(value))
                            count++;
                    }
                }
            }

            return count;
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(s_indexTestData))]
        public ulong ContainsChar_StringLinq(string input, char value)
        {
            var count = 0UL;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (Linq.Enumerable.Contains(input, value))
                            count++;
                    }
                }
            }

            return count;
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(s_indexTestData))]
        public ulong ContainsChar_StringIndexOf(string input, char value)
        {
            var count = 0UL;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        if (input.IndexOf(value) >= 0)
                            count++;
                    }
                }
            }

            return count;
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(s_indexTestData))]
        public ulong ContainsChar_Baseline(string input, char value)
        {
            var count = 0UL;

            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        for (var j = 0; j < input.Length; j++)
                        {
                            if (input[j] == value)
                            {
                                count++;
                                break;
                            }
                        }
                    }
                }
            }

            return count;
        }
    }
}
