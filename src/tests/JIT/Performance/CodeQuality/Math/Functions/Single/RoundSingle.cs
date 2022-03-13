// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Round(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float roundSingleDelta = 0.000628318531f;
        private const float roundSingleExpectedResult = 2.0f;

        public static void RoundSingleTest()
        {
            var result = 0.0f; var value = -1.57079633f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += roundSingleDelta;
                result += MathF.Round(value);
            }

            var diff = MathF.Abs(roundSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {roundSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
