// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests Math.Abs(single) over 5000 iterations for the domain -1, +1

        private const float absSingleDelta = 0.0004f;
        private const float absSingleExpectedResult = 2500.03125f;

        public static void AbsSingleTest()
        {
            var result = 0.0f; var value = -1.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += absSingleDelta;
                result += Math.Abs(value);
            }

            var diff = Math.Abs(absSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {absSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
