// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Floor(double) over 5000 iterations for the domain -1, +1

        private const double floorDelta = 0.0004;
        private const double floorExpectedResult = -2500;

        public void Floor() => FloorTest();

        public static void FloorTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += floorDelta;
                result += Math.Floor(value);
            }

            double diff = Math.Abs(floorExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {floorExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
