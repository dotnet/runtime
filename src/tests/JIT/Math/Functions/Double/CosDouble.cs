// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Cos(double) over 5000 iterations for the domain 0, PI

        private const double cosDoubleDelta = 0.0006283185307180;
        private const double cosDoubleExpectedResult = -1.0000000005924159;

        public void Cos() => CosDoubleTest();

        public static void CosDoubleTest()
        {
            double result = 0.0, value = 0.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += cosDoubleDelta;
                result += Math.Cos(value);
            }

            double diff = Math.Abs(cosDoubleExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {cosDoubleExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
