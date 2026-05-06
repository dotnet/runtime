// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Exp(double) over 5000 iterations for the domain -1, +1

        private const double expDoubleDelta = 0.0004;
        private const double expDoubleExpectedResult = 5877.1812477590884;

        public static void ExpDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += expDoubleDelta;
                result += Math.Exp(value);
            }

            var diff = Math.Abs(expDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {expDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
