// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Min(double) over 5000 iterations for the domain -1, +1

        private const double maxDelta = 0.0004;
        private const double maxExpectedResult = -1.0;

        public void Max() => MaxTest();

        public static void MaxTest()
        {
            double result = 0.0, val1 = -1.0, val2 = -1.0 - maxDelta;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                val2 += maxDelta;
                result += Math.Max(val1, val2);
            }

            double diff = Math.Abs(maxExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {maxExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
