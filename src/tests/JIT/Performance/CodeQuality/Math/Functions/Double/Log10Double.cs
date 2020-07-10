// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Log10(double) over 5000 iterations for the domain -1, +1

        private const double log10DoubleDelta = 0.0004;
        private const double log10DoubleExpectedResult = -664.07384902184072;

        [Benchmark(InnerIterationCount = Log10DoubleIterations)]
        public static void Log10DoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        Log10DoubleTest();
                    }
                }
            }
        }

        public static void Log10DoubleTest()
        {
            var result = 0.0; var value = 0.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += log10DoubleDelta;
                result += Math.Log10(value);
            }

            var diff = Math.Abs(log10DoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {log10DoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
