// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Ceiling(double) over 5000 iterations for the domain -1, +1

        private const double ceilingDelta = 0.0004;
        private const double ceilingExpectedResult = 2500;

        public void Ceiling() => CeilingTest();

        public static void CeilingTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += ceilingDelta;
                result += Math.Ceiling(value);
            }

            double diff = Math.Abs(ceilingExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {ceilingExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
