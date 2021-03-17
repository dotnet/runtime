// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests Math.Abs(single) over 5000 iterations for the domain -1, +1

        private const float absDelta = 0.0004f;
        private const float absExpectedResult = 2500.03125f;

        public void Abs() => AbsTest();

        public static void AbsTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += absDelta;
                result += Math.Abs(value);
            }

            float diff = Math.Abs(absExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {absExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
