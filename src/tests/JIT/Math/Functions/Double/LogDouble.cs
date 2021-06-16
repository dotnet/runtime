// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Double
    {
        // Tests Math.Log(double) over 5000 iterations for the domain -1, +1

        private const double logDelta = 0.0004;
        private const double logExpectedResult = -1529.0865454048721;

        public void Log() => LogTest();

        public static void LogTest()
        {
            double result = 0.0, value = 0.0;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += logDelta;
                result += Math.Log(value);
            }

            double diff = Math.Abs(logExpectedResult - result);

            if (diff > MathTests.DoubleEpsilon)
            {
                throw new Exception($"Expected Result {logExpectedResult,20:g17}; Actual Result {result,20:g17}");
            }
        }
    }

}
