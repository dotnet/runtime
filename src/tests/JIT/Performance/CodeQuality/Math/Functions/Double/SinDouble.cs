// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Sin(double) over 5000 iterations for the domain -PI/2, +PI/2

        private const double sinDoubleDelta = 0.0006283185307180;
        private const double sinDoubleExpectedResult = 1.0000000005445053;

        public static void SinDoubleTest()
        {
            var result = 0.0; var value = -1.5707963267948966;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += sinDoubleDelta;
                result += Math.Sin(value);
            }

            var diff = Math.Abs(sinDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {sinDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
