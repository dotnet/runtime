﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Ceiling(double) over 5000 iterations for the domain -1, +1

        private const double ceilingDoubleDelta = 0.0004;
        private const double ceilingDoubleExpectedResult = 2500;

        [Benchmark]
        public static void CeilingDoubleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    CeilingDoubleTest();
                }
            }
        }

        public static void CeilingDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += ceilingDoubleDelta;
                result += Math.Ceiling(value);
            }

            var diff = Math.Abs(ceilingDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {ceilingDoubleExpectedResult}; Actual Result {result}");
            }
        }
    }
}
