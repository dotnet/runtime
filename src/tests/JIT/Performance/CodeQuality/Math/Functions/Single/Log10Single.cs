// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Log10(float) over 5000 iterations for the domain -1, +1

        private const float log10SingleDelta = 0.0004f;
        private const float log10SingleExpectedResult = -664.094971f;

        [Benchmark(InnerIterationCount = Log10SingleIterations)]
        public static void Log10SingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Log10SingleTest();
                    }
                }
            }
        }

        public static void Log10SingleTest()
        {
            var result = 0.0f; var value = 0.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += log10SingleDelta;
                result += MathF.Log10(value);
            }

            var diff = MathF.Abs(log10SingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {log10SingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
