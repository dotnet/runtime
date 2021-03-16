// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.ScaleB(double, int) over 5000 iterations for the domain x: -1, +1; y: +0, +5000

        private const double scaleBDeltaX = -0.0004;
        private const int scaleBDeltaY = 1;
        private const double scaleBExpectedResult = double.NegativeInfinity;

        public void ScaleB() => ScaleBTest();

        public static void ScaleBTest()
        {
            double result = 0.0, valueX = -1.0;
            int valueY = 1;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += Math.ScaleB(valueX, valueY);
                valueX += scaleBDeltaX; valueY += scaleBDeltaY;
            }

            double diff = Math.Abs(scaleBExpectedResult - result);

            if (double.IsNaN(result) || (diff > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result {scaleBExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
