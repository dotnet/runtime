// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public class Perf_Int64
    {
        private const int InnerCount = 500_000;

        private static string s_resultString;
        private static int s_resultInt32;
        private static long s_resultInt64;

        public static object[][] Int64Values => new[]
        {
            new object[] { 214748364L },
            new object[] { 2L },
            new object[] { 21474836L },
            new object[] { 21474L },
            new object[] { 214L },
            new object[] { 2147L },
            new object[] { 214748L },
            new object[] { 21L },
            new object[] { 2147483L },
            new object[] { 922337203685477580L },
            new object[] { 92233720368547758L },
            new object[] { 9223372036854775L },
            new object[] { 922337203685477L },
            new object[] { 92233720368547L },
            new object[] { 9223372036854L },
            new object[] { 922337203685L },
            new object[] { 92233720368L },
            new object[] { -214748364L },
            new object[] { -2L },
            new object[] { -21474836L },
            new object[] { -21474L },
            new object[] { -214L },
            new object[] { -2147L },
            new object[] { -214748L },
            new object[] { -21L },
            new object[] { -2147483L },
            new object[] { -922337203685477580L },
            new object[] { -92233720368547758L },
            new object[] { -9223372036854775L },
            new object[] { -922337203685477L },
            new object[] { -92233720368547L },
            new object[] { -9223372036854L },
            new object[] { -922337203685L },
            new object[] { -92233720368L },
            new object[] { 0L },
            new object[] { -9223372036854775808L }, // min value
            new object[] { 9223372036854775807L }, // max value
            new object[] { -2147483648L }, // int32 min value
            new object[] { 2147483647L }, // int32 max value
            new object[] { -4294967295000000000L }, // -(uint.MaxValue * Billion)
            new object[] { 4294967295000000000L }, // uint.MaxValue * Billion
            new object[] { -4294967295000000001L }, // -(uint.MaxValue * Billion + 1)
            new object[] { 4294967295000000001L }, // uint.MaxValue * Billion + 1
        };

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(Int64Values))]
        public void ToString(long value)
        {
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < InnerCount; i++)
                    {
                        s_resultString = value.ToString();
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(Int64Values))]
        public void TryFormat(long value)
        {
            Span<char> destination = new char[value.ToString().Length];
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < InnerCount; i++)
                    {
                        value.TryFormat(destination, out s_resultInt32);
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(Int64Values))]
        public void Parse(long value)
        {
            ReadOnlySpan<char> valueSpan = value.ToString();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < InnerCount; i++)
                    {
                        s_resultInt64 = long.Parse(valueSpan);
                    }
                }
            }
        }
    }
}
