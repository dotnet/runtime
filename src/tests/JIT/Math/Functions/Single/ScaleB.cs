// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.ScaleB(float, int) over 5000 iterations for the domain x: -1, +1; y: +0, +5000

        private const float scaleBDeltaX = -0.0004f;
        private const int scaleBDeltaY = 1;
        private const float scaleBExpectedResult = float.NegativeInfinity;

        public void ScaleB() => ScaleBTest();

         public static void ScaleBTest()
        {
            float result = 0.0f, valueX = -1.0f;
            int valueY = 0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += MathF.ScaleB(valueX, valueY);
                valueX += scaleBDeltaX; valueY += scaleBDeltaY;
            }

            float diff = MathF.Abs(scaleBExpectedResult - result);

            if (float.IsNaN(result) || (diff > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result {scaleBExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
