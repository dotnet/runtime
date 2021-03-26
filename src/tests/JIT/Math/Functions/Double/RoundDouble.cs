// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Round(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double roundDelta = 0.0006283185307180;
        private const double roundExpectedResult = 2;

        public void Round() => RoundTest();

        public static void RoundTest()
        {
            double result = 0.0, value = -1.5707963267948966;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += roundDelta;
                result += Math.Round(value);
            }

            double diff = Math.Abs(roundExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {roundExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
