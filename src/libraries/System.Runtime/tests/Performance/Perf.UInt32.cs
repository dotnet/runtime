// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public class Perf_UInt32
    {
        private const int InnerCount = 500_000;

        private static string s_resultString;
#if netcoreapp
        private static int s_resultInt32;
        private static uint s_resultUInt32;
#endif

        public static object[][] UInt32Values => new[]
        {
            new object[] { 0u },
            new object[] { 1u },
            new object[] { 1283u },
            new object[] { 12837467u },
            new object[] { 4294967295u },
        };

        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(UInt32Values))]
        public void ToString(uint value)
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

#if netcoreapp
        [Benchmark(InnerIterationCount = InnerCount)]
        [MemberData(nameof(UInt32Values))]
        public void TryFormat(uint value)
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
        [MemberData(nameof(UInt32Values))]
        public void Parse(uint value)
        {
            ReadOnlySpan<char> valueSpan = value.ToString();
            foreach (BenchmarkIteration iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < InnerCount; i++)
                    {
                        s_resultUInt32 = uint.Parse(valueSpan);
                    }
                }
            }
        }
#endif
    }
}
