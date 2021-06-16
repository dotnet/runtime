// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Floor(float) over 5000 iterations for the domain -1, +1

        private const float floorDelta = 0.0004f;
        private const float floorExpectedResult = -2498.0f;

        public void Floor() => FloorTest();

        public static void FloorTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += floorDelta;
                result += MathF.Floor(value);
            }

            float diff = MathF.Abs(floorExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {floorExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
