// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Sin(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double sinDoubleDelta = 0.0006283185307180;
        private const double sinDoubleExpectedResult = 1.0000000005445053;

        [Benchmark(InnerIterationCount = SinDoubleIterations)]
        public static void SinDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SinDoubleTest();
                    }
                }
            }
        }

        public static void SinDoubleTest()
        {
            var result = 0.0; var value = -1.5707963267948966;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += sinDoubleDelta;
                result += Math.Sin(value);
            }

            var diff = Math.Abs(sinDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {sinDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
