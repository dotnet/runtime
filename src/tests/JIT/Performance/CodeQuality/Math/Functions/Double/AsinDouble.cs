// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Asin(double) over 5000 iterations for the domain -1, +1

        private const double asinDoubleDelta = 0.0004;
        private const double asinDoubleExpectedResult = 1.5707959028763392;

        public static void AsinDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += asinDoubleDelta;
                result += Math.Asin(value);
            }

            var diff = Math.Abs(asinDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {asinDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
