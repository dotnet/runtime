// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Atan(float) over 5000 iterations for the domain -1, +1

        private const float atanSingleDelta = 0.0004f;
        private const float atanSingleExpectedResult = 0.841940999f;

        [Benchmark(InnerIterationCount = AtanSingleIterations)]
        public static void AtanSingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        AtanSingleTest();
                    }
                }
            }
        }

        public static void AtanSingleTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += atanSingleDelta;
                result += MathF.Atan(value);
            }

            var diff = MathF.Abs(atanSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {atanSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
