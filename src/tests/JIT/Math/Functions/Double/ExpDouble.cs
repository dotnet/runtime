// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Exp(double) over 5000 iterations for the domain -1, +1

        private const double expDelta = 0.0004;
        private const double expExpectedResult = 5877.1812477590884;

        public void Exp() => ExpTest();

        public static void ExpTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += expDelta;
                result += Math.Exp(value);
            }

            double diff = Math.Abs(expExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {expExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
