// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Atan2(double, double) over 5000 iterations for the domain y: -1, +1; x: +1, -1

        private const double atan2DoubleDeltaX = -0.0004;
        private const double atan2DoubleDeltaY = 0.0004;
        private const double atan2DoubleExpectedResult = 3926.99081698702;

        [Benchmark(InnerIterationCount = Atan2DoubleIterations)]
        public static void Atan2DoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Atan2DoubleTest();
                    }
                }
            }
        }

        public static void Atan2DoubleTest()
        {
            var result = 0.0; var valueX = 1.0; var valueY = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                valueX += atan2DoubleDeltaX; valueY += atan2DoubleDeltaY;
                result += Math.Atan2(valueY, valueX);
            }

            var diff = Math.Abs(atan2DoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {atan2DoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
