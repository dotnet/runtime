// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Log(double) over 5000 iterations for the domain -1, +1

        private const double logDoubleDelta = 0.0004;
        private const double logDoubleExpectedResult = -1529.0865454048721;

        public static void LogDoubleTest()
        {
            var result = 0.0; var value = 0.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += logDoubleDelta;
                result += Math.Log(value);
            }

            var diff = Math.Abs(logDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {logDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }

}
