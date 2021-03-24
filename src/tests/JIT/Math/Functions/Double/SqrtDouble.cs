// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Sqrt(double) over 5000 iterations for the domain 0, PI

        private const double sqrtDelta = 0.0006283185307180;
        private const double sqrtExpectedResult = 5909.0605337797215;

        public void Sqrt() => SqrtTest();

        public static void SqrtTest()
        {
            double result = 0.0, value = 0.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sqrtDelta;
                result += Math.Sqrt(value);
            }

            double diff = Math.Abs(sqrtExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {sqrtExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
