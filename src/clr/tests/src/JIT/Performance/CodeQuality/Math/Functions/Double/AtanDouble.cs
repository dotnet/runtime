// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Atan(double) over 5000 iterations for the domain -1, +1

        private const double atanDoubleDelta = 0.0004;
        private const double atanDoubleExpectedResult = 0.78539816322061329;

        [Benchmark(InnerIterationCount = AtanDoubleIterations)]
        public static void AtanDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        AtanDoubleTest();
                    }
                }
            }
        }

        public static void AtanDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += atanDoubleDelta;
                result += Math.Atan(value);
            }

            var diff = Math.Abs(atanDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {atanDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
