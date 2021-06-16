// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Atan2(double, double) over 5000 iterations for the domain y: -1, +1; x: +1, -1

        private const double atan2DeltaX = -0.0004;
        private const double atan2DeltaY = 0.0004;
        private const double atan2ExpectedResult = 3926.99081698702;

        public void Atan2() => Atan2Test();

        public static void Atan2Test()
        {
            double result = 0.0, valueX = 1.0, valueY = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                valueX += atan2DeltaX; valueY += atan2DeltaY;
                result += Math.Atan2(valueY, valueX);
            }

            double diff = Math.Abs(atan2ExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {atan2ExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
