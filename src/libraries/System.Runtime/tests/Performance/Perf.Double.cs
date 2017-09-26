// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public class Perf_Double
    {
        private volatile string _string;

        [Benchmark]
        [InlineData(104234.343, 1_000_000)]
        [InlineData(double.MaxValue, 100_000)]
        [InlineData(double.MinValue, 100_000)]
        [InlineData(double.MinValue / 2, 100_000)]
        [InlineData(double.NaN, 10_000_000)]
        [InlineData(double.PositiveInfinity, 10_000_000)]
        [InlineData(2.2250738585072009E-308, 100_000)]
        public void DefaultToString(double number, int innerIterations)
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
        [InlineData("zh", 104234.343, 1_000_000)]
        [InlineData("zh", double.MaxValue, 100_000)]
        [InlineData("zh", double.MinValue, 100_000)]
        [InlineData("zh", double.NaN, 20_000_000)]
        [InlineData("zh", double.PositiveInfinity, 20_000_000)]
        [InlineData("zh", 0.0, 4_000_000)]
        public void ToStringWithCultureInfo(string cultureName, double number, int innerIterations)
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
                "G17",
                "E",
                "F50"
            };

            double[] normalTestValues =
            {
                0.0,
                250.0,
            };

            double[] edgeTestValues =
            {
                double.MaxValue,
                double.MinValue,
                double.Epsilon,
            };

            foreach (string format in formats)
            {
                foreach (double testValue in normalTestValues)
                {
                    yield return new object[] { format, testValue, 2_000_000 };
                }

                foreach (double testValue in edgeTestValues)
                {
                    yield return new object[] { format, testValue, 100_000 };
                }
            }

            yield return new object[] { "G", double.PositiveInfinity, 20_000_000 };
            yield return new object[] { "G", double.NaN, 20_000_000 };
        }

        [Benchmark]
        [MemberData(nameof(ToStringWithFormat_TestData))]
        public void ToStringWithFormat(string format, double number, int innerIterations)
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

        [Benchmark(InnerIterationCount = 4_000_000)]
        public static void Decimal_ToString()
        {
            decimal decimalNum = new decimal(1.23456789E+5);
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        decimalNum.ToString();
                    }
                }
            }
        }
    }
}
