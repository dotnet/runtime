// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.FusedMultiplyAdd(float, float, float) over 5000 iterations for the domain x: +2, +1; y: -2, -1, z: +1, -1

        private const float fusedMultiplyAddDeltaX = -0.0004f;
        private const float fusedMultiplyAddDeltaY = 0.0004f;
        private const float fusedMultiplyAddDeltaZ = -0.0004f;
        private const float fusedMultiplyAddExpectedResult = -6668.49072f;

        public void FusedMultiplyAdd() => FusedMultiplyAddTest();

        public static void FusedMultiplyAddTest()
        {
            float result = 0.0f, valueX = 2.0f, valueY = -2.0f, valueZ = 1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += MathF.FusedMultiplyAdd(valueX, valueY, valueZ);
                valueX += fusedMultiplyAddDeltaX; valueY += fusedMultiplyAddDeltaY; valueZ += fusedMultiplyAddDeltaZ;
            }

            float diff = MathF.Abs(fusedMultiplyAddExpectedResult - result);

            if (float.IsNaN(result) || (diff > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result {fusedMultiplyAddExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
