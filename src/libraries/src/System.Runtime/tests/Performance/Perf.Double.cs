// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Tests
{
    public class Perf_Double
    {
        // NOTE: Consider duplicating any tests added here in Perf.Single.cs

        private volatile string _string;
        private volatile bool _bool;

        // Reenable commented out test cases when https://github.com/xunit/xunit/issues/1822 is fixed.
        [Benchmark]
        [InlineData(double.NegativeInfinity, 10_000_000)]   // Negative Infinity
        [InlineData(double.MinValue, 100_000)]              // Min Negative Normal
        [InlineData(-3.14159265358979324, 1_000_000)]       // Negative pi
        [InlineData(-2.71828182845904524, 1_000_000)]       // Negative e
        [InlineData(-1.0, 1_000_000)]                       // Negative One
        // [InlineData(-2.2250738585072014E-308, 100_000)]     // Max Negative Normal
        [InlineData(-2.2250738585072009E-308, 100_000)]     // Min Negative Subnormal
        [InlineData(-double.Epsilon, 100_000)]              // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0, 10_000_000)]                      // Negative Zero
        [InlineData(double.NaN, 10_000_000)]                // NaN
        [InlineData(0.0, 10_000_000)]                       // Positive Zero
        [InlineData(double.Epsilon, 100_000)]               // Min Positive Subnormal (Positive Epsilon)
        [InlineData(2.2250738585072009E-308, 100_000)]      // Max Positive Subnormal
        // [InlineData(2.2250738585072014E-308, 100_000)]      // Min Positive Normal
        [InlineData(1.0, 1_000_000)]                        // Positive One
        [InlineData(2.71828182845904524, 1_000_000)]        // Positive e
        [InlineData(3.14159265358979324, 1_000_000)]        // Positive pi
        [InlineData(double.MaxValue, 100_000)]              // Max Positive Normal
        [InlineData(double.PositiveInfinity, 10_000_000)]   // Positive Infinity
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
        [InlineData("-∞", 10_000_000)]                      // Negative Infinity
        [InlineData("-1.7976931348623157E+308", 100_000)]   // Min Negative Normal
        [InlineData("-3.1415926535897931", 1_000_000)]      // Negative pi
        [InlineData("-2.7182818284590451", 1_000_000)]      // Negative e
        [InlineData("-1", 1_000_000)]                       // Negative One
        // [InlineData("-2.2250738585072014E-308", 100_000)]   // Max Negative Normal
        [InlineData("-2.2250738585072009E-308", 100_000)]   // Min Negative Subnormal
        [InlineData("-4.94065645841247E-324", 100_000)]     // Max Negative Subnormal (Negative Epsilon)
        [InlineData("-0.0", 10_000_000)]                    // Negative Zero
        [InlineData("NaN", 10_000_000)]                     // NaN
        [InlineData("0", 10_000_000)]                       // Positive Zero
        [InlineData("4.94065645841247E-324", 100_000)]      // Min Positive Subnormal (Positive Epsilon)
        [InlineData("2.2250738585072009E-308", 100_000)]    // Max Positive Subnormal
        // [InlineData("2.2250738585072014E-308", 100_000)]    // Min Positive Normal
        [InlineData("1", 1_000_000)]                        // Positive One
        [InlineData("2.7182818284590451", 1_000_000)]       // Positive e
        [InlineData("3.1415926535897931", 1_000_000)]       // Positive pi
        [InlineData("1.7976931348623157E+308", 100_000)]    // Max Positive Normal
        [InlineData("∞", 10_000_000)]                       // Positive Infinity
        public void DefaultTryParse(string input, int innerIterations)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < innerIterations; i++)
                    {
                        _bool = double.TryParse(input, out var result);
                    }
                }
            }
        }

        [Benchmark]
        [InlineData("zh", double.NegativeInfinity, 10_000_000)] // Negative Infinity
        [InlineData("zh", double.MinValue, 100_000)]            // Min Negative Normal
        [InlineData("zh", -3.14159265358979324, 1_000_000)]     // Negative pi
        [InlineData("zh", -2.71828182845904524, 1_000_000)]     // Negative e
        [InlineData("zh", -1.0, 1_000_000)]                     // Negative One
        // [InlineData("zh", -2.2250738585072014E-308, 100_000)]   // Max Negative Normal
        [InlineData("zh", -2.2250738585072009E-308, 100_000)]   // Min Negative Subnormal
        [InlineData("zh", -double.Epsilon, 100_000)]            // Max Negative Subnormal (Negative Epsilon)
        [InlineData("zh", -0.0, 10_000_000)]                    // Negative Zero
        [InlineData("zh", double.NaN, 10_000_000)]              // NaN
        [InlineData("zh", 0.0, 10_000_000)]                     // Positive Zero
        [InlineData("zh", double.Epsilon, 100_000)]             // Min Positive Subnormal (Positive Epsilon)
        [InlineData("zh", 2.2250738585072009E-308, 100_000)]    // Max Positive Subnormal
        // [InlineData("zh", 2.2250738585072014E-308, 100_000)]    // Min Positive Normal
        [InlineData("zh", 1.0, 1_000_000)]                      // Positive One
        [InlineData("zh", 2.71828182845904524, 1_000_000)]      // Positive e
        [InlineData("zh", 3.14159265358979324, 1_000_000)]      // Positive pi
        [InlineData("zh", double.MaxValue, 100_000)]            // Max Positive Normal
        [InlineData("zh", double.PositiveInfinity, 10_000_000)] // Positive Infinity
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
                "R",    // Roundtrip
                "E",    // Exponential
                "F",    // Fixed Point
            };

            double[] specialTestValues =    // 10_000_000 iterations
            {
                double.NegativeInfinity,    // Negative Infinity
                -0.0,                       // Negative Zero
                double.NaN,                 // NaN
                0.0,                        // Positive Zero
                double.PositiveInfinity,    // Positive Infinity
            };

            double[] normalTestValues =     // 1_000_000 iterations
            {
                -3.14159265358979324,       // Negative pi
                -2.71828182845904524,       // Negative e
                -1.0,                       // Negative One
                1.0,                        // Positive One
                2.71828182845904524,        // Positive e
                3.14159265358979324,        // Positive pi
            };

            double[] edgeTestValues =       // 100_000 iterations
            {
                double.MinValue,            // Min Negative Normal
                // -2.2250738585072014E-308,   // Max Negative Normal
                -2.2250738585072009E-308,   // Min Negative Subnormal
                -double.Epsilon,            // Max Negative Subnormal (Negative Epsilon)
                double.Epsilon,             // Min Positive Subnormal (Positive Epsilon)
                2.2250738585072009E-308,    // Max Positive Subnormal
                // 2.2250738585072014E-308,    // Min Positive Normal
                double.MaxValue,            // Max Positive Normal
            };

            foreach (string format in formats)
            {
                foreach (double testValue in specialTestValues)
                {
                    yield return new object[] { format, testValue, 10_000_000 };
                }

                foreach (double testValue in normalTestValues)
                {
                    yield return new object[] { format, testValue, 1_000_000 };
                }

                foreach (double testValue in edgeTestValues)
                {
                    yield return new object[] { format, testValue, 100_000 };
                }
            }
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
    }
}
