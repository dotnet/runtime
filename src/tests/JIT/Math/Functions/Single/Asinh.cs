// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Asinh(float) over 5000 iterations for the domain -1, +1

        private const float asinhDelta = 0.0004f;
        private const float asinhExpectedResult = -0.814757347f;

        public void Asinh() => AsinhTest();

        public static void AsinhTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += MathF.Asinh(value);
                value += asinhDelta;
            }

            float diff = MathF.Abs(asinhExpectedResult - result);

            if (float.IsNaN(result) || (diff > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result {asinhExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
