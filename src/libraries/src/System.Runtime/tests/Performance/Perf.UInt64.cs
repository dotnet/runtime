// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public class Perf_UInt64
    {
        private const int InnerCount = 100_000;

        private static string s_resultString;
        private static int s_resultInt32;
        private static ulong s_resultUInt64;

        public static object[][] UInt64Values => new[]
        {
            new object[] { 214748364LU },
            new object[] { 2LU },
            new object[] { 21474836LU },
            new object[] { 21474LU },
            new object[] { 214LU },
            new object[] { 2147LU },
            new object[] { 214748LU },
            new object[] { 21LU },
            new object[] { 2147483LU },
            new object[] { 922337203685477580LU },
            new object[] { 92233720368547758LU },
            new object[] { 9223372036854775LU },
            new object[] { 922337203685477LU },
            new object[] { 92233720368547LU },
            new object[] { 9223372036854LU },
            new object[] { 922337203685LU },
            new object[] { 92233720368LU },
            new object[] { 0LU }, // min value
            new object[] { 18446744073709551615LU }, // max value
            new object[] { 2147483647LU }, // int32 max value
            new object[] { 9223372036854775807LU }, // int64 max value
            new object[] { 1000000000000000000LU }, // quintillion
            new object[] { 4294967295000000000LU }, // uint.MaxValue * Billion
            new object[] { 4294967295000000001LU }, // uint.MaxValue * Billion + 1
        };

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(UInt64Values))]
        public void ToString(ulong value)
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
        [MemberData(nameof(UInt64Values))]
        public void TryFormat(ulong value)
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
        [MemberData(nameof(UInt64Values))]
        public void Parse(ulong value)
        {
            ReadOnlySpan<char> valueSpan = value.ToString();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < InnerCount; i++)
                    {
                        s_resultUInt64 = ulong.Parse(valueSpan);
                    }
                }
            }
        }
    }
}
