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
        // NOTE: Consider duplicating any tests added here in Perf.Double.cs

        private volatile string _string;
        private volatile bool _bool;

        // Reenable commented out test cases when https://github.com/xunit/xunit/issues/1822 is fixed.
        [Benchmark]
        [InlineData(float.NegativeInfinity, 10_000_000)]    // Negative Infinity
        [InlineData(float.MinValue, 100_000)]               // Min Negative Normal
        [InlineData(-3.14159265f, 1_000_000)]               // Negative pi
        [InlineData(-2.71828183f, 1_000_000)]               // Negative e
        [InlineData(-1.0f, 1_000_000)]                      // Negative One
        // [InlineData(-1.17549435E-38f, 100_000)]             // Max Negative Normal
        [InlineData(-1.17549421E-38f, 100_000)]             // Min Negative Subnormal
        [InlineData(-float.Epsilon, 100_000)]               // Max Negative Subnormal (Negative Epsilon)
        [InlineData(-0.0f, 10_000_000)]                     // Negative Zero
        [InlineData(float.NaN, 10_000_000)]                 // NaN
        [InlineData(0.0f, 10_000_000)]                      // Positive Zero
        [InlineData(float.Epsilon, 100_000)]                // Min Positive Subnormal (Positive Epsilon)
        [InlineData(1.17549421E-38f, 100_000)]              // Max Positive Subnormal
        // [InlineData(1.17549435E-38f, 100_000)]              // Min Positive Normal
        [InlineData(1.0f, 1_000_000)]                       // Positive One
        [InlineData(2.71828183f, 1_000_000)]                // Positive e
        [InlineData(3.14159265f, 1_000_000)]                // Positive pi
        [InlineData(float.MaxValue, 100_000)]               // Max Positive Normal
        [InlineData(float.PositiveInfinity, 10_000_000)]    // Positive Infinity
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
        [InlineData("-∞", 10_000_000)]                      // Negative Infinity
        [InlineData("-3.40282347E+38", 100_000)]            // Min Negative Normal
        [InlineData("-3.14159274", 1_000_000)]              // Negative pi
        [InlineData("-2.71828175", 1_000_000)]              // Negative e
        [InlineData("-1", 1_000_000)]                       // Negative One
        // [InlineData("-1.17549435E-38", 100_000)]            // Max Negative Normal
        [InlineData("-1.17549421E-38", 100_000)]            // Min Negative Subnormal
        [InlineData("-1.401298E-45", 100_000)]              // Max Negative Subnormal (Negative Epsilon)
        [InlineData("-0.0", 10_000_000)]                    // Negative Zero
        [InlineData("NaN", 10_000_000)]                     // NaN
        [InlineData("0", 10_000_000)]                       // Positive Zero
        [InlineData("1.401298E-45", 100_000)]               // Min Positive Subnormal (Positive Epsilon)
        [InlineData("1.17549421E-38", 100_000)]             // Max Positive Subnormal
        // [InlineData("1.17549435E-38", 100_000)]             // Min Positive Normal
        [InlineData("1", 1_000_000)]                        // Positive One
        [InlineData("2.71828175", 1_000_000)]               // Positive e
        [InlineData("3.14159274", 1_000_000)]               // Positive pi
        [InlineData("3.40282347E+38", 100_000)]             // Max Positive Normal
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
        [InlineData("zh", float.NegativeInfinity, 10_000_000)]  // Negative Infinity
        [InlineData("zh", float.MinValue, 100_000)]             // Min Negative Normal
        [InlineData("zh", -3.14159265f, 1_000_000)]             // Negative pi
        [InlineData("zh", -2.71828183f, 1_000_000)]             // Negative e
        [InlineData("zh", -1.0f, 1_000_000)]                    // Negative One
        // [InlineData("zh", -1.17549435E-38f, 100_000)]           // Max Negative Normal
        [InlineData("zh", -1.17549421E-38f, 100_000)]           // Min Negative Subnormal
        [InlineData("zh", -float.Epsilon, 100_000)]             // Max Negative Subnormal (Negative Epsilon)
        [InlineData("zh", -0.0f, 10_000_000)]                   // Negative Zero
        [InlineData("zh", float.NaN, 10_000_000)]               // NaN
        [InlineData("zh", 0.0f, 10_000_000)]                    // Positive Zero
        [InlineData("zh", float.Epsilon, 100_000)]              // Min Positive Subnormal (Positive Epsilon)
        [InlineData("zh", 1.17549421E-38f, 100_000)]            // Max Positive Subnormal
        // [InlineData("zh", 1.17549435E-38f, 100_000)]            // Min Positive Normal
        [InlineData("zh", 1.0f, 1_000_000)]                     // Positive One
        [InlineData("zh", 2.71828183f, 1_000_000)]              // Positive e
        [InlineData("zh", 3.14159265f, 1_000_000)]              // Positive pi
        [InlineData("zh", float.MaxValue, 100_000)]             // Max Positive Normal
        [InlineData("zh", float.PositiveInfinity, 10_000_000)]  // Positive Infinity
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
                "R",    // Roundtrip
                "E",    // Exponential
                "F",    // Fixed Point
            };

            float[] specialTestValues =     // 10_000_000 iterations
            {
                float.NegativeInfinity,     // Negative Infinity
                -0.0f,                      // Negative Zero
                float.NaN,                  // NaN
                0.0f,                       // Positive Zero
                float.PositiveInfinity,     // Positive Infinity
            };

            float[] normalTestValues =      // 1_000_000 iterations
            {
                -3.14159265f,               // Negative pi
                -2.71828183f,               // Negative e
                -1.0f,                      // Negative One
                1.0f,                       // Positive One
                2.71828183f,                // Positive e
                3.14159265f,                // Positive pi
            };

            float[] edgeTestValues =        // 100_000 iterations
            {
                float.MinValue,             // Min Negative Normal
                // -1.17549435E-38f,           // Max Negative Normal
                -1.17549421E-38f,           // Min Negative Subnormal
                -float.Epsilon,             // Max Negative Subnormal (Negative Epsilon)
                float.Epsilon,              // Min Positive Subnormal (Positive Epsilon)
                1.17549421E-38f,            // Max Positive Subnormal
                // 1.17549435E-38f,            // Min Positive Normal
                float.MaxValue,             // Max Positive Normal
            };

            foreach (string format in formats)
            {
                foreach (float testValue in specialTestValues)
                {
                    yield return new object[] { format, testValue, 10_000_000 };
                }

                foreach (float testValue in normalTestValues)
                {
                    yield return new object[] { format, testValue, 1_000_000 };
                }

                foreach (float testValue in edgeTestValues)
                {
                    yield return new object[] { format, testValue, 100_000 };
                }
            }
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
