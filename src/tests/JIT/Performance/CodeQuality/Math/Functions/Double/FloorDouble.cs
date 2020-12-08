// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Floor(double) over 5000 iterations for the domain -1, +1

        private const double floorDoubleDelta = 0.0004;
        private const double floorDoubleExpectedResult = -2500;

        [Benchmark(InnerIterationCount = FloorDoubleIterations)]
        public static void FloorDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        FloorDoubleTest();
                    }
                }
            }
        }

        public static void FloorDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += floorDoubleDelta;
                result += Math.Floor(value);
            }

            var diff = Math.Abs(floorDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {floorDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
