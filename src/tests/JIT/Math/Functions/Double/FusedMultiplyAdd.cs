// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.FusedMultiplyAdd(double, double, double) over 5000 iterations for the domain x: +2, +1; y: -2, -1, z: +1, -1

        private const double fusedMultiplyAddDeltaX = -0.0004;
        private const double fusedMultiplyAddDeltaY = 0.0004;
        private const double fusedMultiplyAddDeltaZ = -0.0004;
        private const double fusedMultiplyAddExpectedResult = -6667.6668000005066;

        public void FusedMultiplyAdd() => FusedMultiplyAddTest();

        public static void FusedMultiplyAddTest()
        {
            double result = 0.0, valueX = 2.0, valueY = -2.0, valueZ = 1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += Math.FusedMultiplyAdd(valueX, valueY, valueZ);
                valueX += fusedMultiplyAddDeltaX; valueY += fusedMultiplyAddDeltaY; valueZ += fusedMultiplyAddDeltaZ;
            }

            double diff = Math.Abs(fusedMultiplyAddExpectedResult - result);

            if (double.IsNaN(result) || (diff > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result {fusedMultiplyAddExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
