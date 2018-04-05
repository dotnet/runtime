// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public class Perf_Int32
    {
        private const int InnerCount = 500_000;

        private static string s_resultString;
        private static int s_resultInt32;

        public static object[][] Int32Values => new[]
        {
            new object[] { 0 },
            new object[] { -1 },
            new object[] { 1 },
            new object[] { -1283 },
            new object[] { 1283 },
            new object[] { -12837467 },
            new object[] { 12837467 },
            new object[] { -2147483648 },
            new object[] { 2147483647 },
        };

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(Int32Values))]
        public void ToString(int value)
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
        [MemberData(nameof(Int32Values))]
        public void TryFormat(int value)
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
        [MemberData(nameof(Int32Values))]
        public void Parse(int value)
        {
            ReadOnlySpan<char> valueSpan = value.ToString();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < InnerCount; i++)
                    {
                        s_resultInt32 = int.Parse(valueSpan);
                    }
                }
            }
        }
    }
}
