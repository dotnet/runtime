// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Sqrt(float) over 5000 iterations for the domain 0, PI

        private const float sqrtSingleDelta = 0.000628318531f;
        private const float sqrtSingleExpectedResult = 5909.03027f;

        public static void SqrtSingleTest()
        {
            var result = 0.0f; var value = 0.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += sqrtSingleDelta;
                result += MathF.Sqrt(value);
            }

            var diff = MathF.Abs(sqrtSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {sqrtSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
