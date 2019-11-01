// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Tanh(float) over 5000 iterations for the domain -1, +1

        private const float tanhSingleDelta = 0.0004f;
        private const float tanhSingleExpectedResult = 0.816701353f;

        [Benchmark(InnerIterationCount = TanhSingleIterations)]
        public static void TanhSingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        TanhSingleTest();
                    }
                }
            }
        }

        public static void TanhSingleTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += tanhSingleDelta;
                result += MathF.Tanh(value);
            }

            var diff = MathF.Abs(tanhSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {tanhSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
