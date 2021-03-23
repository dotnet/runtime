// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Pow(double, double) over 5000 iterations for the domain x: +2, +1; y: -2, -1

        private const double powDeltaX = -0.0004;
        private const double powDeltaY = 0.0004;
        private const double powExpectedResult = 4659.4627376138733;

        public void Pow() => PowTest();

        public static void PowTest()
        {
            double result = 0.0, valueX = 2.0, valueY = -2.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                valueX += powDeltaX; valueY += powDeltaY;
                result += Math.Pow(valueX, valueY);
            }

            double diff = Math.Abs(powExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {powExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
