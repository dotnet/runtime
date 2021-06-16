// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Asinh(double) over 5000 iterations for the domain -1, +1

        private const double asinhDelta = 0.0004;
        private const double asinhExpectedResult = -0.88137358721605752;

        public void Asinh() => AsinhTest();

        public static void AsinhTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += Math.Asinh(value);
                value += asinhDelta;
            }

            double diff = Math.Abs(asinhExpectedResult - result);

            if (double.IsNaN(result) || (diff > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result {asinhExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
