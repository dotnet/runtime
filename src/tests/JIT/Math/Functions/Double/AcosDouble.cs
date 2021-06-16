// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Acos(double) over 5000 iterations for the domain -1, +1

        private const double acosDelta = 0.0004;
        private const double acosExpectedResult = 7852.4108380716079;

        public void Acos() => AcosTest();

        public static void AcosTest()
        {
            double result = 0.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += acosDelta;
                result += Math.Acos(value);
            }

            double diff = Math.Abs(acosExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {acosExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }
}
