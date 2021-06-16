// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Tanh(float) over 5000 iterations for the domain -1, +1

        private const float tanhDelta = 0.0004f;
        private const float tanhExpectedResult = 0.816701353f;

        public void Tanh() => TanhTest();

        public static void TanhTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += tanhDelta;
                result += MathF.Tanh(value);
            }

            float diff = MathF.Abs(tanhExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {tanhExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
