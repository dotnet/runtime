// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Sin(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float sinDelta = 0.000628318531f;
        private const float sinExpectedResult = 1.03592682f;

        public void Sin() => SinTest();

        public static void SinTest()
        {
            float result = 0.0f, value = -1.57079633f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinDelta;
                result += MathF.Sin(value);
            }

            float diff = MathF.Abs(sinExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {sinExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
