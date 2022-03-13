// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Tanh(double) over 5000 iterations for the domain -1, +1

        private const double tanhDoubleDelta = 0.0004;
        private const double tanhDoubleExpectedResult = 0.76159415578341827;

        public static void TanhDoubleTest()
        {
            var result = 0.0; var value = -1.0;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += tanhDoubleDelta;
                result += Math.Tanh(value);
            }

            var diff = Math.Abs(tanhDoubleExpectedResult - result);

            if (diff > doubleEpsilon)
            {
                throw new Exception($"Expected Result {tanhDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
