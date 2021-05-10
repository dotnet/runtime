// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Tan(float) over 5000 iterations for the domain -PI/2, +PI/2

        private const float tanDelta = 0.0004f;
        private const float tanExpectedResult = 1.66717815f;

        public void Tan() => TanTest();

        public static void TanTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += tanDelta;
                result += MathF.Tan(value);
            }

            float diff = MathF.Abs(tanExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {tanExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
