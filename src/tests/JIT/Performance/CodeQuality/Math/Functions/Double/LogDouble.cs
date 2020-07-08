// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Log(double) over 5000 iterations for the domain -1, +1

        private const double logDoubleDelta = 0.0004;
        private const double logDoubleExpectedResult = -1529.0865454048721;

        [Benchmark(InnerIterationCount = LogDoubleIterations)]
        public static void LogDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        LogDoubleTest();
                    }
                }
            }
        }

        public static void LogDoubleTest()
        {
            var result = 0.0; var value = 0.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += logDoubleDelta;
                result += Math.Log(value);
            }

            var diff = Math.Abs(logDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {logDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }

}
