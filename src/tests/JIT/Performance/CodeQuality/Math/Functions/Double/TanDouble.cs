// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Tan(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double tanDoubleDelta = 0.0004;
        private const double tanDoubleExpectedResult = 1.5574077243051505;

        public static void TanDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += tanDoubleDelta;
                result += Math.Tan(value);
            }

            var diff = Math.Abs(tanDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {tanDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
