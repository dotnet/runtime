// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Sinh(double) over 5000 iterations for the domain -1, +1

        private const double sinhDoubleDelta = 0.0004;
        private const double sinhDoubleExpectedResult = 1.17520119337903;

        [Benchmark(InnerIterationCount = SinhDoubleIterations)]
        public static void SinhDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SinhDoubleTest();
                    }
                }
            }
        }

        public static void SinhDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += sinhDoubleDelta;
                result += Math.Sinh(value);
            }

            var diff = Math.Abs(sinhDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {sinhDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
