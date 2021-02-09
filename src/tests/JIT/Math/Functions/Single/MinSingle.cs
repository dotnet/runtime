// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests Math.Min(double) over 5000 iterations for the domain -1, +1

        private const float minDelta = 0.0004f;
        private const float minExpectedResult = 0.9267354f;

        public void Min() => MinTest();

        public static void MinTest()
        {
            float result = 0.0f, val1 = 1.0f, val2 = 1.0f + minDelta;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                val2 -= minDelta;
                result += Math.Min(val1, val2);
            }

            float diff = Math.Abs(minExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {minExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
