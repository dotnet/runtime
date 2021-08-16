// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Log(float) over 5000 iterations for the domain -1, +1

        private const float logDelta = 0.0004f;
        private const float logExpectedResult = -1529.14014f;

        public void Log() => LogTest();

        public static void LogTest()
        {
            float result = 0.0f, value = 0.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += logDelta;
                result += MathF.Log(value);
            }

            float diff = MathF.Abs(logExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {logExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }

}
