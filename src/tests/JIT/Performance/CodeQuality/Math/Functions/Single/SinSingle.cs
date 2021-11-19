// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Sin(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float sinSingleDelta = 0.000628318531f;
        private const float sinSingleExpectedResult = 1.03592682f;

        public static void SinSingleTest()
        {
            var result = 0.0f; var value = -1.57079633f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += sinSingleDelta;
                result += MathF.Sin(value);
            }

            var diff = MathF.Abs(sinSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {sinSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
