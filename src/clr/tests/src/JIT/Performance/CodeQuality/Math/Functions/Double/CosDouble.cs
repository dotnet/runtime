﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Cos(double) over 5000 iterations for the domain 0, PI

        private const double cosDoubleDelta = 0.0006283185307180;
        private const double cosDoubleExpectedResult = -1.0000000005924159;

        [Benchmark(InnerIterationCount = CosDoubleIterations)]
        public static void CosDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        CosDoubleTest();
                    }
                }
            }
        }

        public static void CosDoubleTest()
        {
            var result = 0.0; var value = 0.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += cosDoubleDelta;
                result += Math.Cos(value);
            }

            var diff = Math.Abs(cosDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {cosDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
