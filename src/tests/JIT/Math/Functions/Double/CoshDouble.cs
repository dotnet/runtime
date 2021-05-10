// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Cosh(double) over 5000 iterations for the domain -1, +1

        private const double coshDelta = 0.0004;
        private const double coshExpectedResult = 5876.0060465657216;

        public void Cosh() => CoshTest();

        public static void CoshTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += coshDelta;
                result += Math.Cosh(value);
            }

            double diff = Math.Abs(coshExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {coshExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
