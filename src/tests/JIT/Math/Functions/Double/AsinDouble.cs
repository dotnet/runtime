// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Asin(double) over 5000 iterations for the domain -1, +1

        private const double asinDelta = 0.0004;
        private const double asinExpectedResult = 1.5707959028763392;

        public void Asin() => AsinTest();

        public static void AsinTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += asinDelta;
                result += Math.Asin(value);
            }

            double diff = Math.Abs(asinExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {asinExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
