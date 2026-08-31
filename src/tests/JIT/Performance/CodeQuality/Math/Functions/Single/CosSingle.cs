// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Cos(float) over 5000 iterations for the domain 0, PI

        private const float cosSingleDelta = 0.000628318531f;
        private const float cosSingleExpectedResult = -0.993487537f;

        public static void CosSingleTest()
        {
            var result = 0.0f; var value = 0.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += cosSingleDelta;
                result += MathF.Cos(value);
            }

            var diff = MathF.Abs(cosSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {cosSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
