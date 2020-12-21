// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Ceiling(double) over 5000 iterations for the domain -1, +1

        private const double ceilingDoubleDelta = 0.0004;
        private const double ceilingDoubleExpectedResult = 2500;

        [Benchmark(InnerIterationCount = CeilingDoubleIterations)]
        public static void CeilingDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        CeilingDoubleTest();
                    }
                }
            }
        }

        public static void CeilingDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += ceilingDoubleDelta;
                result += Math.Ceiling(value);
            }

            var diff = Math.Abs(ceilingDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {ceilingDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
