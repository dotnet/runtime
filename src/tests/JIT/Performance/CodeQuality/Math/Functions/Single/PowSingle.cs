// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Pow(float, float) over 5000 iterations for the domain x: +2, +1; y: -2, -1

        private const float powSingleDeltaX = -0.0004f;
        private const float powSingleDeltaY = 0.0004f;
        private const float powSingleExpectedResult = 4659.30762f;

        [Benchmark(InnerIterationCount = PowSingleIterations)]
        public static void PowSingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        PowSingleTest();
                    }
                }
            }
        }

        public static void PowSingleTest()
        {
            var result = 0.0f; var valueX = 2.0f; var valueY = -2.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                valueX += powSingleDeltaX; valueY += powSingleDeltaY;
                result += MathF.Pow(valueX, valueY);
            }

            var diff = MathF.Abs(powSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {powSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
