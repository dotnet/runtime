// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public class Perf_Single
    {
        private volatile string _string;

        [Benchmark]
        [InlineData(104234.343f, 1_000_000)]
        [InlineData(float.MaxValue, 100_000)]
        [InlineData(float.MinValue, 100_000)]
        [InlineData(float.MinValue / 2, 100_000)]
        [InlineData(float.NaN, 10_000_000)]
        [InlineData(float.PositiveInfinity, 10_000_000)]
        [InlineData(float.Epsilon, 100_000)]
        public void DefaultToString(float number, int innerIterations)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        _string = number.ToString();
                    }
                }
            }
        }

        [Benchmark]
        [InlineData("zh", 104234.343f, 1_000_000)]
        [InlineData("zh", float.MaxValue, 100_000)]
        [InlineData("zh", float.MinValue, 100_000)]
        [InlineData("zh", float.NaN, 20_000_000)]
        [InlineData("zh", float.PositiveInfinity, 20_000_000)]
        [InlineData("zh", 0.0, 4_000_000)]
        public void ToStringWithCultureInfo(string cultureName, float number, int innerIterations)
        {
            CultureInfo cultureInfo = new CultureInfo(cultureName);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        _string = number.ToString(cultureInfo);
                    }
                }
            }
        }

        public static IEnumerable<object[]> ToStringWithFormat_TestData()
        {
            string[] formats =
            {
                "R",
                "G",
                "G9",
                "E",
                "F50"
            };

            float[] normalTestValues =
            {
                0.0f,
                250.0f,
            };

            float[] edgeTestValues =
            {
                float.MaxValue,
                float.MinValue,
                float.Epsilon,
            };

            foreach (string format in formats)
            {
                foreach (float testValue in normalTestValues)
                {
                    yield return new object[] { format, testValue, 2_000_000 };
                }

                foreach (float testValue in edgeTestValues)
                {
                    yield return new object[] { format, testValue, 100_000 };
                }
            }

            yield return new object[] { "G", float.PositiveInfinity, 20_000_000 };
            yield return new object[] { "G", float.NaN, 20_000_000 };
        }

        [Benchmark]
        [MemberData(nameof(ToStringWithFormat_TestData))]
        public void ToStringWithFormat(string format, float number, int innerIterations)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        _string = number.ToString(format);
                    }
                }
            }
        }
    }
}
