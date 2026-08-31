// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Functions
{
    public static partial class MathTests
    {
        // Tests MathF.Log(float) over 5000 iterations for the domain -1, +1

        private const float logSingleDelta = 0.0004f;
        private const float logSingleExpectedResult = -1529.14014f;

        public static void LogSingleTest()
        {
            var result = 0.0f; var value = 0.0f;

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                value += logSingleDelta;
                result += MathF.Log(value);
            }

            var diff = MathF.Abs(logSingleExpectedResult - result);

            if (diff > singleEpsilon)
            {
                throw new Exception($"Expected Result {logSingleExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }

}
