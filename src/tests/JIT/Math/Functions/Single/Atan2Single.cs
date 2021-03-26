// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Atan2(float, float) over 5000 iterations for the domain y: -1, +1; x: +1, -1

        private const float atan2DeltaX = -0.0004f;
        private const float atan2DeltaY = 0.0004f;
        private const float atan2ExpectedResult = 3930.14282f;

        public void Atan2() => Atan2Test();

        public static void Atan2Test()
        {
            float result = 0.0f, valueX = 1.0f, valueY = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                valueX += atan2DeltaX; valueY += atan2DeltaY;
                result += MathF.Atan2(valueY, valueX);
            }

            float diff = MathF.Abs(atan2ExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {atan2ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
