﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Xunit.Performance;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Abs(single) over 5000 iterations for the domain -1, +1

        private const float absSingleDelta = 0.0004f;
        private const float absSingleExpectedResult = 2500.03125f;

        [Benchmark]
        public static void AbsSingleBenchmark()
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                using (iteration.StartMeasurement())
                {
                    AbsSingleTest();
                }
            }
        }

        public static void AbsSingleTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += absSingleDelta;
                result += Math.Abs(value);
            }

            var diff = Math.Abs(absSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {absSingleExpectedResult}; Actual Result {result}");
            }
        }
    }
}
