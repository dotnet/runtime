// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.CopySign(double) over 5000 iterations for the domain -1, +1

        private const double copySignDelta = 0.0004;
        private const int copySignExpectedResult = 0;

        public void CopySign() => CopySignTest();

        public static void CopySignTest()
        {
            double result = 1.0, value = -1.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += copySignDelta;
                result += Math.CopySign(result, value);
            }

            if (result != copySignExpectedResult)
            {
                throw new Exception($"Expected Result {copySignExpectedResult}; Actual Result {result}");
            }
        }
    }
}
