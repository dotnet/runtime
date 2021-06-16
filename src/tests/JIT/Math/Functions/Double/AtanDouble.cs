// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Atan(double) over 5000 iterations for the domain -1, +1

        private const double atanDelta = 0.0004;
        private const double atanExpectedResult = 0.78539816322061329;

        public void Atan() => AtanTest();

        public static void AtanTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += atanDelta;
                result += Math.Atan(value);
            }

            double diff = Math.Abs(atanExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {atanExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
