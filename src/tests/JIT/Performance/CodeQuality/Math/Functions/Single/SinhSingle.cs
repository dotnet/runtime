// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Sinh(float) over 5000 iterations for the domain -1, +1

        private const float sinhSingleDelta = 0.0004f;
        private const float sinhSingleExpectedResult = 1.26028216f;

        [Benchmark(InnerIterationCount = SinhSingleIterations)]
        public static void SinhSingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        SinhSingleTest();
                    }
                }
            }
        }

        public static void SinhSingleTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += sinhSingleDelta;
                result += MathF.Sinh(value);
            }

            var diff = MathF.Abs(sinhSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {sinhSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
