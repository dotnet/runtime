// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Ceiling(float) over 5000 iterations for the domain -1, +1

        private const float ceilingDelta = 0.0004f;
        private const float ceilingExpectedResult = 2502.0f;

        public void Ceiling() => CeilingTest();

        public static void CeilingTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += ceilingDelta;
                result += MathF.Ceiling(value);
            }

            float diff = MathF.Abs(ceilingExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {ceilingExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
