// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Abs(double) over 5000 iterations for the domain -1, +1

        private const double absDelta = 0.0004;
        private const double absExpectedResult = 2499.9999999999659;

        public void Abs() => AbsTest();

        public static void AbsTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += absDelta;
                result += Math.Abs(value);
            }

            double diff = Math.Abs(absExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {absExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
