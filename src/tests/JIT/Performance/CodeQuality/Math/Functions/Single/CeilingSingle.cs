// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Ceiling(float) over 5000 iterations for the domain -1, +1

        private const float ceilingSingleDelta = 0.0004f;
        private const float ceilingSingleExpectedResult = 2502.0f;

        [Benchmark(InnerIterationCount = CeilingSingleIterations)]
        public static void CeilingSingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        CeilingSingleTest();
                    }
                }
            }
        }

        public static void CeilingSingleTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += ceilingSingleDelta;
                result += MathF.Ceiling(value);
            }

            var diff = MathF.Abs(ceilingSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {ceilingSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
