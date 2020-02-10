// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Sqrt(double) over 5000 iterations for the domain 0, PI

        private const double sqrtDoubleDelta = 0.0006283185307180;
        private const double sqrtDoubleExpectedResult = 5909.0605337797215;

        [Benchmark(InnerIterationCount = SqrtDoubleIterations)]
        public static void SqrtDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SqrtDoubleTest();
                    }
                }
            }
        }

        public static void SqrtDoubleTest()
        {
            var result = 0.0; var value = 0.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += sqrtDoubleDelta;
                result += Math.Sqrt(value);
            }

            var diff = Math.Abs(sqrtDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {sqrtDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
