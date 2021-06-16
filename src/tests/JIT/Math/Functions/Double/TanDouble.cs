// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Tan(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double tanDelta = 0.0004;
        private const double tanExpectedResult = 1.5574077243051505;

        public void Tan() => TanTest();

        public static void TanTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += tanDelta;
                result += Math.Tan(value);
            }

            double diff = Math.Abs(tanExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {tanExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
