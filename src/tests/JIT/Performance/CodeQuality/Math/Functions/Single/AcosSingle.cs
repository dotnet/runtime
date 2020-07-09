// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Acos(float) over 5000 iterations for the domain -1, +1

        private const float acosSingleDelta = 0.0004f;
        private const float acosSingleExpectedResult = 7852.41084f;

        [Benchmark(InnerIterationCount = AcosSingleIterations)]
        public static void AcosSingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        AcosSingleTest();
                    }
                }
            }
        }

        public static void AcosSingleTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += acosSingleDelta;
                result += MathF.Acos(value);
            }

            var diff = MathF.Abs(acosSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {acosSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
