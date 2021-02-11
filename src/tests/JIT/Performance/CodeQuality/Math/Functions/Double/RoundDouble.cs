// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Round(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double roundDoubleDelta = 0.0006283185307180;
        private const double roundDoubleExpectedResult = 2;

        [Benchmark(InnerIterationCount = RoundDoubleIterations)]
        public static void RoundDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        RoundDoubleTest();
                    }
                }
            }
        }

        public static void RoundDoubleTest()
        {
            var result = 0.0; var value = -1.5707963267948966;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += roundDoubleDelta;
                result += Math.Round(value);
            }

            var diff = Math.Abs(roundDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {roundDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
