// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Exp(float) over 5000 iterations for the domain -1, +1

        private const float expSingleDelta = 0.0004f;
        private const float expSingleExpectedResult = 5877.28564f;

        [Benchmark(InnerIterationCount = ExpSingleIterations)]
        public static void ExpSingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        ExpSingleTest();
                    }
                }
            }
        }

        public static void ExpSingleTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += expSingleDelta;
                result += MathF.Exp(value);
            }

            var diff = MathF.Abs(expSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {expSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
