// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Atanh(double) over 5000 iterations for the domain -1, +1

        private const double atanhDelta = 0.0004;
        private const double atanhExpectedResult = float.NegativeInfinity;

        public void Atanh() => AtanhTest();

        public static void AtanhTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += Math.Atanh(value);
                value += atanhDelta;
            }

            double diff = Math.Abs(atanhExpectedResult - result);

            if (double.IsNaN(result) || (diff > MathTests.DoubleEpsilon))
            {
                throw new Exception($"Expected Result {atanhExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
